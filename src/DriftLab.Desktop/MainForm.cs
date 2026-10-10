using DriftLab.Core;
using DriftLab.Protocol;
using DriftLab.Storage;
using DriftLab.WindowsHid;
namespace DriftLab.Desktop;
public sealed class MainForm:Form
{
 private readonly Label connection=new(),instruction=new(),metrics=new();private readonly ProgressBar progress=new();private readonly StickView left=new(false),right=new(true);private readonly TimelineView timeline=new();private readonly FlowLayoutPanel buttons=new();
 private readonly Button scan=new(),test=new(),hard=new(),save=new(),restore=new(),reboot=new(),export=new(),import=new(),cancel=new(),credits=new();private readonly System.Windows.Forms.Timer tick=new(){Interval=33};
 private readonly BackupRepository repository=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DualSenseDriftLab","Backups"));
 private readonly LiveController live;private ControllerSession? session;private CancellationTokenSource? operation;private bool working,searching,closing;private int searchTicks;
 public MainForm()
 {
  Text="DualSense Drift Lab · USB · Beta 3";ClientSize=new(1080,930);MinimumSize=new(1080,650);AutoScaleMode=AutoScaleMode.Dpi;Font=new("Segoe UI",10);BackColor=Color.FromArgb(12,18,28);ForeColor=Color.Gainsboro;
  live=new(Confirm);live.Disconnected+=()=>{session?.Invalidate();Ui(()=>{connection.Text=AppText.T("Controller scollegato");RefreshButtons();});};
  var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
  var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,RowCount=9,Padding=new(20)};
  foreach(float height in new[]{42f,54f,92f,260f,140f,155f,30f})layout.RowStyles.Add(new(SizeType.Absolute,height));
  layout.RowStyles.Add(new(SizeType.Absolute,100));
  layout.RowStyles.Add(new(SizeType.Absolute,28));
  var title=new Label{Text="DualSense Drift Lab",Font=new("Segoe UI Semibold",22),Dock=DockStyle.Fill};layout.Controls.Add(title);
  connection.Text=AppText.T("Collega un DualSense standard con cavo USB dati");connection.Dock=DockStyle.Fill;layout.Controls.Add(connection);
  instruction.Text=AppText.T("Test reale del rientro dello stick sinistro. Le modifiche automatiche sono temporanee; il salvataggio permanente richiede conferma.");instruction.Dock=DockStyle.Fill;instruction.Font=new("Segoe UI",12);layout.Controls.Add(instruction);
  var views=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};views.ColumnStyles.Add(new(SizeType.Percent,50));views.ColumnStyles.Add(new(SizeType.Percent,50));views.Controls.Add(left,0,0);views.Controls.Add(right,1,0);layout.Controls.Add(views);layout.Controls.Add(timeline);
  metrics.Dock=DockStyle.Fill;metrics.Text=AppText.T("Test preciso di entrambi gli stick: centro ±0,78% · oscillazione oltre un passo USB\nRileva deriva lenta, microtremolii, rumore e picchi isolati. Nessun test ancora eseguito.\nUn valore USB varia a passi di 0,78%; lo zero esatto non è rappresentabile in un singolo campione.");layout.Controls.Add(metrics);progress.Dock=DockStyle.Fill;layout.Controls.Add(progress);
  buttons.Dock=DockStyle.Fill;buttons.WrapContents=true;layout.Controls.Add(buttons);scroll.Controls.Add(layout);Controls.Add(scroll);
  layout.Controls.Add(new Label{Text="Made by zer0day · "+AppText.T("Protocollo: dualshock-tools · the_al"),Dock=DockStyle.Fill,Font=new("Segoe UI",9)});
  Add(scan,AppText.T("Cerca controller"),async()=>await Discover());Add(test,AppText.T("Test e correzione automatica"),()=>Run(ct=>session!.RunAutoAsync(ct)));Add(hard,AppText.T("Rimuovi tremolio · Hard Detect"),()=>Run(ct=>session!.RunHardDetectAsync(ct)));Add(save,AppText.T("Salva nel controller"),()=>Run(ct=>session!.SaveAsync(ct)));Add(restore,AppText.T("Restore"),()=>Run(RestoreHistory));Add(reboot,AppText.T("Riavvia controller"),()=>Run(ct=>session!.RebootAsync(ct)));Add(export,AppText.T("Esporta backup"),()=>Run(Export));Add(import,AppText.T("Importa backup"),()=>Run(Import));Add(cancel,AppText.T("Annulla test"),()=>{if(session?.State.CanCancel==true)operation?.Cancel();return Task.CompletedTask;});
  Add(credits,AppText.T("Crediti"),()=>{using var dialog=new CreditsDialog();dialog.ShowDialog(this);return Task.CompletedTask;});
  Shown+=async(_,_)=>{tick.Start();await Discover();};tick.Tick+=async(_,_)=>{var s=live.Latest;left.UpdateSample(s);right.UpdateSample(s);timeline.UpdateSample(s);RefreshButtons();if(++searchTicks>=60){searchTicks=0;if(!working&&!searching&&!closing&&!live.IsAlive)await Discover();}};
  FormClosing+=OnClosing;
 }
 private void Add(Button b,string text,Func<Task> action){b.Text=text;b.AutoSize=true;b.FlatStyle=FlatStyle.Flat;b.Margin=new(2,3,6,3);b.BackColor=Color.FromArgb(24,43,57);b.ForeColor=ForeColor;b.Click+=async(_,_)=>{try{await action();}catch(Exception e){ShowError(e);}};buttons.Controls.Add(b);}
 private void Ui(Action action){if(IsDisposed||closing)return;if(InvokeRequired)BeginInvoke(action);else action();}
 private Task<bool> Confirm(string text,CancellationToken ct){var tcs=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);Ui(()=>{if(ct.IsCancellationRequested){tcs.TrySetCanceled(ct);return;}using var dialog=new ConfirmationDialog(text);tcs.TrySetResult(dialog.ShowDialog(this)==DialogResult.Yes);});return tcs.Task.WaitAsync(ct);}
 private void RefreshButtons(){var state=session?.State;bool ready=state?.Connected==true&&!working&&!searching&&!closing;scan.Enabled=!working&&!searching;test.Enabled=ready;hard.Enabled=ready;save.Enabled=ready&&state!.CanSave;save.Text=state?.RestorePending==true?AppText.T("Salva Restore"):AppText.T("Salva nel controller");restore.Enabled=ready&&state!.CanRestore;reboot.Enabled=ready&&state!.CanReboot;import.Enabled=ready;export.Enabled=ready&&session?.LatestBackup!=null;cancel.Enabled=working&&state?.CanCancel==true;credits.Enabled=!working&&!searching&&!closing;}
 private async Task Discover()
 {
  if(searching||working||closing)return;searching=true;RefreshButtons();
  try{
   if(session?.State.Connected==true&&live.IsAlive)return;session?.Invalidate();session=null;await live.CloseAsync();var devices=await Task.Run(HidDiscovery.FindDualSenseUsb);
   if(devices.Count==0){connection.Text=AppText.T("Nessun DualSense USB trovato. Usa un cavo dati; chiudi eventuali app che occupano il controller.");return;}
   HidDeviceInfo? selected=devices[0];if(devices.Count>1){using var dialog=new SelectionDialog<HidDeviceInfo>(AppText.T("Scegli l'interfaccia DualSense"),devices,x=>x.ToString());selected=dialog.ShowDialog(this)==DialogResult.OK?dialog.Selected:null;}if(selected==null)return;
   using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));var controller=await live.OpenAsync(selected,timeout.Token);
   try{var id=await controller.ReadIdentityAsync(timeout.Token);session=new(controller,id,repository,live);session.Updated+=snapshot=>Ui(()=>Apply(snapshot));connection.Text=$"DualSense USB · {id.Serial} · FW 0x{id.Firmware:X8} · HW 0x{id.Hardware:X8}";instruction.Text=AppText.T("Pronto. Premi Test e correzione automatica e segui le istruzioni.");timeline.Clear();}
   catch(Exception e){connection.Text=AppText.T("USB rilevato: monitoraggio disponibile, calibrazione bloccata");instruction.Text=e.Message;}
  }catch(Exception e){connection.Text=AppText.T("Connessione USB non disponibile");instruction.Text=e.Message;}finally{searching=false;RefreshButtons();}
 }
 private void Apply(SessionSnapshot s)
 {
  instruction.Text=s.Message;progress.Value=(int)(100*Math.Clamp(s.Progress,0,1));
  string Describe(GuidedTestResult? r){
   if(r==null)return "—";var v=r.Overall;string Center(int i)=>$"X {v.Axes[i].Median*100:+0.00;-0.00;0.00}% Y {v.Axes[i+1].Median*100:+0.00;-0.00;0.00}%";
   return AppText.Format("{0} · centro SX {1} · DX {2}",KindName(v.Kind),Center(0),Center(2))+"\n"+AppText.Format("Deriva SX {0:0.00}% / DX {1:0.00}% · oscillazione SX {2:0.00}% / DX {3:0.00}% · picco {4:0.00}% · campioni con picchi {5}",v.Axes.Take(2).Max(a=>Math.Abs(a.Delta))*100,v.Axes.Skip(2).Max(a=>Math.Abs(a.Delta))*100,v.Axes.Take(2).Max(a=>a.Spread)*100,v.Axes.Skip(2).Max(a=>a.Spread)*100,v.Axes.Max(a=>a.PeakDeviation)*100,v.Axes.Sum(a=>a.SpikeCount));
  }
  var initial=session?.HardBefore;var checkedAfter=session?.HardAfter;
  string Detect(TremorReport r)=>AppText.Format("SX {0:0.00}% · DX {1:0.00}% · picchi {2}",r.LeftOscillation*100,r.RightOscillation*100,r.SpikeCount);
  var hardDetails=initial==null?"":AppText.Format("Hard Detect (segnale USB senza filtri) · prima: {0} · dopo: {1}\n",Detect(initial),checkedAfter==null?"—":Detect(checkedAfter));
  metrics.Text=hardDetails+AppText.Format("Prima: {0}\nDopo: {1}\nValori reali senza filtro. Oscillazione = ampiezza P5–P95; picco = distanza dalla mediana. Alto = Y negativo.",Describe(s.Before),Describe(s.After));RefreshButtons();
 }
 private static string KindName(DriftKind k)=>k switch{DriftKind.Normal=>AppText.T("Nessun difetto rilevato"),DriftKind.StableOffset=>AppText.T("Scostamento stabile"),DriftKind.Progressive=>AppText.T("Movimento progressivo"),DriftKind.Noisy=>AppText.T("Rumore significativo"),DriftKind.MicroTremor=>AppText.T("Microtremolio"),DriftKind.Spikes=>AppText.T("Picchi intermittenti"),DriftKind.DirectionDependent=>AppText.T("Centro dipendente dal rilascio"),_=>AppText.T("Test non valido")};
 private async Task Run(Func<CancellationToken,Task> action){if(working||searching||session==null)return;working=true;operation=new();RefreshButtons();try{await action(operation.Token);}catch(OperationCanceledException){instruction.Text=AppText.T("Operazione annullata. Ripristino temporaneo tentato se necessario.");}catch(Exception e){ShowError(e);}finally{operation.Dispose();operation=null;working=false;RefreshButtons();}}
 private void ShowError(Exception e){instruction.Text=e.Message;MessageBox.Show(this,e.Message,AppText.T("Operazione non conclusa"),MessageBoxButtons.OK,MessageBoxIcon.Error);}
 private static string BackupKind(string kind)=>kind switch{"Initial"=>AppText.T("Iniziale"),"Saved"=>AppText.T("Salvato"),"Restored"=>AppText.T("Ripristinato"),_=>kind};
 private async Task RestoreHistory(CancellationToken ct){var list=await repository.ListAsync(session!.Identity,ct);if(list.Count==0){instruction.Text=AppText.T("Nessun backup conservato per questo controller.");return;}using var dialog=new SelectionDialog<CalibrationBackup>(AppText.T("Restore: scegli un backup"),list,b=>$"{b.CreatedUtc.ToLocalTime():g} · {BackupKind(b.Kind)} · {b.Id[..8]}");if(dialog.ShowDialog(this)==DialogResult.OK&&dialog.Selected!=null)await session.RestoreAsync(dialog.Selected,ct);}
 private async Task Export(CancellationToken ct){var b=session!.LatestBackup;if(b==null)return;using var dialog=new SaveFileDialog{Filter=AppText.T("Backup calibrazione JSON|*.json"),FileName=$"DualSense_Backup_{b.CreatedUtc:yyyyMMdd_HHmmss}.json"};if(dialog.ShowDialog(this)==DialogResult.OK){await BackupRepository.ExportAsync(b,dialog.FileName,ct);instruction.Text=AppText.T("Backup esportato.");}}
 private async Task Import(CancellationToken ct){using var dialog=new OpenFileDialog{Filter=AppText.T("Backup calibrazione JSON|*.json")};if(dialog.ShowDialog(this)!=DialogResult.OK)return;var b=await repository.ReadAsync(dialog.FileName,ct);BackupValidator.Validate(b,session!.Identity);await repository.SaveAsync(b,ct);await session.RestoreAsync(b,ct);}
 private async void OnClosing(object? sender,FormClosingEventArgs e){if(closing)return;if(session?.State.IsPersistentWrite==true){e.Cancel=true;instruction.Text=AppText.T("Salvataggio permanente in corso: attendi il completamento prima di chiudere.");return;}e.Cancel=true;operation?.Cancel();if(working){instruction.Text=AppText.T("Annullamento e ripristino in corso. Chiudi quando l'operazione termina.");return;}closing=true;tick.Stop();await live.DisposeAsync();Close();}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x219&&!working&&!searching&&!closing&&IsHandleCreated)BeginInvoke(async()=>await Discover());}
}
internal sealed class SelectionDialog<T>:Form where T:class
{
 private readonly ListBox list=new(){Dock=DockStyle.Fill};public T? Selected=>list.SelectedItem is Entry e?e.Value:null;
 private sealed record Entry(T Value,string Text){public override string ToString()=>Text;}
 public SelectionDialog(string title,IReadOnlyList<T> choices,Func<T,string> text){Text=title;Size=new(740,300);StartPosition=FormStartPosition.CenterParent;var select=new Button{Text=AppText.T("Seleziona"),Dock=DockStyle.Bottom,DialogResult=DialogResult.OK,Height=38};Controls.Add(list);Controls.Add(select);foreach(var c in choices)list.Items.Add(new Entry(c,text(c)));if(list.Items.Count>0)list.SelectedIndex=0;AcceptButton=select;}
}
