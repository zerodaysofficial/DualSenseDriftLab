using DriftLab.Core;
using System.Buffers.Binary;
using System.Text;
namespace DriftLab.Protocol;
public sealed class DualSenseController(IHidTransport transport)
{
 private readonly SemaphoreSlim logical=new(1);
 private async Task<T> Single<T>(Func<Task<T>> operation,CancellationToken ct){await logical.WaitAsync(ct);try{return await operation();}finally{logical.Release();}}
 private async Task<byte[]> Command(byte[] payload,CancellationToken ct){await transport.SetFeatureAsync(0x80,payload,ct);await Task.Delay(100,ct);return await transport.GetFeatureAsync(0x81,ct);}
 private async Task<NvStatus> Nv(CancellationToken ct)=>FeatureCodec.Nv(await Command([3,3],ct));
 private async Task<CalibrationData> Read(CancellationToken ct)=>FeatureCodec.ReadCalibration(await Command([12,2],ct));
 public Task<NvStatus> QueryNvStatusAsync(CancellationToken ct)=>Single(()=>Nv(ct),ct);
 public Task<CalibrationData> ReadCalibrationAsync(CancellationToken ct)=>Single(()=>Read(ct),ct);
 public Task<ControllerIdentity> ReadIdentityAsync(CancellationToken ct)=>Single(async()=>{
  var b=await transport.GetFeatureAsync(0x20,ct);if(b.Length<64||b[0]!=0x20)throw new InvalidDataException(AppText.T("Identità firmware non disponibile"));string date=Encoding.ASCII.GetString(b,1,11);
  if(date.Contains("2020")||date.Contains("2021"))throw new NotSupportedException(AppText.T("Firmware troppo vecchio: aggiorna il controller prima della calibrazione"));
  var serialData=await Command([1,19],ct);if(serialData.Length<21||serialData[0]!=0x81||serialData[1]!=1||serialData[2]!=19)throw new InvalidDataException(AppText.T("Risposta seriale inattesa"));
  string serial=Encoding.ASCII.GetString(serialData,4,17).TrimEnd('\0',' ');if(!FeatureCodec.ValidSerial(serial))throw new InvalidDataException(AppText.T("Seriale non affidabile: modifiche bloccate"));
  return new ControllerIdentity(serial,BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(28,4)),BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(24,4)));
 },ct);
 public Task WriteTemporaryAsync(CalibrationData data,CancellationToken ct)=>Single(async()=>{
  if(await Nv(ct)!=NvStatus.Locked)throw new InvalidOperationException(AppText.T("Memoria non bloccata: regolazione temporanea vietata"));
  await transport.SetFeatureAsync(0x80,FeatureCodec.Encode(data),ct);await Task.Delay(100,ct);if(!(await Read(ct)).SameAs(data))throw new IOException(AppText.T("Rilettura diversa: regolazione non verificata"));return true;
 },ct);
 public Task SavePermanentAsync(CancellationToken ct)=>Single(async()=>{
  if(await Nv(ct)!=NvStatus.Locked)throw new InvalidOperationException(AppText.T("Stato memoria incompatibile"));Exception? original=null;
  try{await Command([3,2,101,50,64,12],ct);if(await Nv(ct)!=NvStatus.Unlocked)throw new IOException(AppText.T("Sblocco memoria non verificato"));}
  catch(Exception e){original=e;}
  using var recovery=new CancellationTokenSource(TimeSpan.FromSeconds(8));
  try{await Command([3,1],recovery.Token);var state=await Nv(recovery.Token);if(state is not (NvStatus.Locked or NvStatus.PendingReboot))throw new IOException(AppText.T("Blocco finale memoria non confermato"));}
  catch(Exception e){throw new IOException(AppText.T("Blocco finale fallito. Salvataggio incerto; ricollega e verifica il controller."),e);}
  if(original!=null)throw new IOException(AppText.T("Salvataggio non concluso; blocco memoria ripristinato."),original);return true;
 },ct);
 public Task RebootAsync(CancellationToken ct)=>Single(async()=>{await transport.SetFeatureAsync(0x80,new byte[]{1,1},ct);return true;},ct);
 public Task CalibrateCenterAsync(CancellationToken ct)=>Single(async()=>{
  if(await Nv(ct)!=NvStatus.Locked)throw new InvalidOperationException(AppText.T("Memoria non bloccata"));
  foreach(var payload in new byte[][]{[1,1,1],[3,1,1],[2,1,1]}){await transport.SetFeatureAsync(0x82,payload,ct);await Task.Delay(200,ct);var b=await transport.GetFeatureAsync(0x83,ct);byte end=(byte)(payload[0]==2?2:1);if(b.Length<4||b[0]!=0x83||b[1]!=1||b[2]!=1||b[3]!=end)throw new IOException(AppText.T("Calibrazione guidata non confermata"));}return true;
 },ct);
}
