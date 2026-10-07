using DriftLab.Core;
namespace DriftLab.Desktop;
internal static class Program
{
 [STAThread]private static void Main()
 {
  ApplicationConfiguration.Initialize();Application.ThreadException+=(_,e)=>MessageBox.Show(e.Exception.Message,"DualSense Drift Lab",MessageBoxButtons.OK,MessageBoxIcon.Error);
  using var language=new LanguageDialog();if(language.ShowDialog()!=DialogResult.OK)return;
  AppText.Select(language.SelectedLanguage);Application.Run(new MainForm());
 }
}
