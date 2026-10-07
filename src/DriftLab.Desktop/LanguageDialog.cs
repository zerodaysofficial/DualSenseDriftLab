using DriftLab.Core;
namespace DriftLab.Desktop;
internal sealed class LanguageDialog:Form
{
 private readonly RadioButton english=new(){Text="English",Checked=true,Dock=DockStyle.Fill};
 private readonly RadioButton italian=new(){Text="Italiano",Dock=DockStyle.Fill};
 public AppLanguage SelectedLanguage=>italian.Checked?AppLanguage.Italian:AppLanguage.English;
 public LanguageDialog()
 {
  Text="DualSense Drift Lab · Select your language";ClientSize=new(520,330);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
  Font=new("Segoe UI",11);BackColor=Color.FromArgb(12,18,28);ForeColor=Color.Gainsboro;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new(20)};
  foreach(float height in new[]{48f,36f,38f,38f,50f,60f})layout.RowStyles.Add(new(SizeType.Absolute,height));
  layout.Controls.Add(new Label{Text="DualSense Drift Lab",Font=new("Segoe UI Semibold",21),Dock=DockStyle.Fill});
  layout.Controls.Add(new Label{Text="Select your language",Dock=DockStyle.Fill});layout.Controls.Add(english);layout.Controls.Add(italian);
  var proceed=new Button{Text="Continue",DialogResult=DialogResult.OK,Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,43,57),ForeColor=ForeColor,FlatStyle=FlatStyle.Flat};layout.Controls.Add(proceed);
  layout.Controls.Add(new Label{Text="Made by zer0day\nProtocol reference: dualshock-tools · the_al",Dock=DockStyle.Fill,Font=new("Segoe UI",9),Padding=new(0,8,0,0)});
  Controls.Add(layout);AcceptButton=proceed;ActiveControl=english;
 }
}
