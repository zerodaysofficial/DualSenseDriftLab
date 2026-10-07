using DriftLab.Core;
using DriftLab.Storage;
namespace DriftLab.Protocol;
public interface ITestInteraction
{
 bool Rebooting{get;set;}
 BatteryInfo Battery{get;}
 Task<bool> ConfirmAsync(string message,CancellationToken ct);
 Task<GuidedTestResult> GuidedAsync(long generation,Action<string,double> progress,CancellationToken ct);
 Task<RangeCheckResult> RangeAsync(Action<string,double> progress,CancellationToken ct);
 Task<IReadOnlyList<StickSample>> FreshAsync(TimeSpan duration,CancellationToken ct);
 Task<Reconnection> ReconnectAsync(ControllerIdentity identity,CancellationToken ct);
}
public sealed record Reconnection(DualSenseController Controller,bool ResetObserved);
public sealed record SessionSnapshot(string Message,double Progress,GuidedTestResult? Before,GuidedTestResult? After);
public sealed class ControllerSession(DualSenseController controller,ControllerIdentity identity,BackupRepository repository,ITestInteraction io,TimeProvider? timeProvider=null):ICalibrationSession
{
 private readonly TimeProvider clock=timeProvider??TimeProvider.System;private DualSenseController device=controller;private long generation=1;private int active;private CalibrationData? target;private long verifiedAt;
 public ControllerIdentity Identity=>identity;public SessionState State{get;}=new(){Connected=true};
 public CalibrationBackup? LatestBackup{get;private set;}public GuidedTestResult? Before{get;private set;}public GuidedTestResult? After{get;private set;}
 public event Action<SessionSnapshot>? Updated;
 private void Publish(string message,double progress=0)=>Updated?.Invoke(new(message,progress,Before,After));
 private long Begin(){if(Interlocked.CompareExchange(ref active,1,0)!=0)throw new InvalidOperationException(AppText.T("Un'operazione è già in corso"));if(!State.Connected){Interlocked.Exchange(ref active,0);throw new IOException(AppText.T("Controller non connesso"));}State.Busy=true;return generation;}
 private void End(){State.Busy=false;State.IsPersistentWrite=false;Interlocked.Exchange(ref active,0);}
 private void Guard(long expected){if(!State.Connected||generation!=expected)throw new IOException(AppText.T("Connessione cambiata: risultato invalidato"));}
 private void CurrentProof(){if(target==null||!(State.Verified||State.RestorePending)||clock.GetElapsedTime(verifiedAt)>TimeSpan.FromMinutes(10)){State.Verified=false;State.RestorePending=false;throw new InvalidOperationException(AppText.T("Verifica scaduta: serve un nuovo test positivo o un Restore verificato"));}}
 public void Invalidate(){Interlocked.Increment(ref generation);State.Invalidate();Publish(AppText.T("Controller scollegato. Risultati invalidati."));}
 private async Task Same(long g,CancellationToken ct){Guard(g);if(await device.ReadIdentityAsync(ct)!=identity)throw new IOException(AppText.T("Identità controller cambiata"));Guard(g);}
 private async Task<CalibrationData> Backup(CancellationToken ct){var original=await device.ReadCalibrationAsync(ct);var b=CalibrationBackup.Create(identity,original,"Initial");await repository.SaveAsync(b,ct);LatestBackup=b;return original;}
 public async Task RunAutoAsync(CancellationToken ct)
 {
  long g=Begin();CalibrationData? original=null;bool changed=false;State.Verified=false;State.RestorePending=false;target=null;Before=null;After=null;
  try{
   await Same(g,ct);Publish(AppText.T("Creo il backup iniziale prima del test."));original=await Backup(ct);Guard(g);
   Before=await io.GuidedAsync(g,Publish,ct);Guard(g);var drift=Before.Overall;
   if(drift.Kind==DriftKind.Insufficient){Publish(AppText.T("Test non valido: campioni insufficienti."));return;}
   if(drift.Kind==DriftKind.Normal){Publish(AppText.T("Nessun difetto rilevato nei periodi osservati. Nessuna modifica."));return;}
   if(drift.Kind==DriftKind.StableOffset&&Before.Rest.RightNormal&&Before.Cycles.All(r=>r.RightNormal)){Publish(AppText.T("Scostamento stabile: tentativo temporaneo del solo centro sinistro."));changed=true;var attempt=await CenterCorrector.CorrectAsync(original,drift,this,ct);Guard(g);if(!attempt.Converged){changed=false;Publish(AppText.T("Nessun miglioramento stabile. Calibrazione originale ripristinata."));return;}target=attempt.Data;}
   else{
    if(!await io.ConfirmAsync(AppText.T("Rilevato tremolio, picchi, centro instabile o un problema anche allo stick destro. La calibrazione non ripara l'usura. Vuoi provare UNA calibrazione temporanea del centro di ENTRAMBI gli stick? Lasciali entrambi liberi."),ct)){Publish(AppText.T("Tentativo non eseguito. Nessuna modifica."));return;}
    Guard(g);changed=true;await device.CalibrateCenterAsync(ct);target=await device.ReadCalibrationAsync(ct);
   }
   await Same(g,ct);Publish(AppText.T("Ripeto il test completo con gli stessi criteri."));After=await io.GuidedAsync(g,Publish,ct);Guard(g);
   var range=await io.RangeAsync(Publish,ct);Guard(g);var verification=VerificationPolicy.Evaluate(Before,After,range);
   if(!verification.CanSave){using var recovery=new CancellationTokenSource(TimeSpan.FromSeconds(8));await device.WriteTemporaryAsync(original,recovery.Token);changed=false;target=null;var partial=Math.Abs(After.Overall.Axes[1].Median)<Math.Abs(Before.Overall.Axes[1].Median);Publish((partial?AppText.T("Miglioramento parziale"):AppText.T("Nessun miglioramento verificato"))+AppText.T(". Calibrazione originale ripristinata; salvataggio disabilitato."));return;}
   await Same(g,ct);if(!(await device.ReadCalibrationAsync(ct)).SameAs(target!))throw new IOException(AppText.T("Calibrazione cambiata durante la verifica"));Guard(g);State.Verified=true;verifiedAt=clock.GetTimestamp();changed=false;Publish(verification.Message,1);
  }catch(Exception e){State.Verified=false;State.RestorePending=false;target=null;if(changed&&original!=null&&State.Connected){try{using var recovery=new CancellationTokenSource(TimeSpan.FromSeconds(8));await device.WriteTemporaryAsync(original,recovery.Token);Publish(AppText.T("Test interrotto. Ripristino temporaneo verificato."));}catch(Exception rollback){throw new IOException(AppText.T("Operazione fallita e ripristino NON verificato. Ricollega il controller; nessun salvataggio abilitato."),new AggregateException(e,rollback));}}throw;}
  finally{End();}
 }
 public async Task RestoreAsync(CalibrationBackup backup,CancellationToken ct)
 {
  BackupValidator.Validate(backup,identity);long g=Begin();State.Verified=false;State.RestorePending=false;target=null;
  try{
   if(!await io.ConfirmAsync(AppText.Format("Ripristinare TEMPORANEAMENTE il backup del {0:g}? Non è un reset di fabbrica. Potrebbe riportare il vecchio drift.",backup.CreatedUtc.ToLocalTime()),ct))return;
   await Same(g,ct);await Backup(ct);Guard(g);await device.WriteTemporaryAsync(backup.Data,ct);Guard(g);target=backup.Data;State.RestorePending=true;verifiedAt=clock.GetTimestamp();Publish(AppText.T("Restore temporaneo verificato. Usa Salva Restore per renderlo permanente, oppure Riavvia per scartarlo."),1);
  }finally{End();}
 }
 public async Task SaveAsync(CancellationToken ct)
 {
  if(!State.CanSave)throw new InvalidOperationException(AppText.T("Serve un test positivo recente o un Restore verificato"));CurrentProof();
  long g=Begin();bool restored=State.RestorePending;
  try{
   if(io.Battery.Error)throw new InvalidOperationException(AppText.T("Stato batteria anomalo: scrittura bloccata"));
   string warning=restored?AppText.T("Salvare il RESTORE nel controller? Il backup può contenere il vecchio drift. "):AppText.T("Salvare la nuova calibrazione nel controller? ");
   warning+=AppText.T("Mantieni il cavo USB e la batteria INTERNA collegati. Confermi che l'alimentazione è stabile? Non scollegare o chiudere durante la scrittura.");
   if(!await io.ConfirmAsync(warning,ct))return;await Same(g,ct);var fresh=await io.FreshAsync(TimeSpan.FromSeconds(.5),ct);Guard(g);
   if(fresh.Count<32||io.Battery.Error)throw new IOException(AppText.T("Dati freschi/alimentazione non validi"));
   if(!(await device.ReadCalibrationAsync(ct)).SameAs(target!))throw new IOException(AppText.T("Parametri diversi dalla calibrazione verificata"));await Backup(ct);Guard(g);ct.ThrowIfCancellationRequested();
   CurrentProof();State.Verified=false;State.RestorePending=false;State.IsPersistentWrite=true;Publish(AppText.T("Salvataggio permanente in corso. NON scollegare il controller."));
   using var commit=new CancellationTokenSource(TimeSpan.FromSeconds(45));await device.SavePermanentAsync(commit.Token);Guard(g);
   Publish(AppText.T("Memoria bloccata. Riavvio e verifica del salvataggio..."));io.Rebooting=true;await device.RebootAsync(commit.Token);
   using var reconnect=CancellationTokenSource.CreateLinkedTokenSource(commit.Token);reconnect.CancelAfter(TimeSpan.FromSeconds(15));var result=await io.ReconnectAsync(identity,reconnect.Token);if(!result.ResetObserved)throw new IOException(AppText.T("Riavvio fisico non osservato: salvataggio non verificato"));device=result.Controller;
   if(await device.ReadIdentityAsync(commit.Token)!=identity||!(await device.ReadCalibrationAsync(commit.Token)).SameAs(target!)||await device.QueryNvStatusAsync(commit.Token)!=NvStatus.Locked)throw new IOException(AppText.T("Rilettura dopo riavvio non corrispondente"));Guard(g);
   var saved=CalibrationBackup.Create(identity,target!,restored?"Restored":"Saved");await repository.SaveAsync(saved,commit.Token);LatestBackup=saved;Interlocked.Increment(ref generation);target=null;Publish(AppText.T("Salvataggio confermato dopo riavvio e rilettura. Prova ora il controller nel gioco."),1);
  }catch{State.Verified=false;State.RestorePending=false;if(State.IsPersistentWrite){State.Connected=false;Publish(AppText.T("Salvataggio INCERTO: non eseguire altre scritture. Ricollega e verifica il controller."));}throw;}
  finally{io.Rebooting=false;End();}
 }
 public async Task RebootAsync(CancellationToken ct)
 {
  long g=Begin();State.Verified=false;State.RestorePending=false;target=null;
  try{await Same(g,ct);Publish(AppText.T("Riavvio: le modifiche temporanee verranno scartate."));io.Rebooting=true;await device.RebootAsync(ct);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(15));var result=await io.ReconnectAsync(identity,timeout.Token);if(!result.ResetObserved)throw new IOException(AppText.T("Riavvio fisico non osservato"));device=result.Controller;if(await device.ReadIdentityAsync(timeout.Token)!=identity)throw new IOException(AppText.T("Controller diverso dopo riavvio"));Guard(g);Interlocked.Increment(ref generation);Publish(AppText.T("Stesso controller riconnesso. Esegui un nuovo test."));}
  catch{State.Invalidate();throw;}finally{io.Rebooting=false;End();}
 }
 public Task WriteTemporaryAsync(CalibrationData data,CancellationToken ct)=>device.WriteTemporaryAsync(data,ct);
 public Task<IReadOnlyList<StickSample>> GetFreshSamplesAsync(TimeSpan duration,CancellationToken ct)=>io.FreshAsync(duration,ct);
}
