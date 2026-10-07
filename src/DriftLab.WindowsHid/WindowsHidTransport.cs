using DriftLab.Core;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using DriftLab.Protocol;
namespace DriftLab.WindowsHid;
public sealed class WindowsHidTransport:IHidTransport
{
 private static readonly ConcurrentDictionary<string,Task> Active=new(StringComparer.OrdinalIgnoreCase);
 private static readonly ConcurrentDictionary<string,byte> Opened=new(StringComparer.OrdinalIgnoreCase);
 private readonly HidDeviceInfo info;private readonly SafeFileHandle handle;private readonly FileStream stream;private readonly FeatureTransferGate features=new(TimeSpan.FromSeconds(3));private volatile bool verified,disposed;
 public WindowsHidTransport(HidDeviceInfo info)
 {
  this.info=info;if(Active.TryGetValue(info.Path,out var pending)&&!pending.IsCompleted)throw new IOException(AppText.T("Trasferimento HID precedente ancora in corso"));if(!Opened.TryAdd(info.Path,0))throw new IOException(AppText.T("Interfaccia controller già aperta"));
  try{handle=NativeMethods.CreateFileW(info.Path,0xC0000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero);if(handle.IsInvalid){handle.Dispose();throw new Win32Exception();}stream=new(handle,FileAccess.ReadWrite,info.InputLength,true);}catch{Opened.TryRemove(info.Path,out _);throw;}
 }
 private Task<T> Feature<T>(Func<T> operation,CancellationToken ct){if(disposed||!verified)throw new IOException(AppText.T("Connessione USB non verificata"));return features.RunAsync(()=>{var task=Task.Run(operation);Active[info.Path]=task;return task;},ct);}
 public Task<byte[]> GetFeatureAsync(byte reportId,CancellationToken ct)=>Feature(()=>{var b=new byte[Math.Max(64,info.FeatureLength)];b[0]=reportId;if(!NativeMethods.HidD_GetFeature(handle,b,b.Length))throw new Win32Exception();return b;},ct);
 public async Task SetFeatureAsync(byte reportId,ReadOnlyMemory<byte> payload,CancellationToken ct){if(payload.Length+1>info.FeatureLength)throw new InvalidDataException(AppText.T("Payload fuori limite HID"));var b=new byte[info.FeatureLength];b[0]=reportId;payload.CopyTo(b.AsMemory(1));await Feature(()=>{if(!NativeMethods.HidD_SetFeature(handle,b,b.Length))throw new Win32Exception();return true;},ct);}
 public async IAsyncEnumerable<byte[]> ReadInputsAsync([EnumeratorCancellation]CancellationToken ct)
 {
  while(!ct.IsCancellationRequested&&!disposed){var b=new byte[info.InputLength];int count=await stream.ReadAsync(b,ct);if(count==0)throw new IOException(AppText.T("Controller scollegato"));if(count!=64||b[0]!=1)throw new InvalidDataException(AppText.T("Report non USB: calibrazione bloccata"));verified=true;yield return b;}
 }
 public async ValueTask DisposeAsync(){if(disposed)return;disposed=true;NativeMethods.CancelIoEx(handle,IntPtr.Zero);try{await stream.DisposeAsync();}finally{handle.Dispose();Opened.TryRemove(info.Path,out _);}}
}
