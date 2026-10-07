using System.Diagnostics;
using System.Threading.Channels;
using DriftLab.Core;
using DriftLab.Protocol;
using DriftLab.WindowsHid;
namespace DriftLab.Desktop;
public sealed class LiveController(Func<string,CancellationToken,Task<bool>> confirm):ITestInteraction,IAsyncDisposable
{
 private sealed class Connection(WindowsHidTransport transport,string path)
 {
  public readonly string Path=path;public readonly WindowsHidTransport Transport=transport;public readonly CancellationTokenSource Stop=new();public readonly Stopwatch Clock=Stopwatch.StartNew();public readonly object Sync=new();
  public readonly TaskCompletionSource First=new(TaskCreationOptions.RunContinuationsAsynchronously);public Task Pump=Task.CompletedTask;public StickSample Latest;public BatteryInfo Battery=new(null,AppText.T("Sconosciuta"),false);public bool Seen;public int Lost;
 }
 private Connection? current;private event Action<Connection,StickSample>? Sample;
 private volatile bool rebooting;public bool Rebooting{get=>rebooting;set=>rebooting=value;}public event Action? Disconnected;
 public BatteryInfo Battery{get{var c=current;if(c==null)return new(null,AppText.T("Sconosciuta"),false);lock(c.Sync)return c.Battery;}}
 public StickSample? Latest{get{var c=current;if(c==null)return null;lock(c.Sync)return c.Seen?c.Latest:null;}}
 public bool IsAlive{get{var c=current;if(c==null||c.Stop.IsCancellationRequested)return false;lock(c.Sync)return c.Seen&&(c.Clock.Elapsed-c.Latest.Time).TotalSeconds<3;}}
 public async Task<DualSenseController> OpenAsync(HidDeviceInfo device,CancellationToken ct)
 {
  await CloseAsync();var c=new Connection(new WindowsHidTransport(device),device.Path);current=c;c.Pump=Pump(c);_ = Watch(c);
  try{await c.First.Task.WaitAsync(TimeSpan.FromSeconds(3),ct);return new(c.Transport);}catch{await CloseAsync();throw;}
 }
 private void Lose(Connection c){if(Interlocked.Exchange(ref c.Lost,1)!=0)return;if(ReferenceEquals(current,c)&&!Rebooting)Disconnected?.Invoke();c.Stop.Cancel();NativeClose(c);}
 private static async void NativeClose(Connection c){try{await c.Transport.DisposeAsync();}catch{/* Connection already invalid; no further feature transfer is allowed. */}}
 private async Task Pump(Connection c)
 {
  try{await foreach(var report in c.Transport.ReadInputsAsync(c.Stop.Token)){if(!UsbInputDecoder.TryDecode(report,c.Clock.Elapsed,out var s,out var battery))throw new InvalidDataException(AppText.T("Report USB inatteso"));lock(c.Sync){c.Latest=s;c.Battery=battery;c.Seen=true;}c.First.TrySetResult();Sample?.Invoke(c,s);}}
  catch(Exception e){c.First.TrySetException(e);if(!c.Stop.IsCancellationRequested)Lose(c);}
 }
 private async Task Watch(Connection c)
 {
  try{using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(500));while(await timer.WaitForNextTickAsync(c.Stop.Token)){double age;lock(c.Sync)age=c.Seen?(c.Clock.Elapsed-c.Latest.Time).TotalSeconds:c.Clock.Elapsed.TotalSeconds;if(age>3){Lose(c);return;}}}catch(OperationCanceledException){}
 }
 public async Task CloseAsync(){var c=current;current=null;if(c==null)return;c.Stop.Cancel();await c.Transport.DisposeAsync();try{await c.Pump.WaitAsync(TimeSpan.FromSeconds(2));}catch{}c.Stop.Dispose();}
 private async Task<T> Collect<T>(TimeSpan deadline,Func<ChannelReader<StickSample>,CancellationToken,Task<T>> collect,CancellationToken ct)
 {
  var connection=current??throw new IOException(AppText.T("Controller non connesso"));using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,connection.Stop.Token);linked.CancelAfter(deadline);
  var channel=Channel.CreateBounded<StickSample>(new BoundedChannelOptions(4096){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true,SingleWriter=true});
  void Accept(Connection source,StickSample s){if(ReferenceEquals(source,connection))channel.Writer.TryWrite(s);}
  Sample+=Accept;try{return await collect(channel.Reader,linked.Token);}catch(OperationCanceledException) when(!ct.IsCancellationRequested){throw new IOException(AppText.T("Acquisizione scaduta o controller scollegato. Test invalidato."));}finally{Sample-=Accept;channel.Writer.TryComplete();}
 }
 public Task<IReadOnlyList<StickSample>> FreshAsync(TimeSpan duration,CancellationToken ct)=>Collect<IReadOnlyList<StickSample>>(duration+TimeSpan.FromSeconds(3),async(reader,token)=>{
  var samples=new List<StickSample>();await foreach(var s in reader.ReadAllAsync(token)){samples.Add(s);if(s.Time-samples[0].Time>=duration){var r=DriftAnalyzer.Analyze(samples,new(Duration:duration.TotalSeconds,Minimum:32));if(r.Kind==DriftKind.Insufficient)throw new IOException(AppText.T("Campioni freschi insufficienti"));return samples;}}throw new IOException(AppText.T("Acquisizione terminata"));},ct);
 public Task<GuidedTestResult> GuidedAsync(long generation,Action<string,double> progress,CancellationToken ct)=>Collect(TimeSpan.FromMinutes(3),(reader,token)=>SampleAcquisition.GuidedAsync(reader,generation,progress,ConfirmAsync,token),ct);
 public Task<RangeCheckResult> RangeAsync(Action<string,double> progress,CancellationToken ct)=>Collect(TimeSpan.FromMinutes(1),async(reader,token)=>{
  var range=new RangeCapture();progress(AppText.T("Corsa completa: ruota ENTRAMBI gli stick fino ai bordi, poi rilasciali al centro."),.9);
  await foreach(var s in reader.ReadAllAsync(token)){range.Accept(s);if(range.Passed)return new RangeCheckResult(true);}return new RangeCheckResult(false);},ct);
 public Task<bool> ConfirmAsync(string text,CancellationToken ct)=>confirm(text,ct);
 public async Task<Reconnection> ReconnectAsync(ControllerIdentity identity,CancellationToken ct)
 {
  var old=current??throw new IOException(AppText.T("Manca la connessione precedente al riavvio"));
  // Keeping the old handle alive avoids mistaking our own CloseAsync for a
  // controller reset. A successful write alone is not evidence of reboot.
  while(await Task.Run(()=>HidDiscovery.IsPresent(old.Path),ct))await Task.Delay(100,ct);
  await CloseAsync();
  while(true){ct.ThrowIfCancellationRequested();foreach(var info in await Task.Run(HidDiscovery.FindDualSenseUsb,ct)){
   try{var device=await OpenAsync(info,ct);if(await device.ReadIdentityAsync(ct)==identity)return new(device,true);await CloseAsync();}catch(OperationCanceledException){throw;}catch{await CloseAsync();}
  }await Task.Delay(250,ct);}
 }
 public async ValueTask DisposeAsync()=>await CloseAsync();
}
