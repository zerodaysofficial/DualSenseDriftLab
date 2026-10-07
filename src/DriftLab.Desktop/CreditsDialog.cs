using System.Diagnostics;
using DriftLab.Core;
namespace DriftLab.Desktop;
internal sealed class CreditsDialog:Form
{
 public CreditsDialog()
 {
  Text=AppText.T("Crediti");ClientSize=new(650,340);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;Font=new("Segoe UI",10);BackColor=Color.FromArgb(12,18,28);ForeColor=Color.Gainsboro;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new(20)};
  layout.RowStyles.Add(new(SizeType.Absolute,54));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,36));layout.RowStyles.Add(new(SizeType.Absolute,42));
  layout.Controls.Add(new Label{Text="Made by zer0day",Font=new("Segoe UI Semibold",21),Dock=DockStyle.Fill});
  layout.Controls.Add(new Label{Text=AppText.T("Riferimento del protocollo di calibrazione DualSense:\ndualshock-tools · the_al\nLicenza MIT; attribuzione e testo completo in THIRD_PARTY_NOTICES.md.\n\nLa calibrazione può correggere il centro; non ripara un sensore usurato."),Dock=DockStyle.Fill});
  var link=new LinkLabel{Text="github.com/dualshock-tools/dualshock-tools.github.io",Dock=DockStyle.Fill,LinkColor=Color.Turquoise,VisitedLinkColor=Color.Turquoise};
  link.LinkClicked+=(_,_)=>{try{Process.Start(new ProcessStartInfo("https://github.com/dualshock-tools/dualshock-tools.github.io"){UseShellExecute=true});}catch(Exception e){MessageBox.Show(this,e.Message,AppText.T("Operazione non conclusa"),MessageBoxButtons.OK,MessageBoxIcon.Error);}};layout.Controls.Add(link);
  var close=new Button{Text=AppText.T("Chiudi"),DialogResult=DialogResult.OK,Dock=DockStyle.Fill};layout.Controls.Add(close);Controls.Add(layout);AcceptButton=close;CancelButton=close;
 }
}
internal sealed class ConfirmationDialog:Form
{
 public ConfirmationDialog(string message)
 {
  Text=AppText.T("Conferma operazione");ClientSize=new(670,270);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Dpi;Font=new("Segoe UI",11);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new(18)};layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,46));layout.Controls.Add(new Label{Text=message,Dock=DockStyle.Fill});
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text=AppText.T("No"),DialogResult=DialogResult.No,AutoSize=true};var yes=new Button{Text=AppText.T("Sì"),DialogResult=DialogResult.Yes,AutoSize=true};actions.Controls.Add(no);actions.Controls.Add(yes);layout.Controls.Add(actions);Controls.Add(layout);AcceptButton=no;CancelButton=no;ActiveControl=no;
 }
}
