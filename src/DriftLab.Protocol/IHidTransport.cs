using DriftLab.Core;
namespace DriftLab.Protocol;
public interface IHidTransport:IAsyncDisposable
{
 Task<byte[]> GetFeatureAsync(byte reportId,CancellationToken ct);
 Task SetFeatureAsync(byte reportId,ReadOnlyMemory<byte> payload,CancellationToken ct);
 IAsyncEnumerable<byte[]> ReadInputsAsync(CancellationToken ct);
}
public sealed class FeatureTransferGate(TimeSpan timeout)
{
 private readonly SemaphoreSlim gate=new(1);private volatile bool poisoned;
 public async Task<T> RunAsync<T>(Func<Task<T>> operation,CancellationToken ct)
 {
  if(poisoned)throw new IOException(AppText.T("Sessione HID invalidata da un trasferimento interrotto"));
  await gate.WaitAsync(ct);bool held=true;
  try{
   if(poisoned)throw new IOException(AppText.T("Sessione HID invalidata"));var pending=operation();
   try{return await pending.WaitAsync(timeout,ct);}catch when(!pending.IsCompleted){poisoned=true;held=false;_ = pending.ContinueWith(t=>{_ = t.Exception;gate.Release();},TaskScheduler.Default);throw;}
  }finally{if(held)gate.Release();}
 }
}
