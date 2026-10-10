using System.Threading.Channels;
using DriftLab.Core;
namespace DriftLab.Protocol;
public static class SampleAcquisition
{
 public static async Task<GuidedTestResult> GuidedAsync(ChannelReader<StickSample> reader,long generation,Action<string,double> progress,Func<string,CancellationToken,Task<bool>> confirm,CancellationToken token)
 {
  var test=new GuidedTest(generation);TimeSpan? last=null;progress(test.Instruction,0);
  await foreach(var s in reader.ReadAllAsync(token)){
   test.Accept(s);if(test.Failed)throw new IOException(test.Instruction);
   if(test.RetryNeeded){if(!await confirm(AppText.T("Ciclo non valido: hai mosso lo stick o mancano campioni. Vuoi ripetere questo ciclo?"),token))throw new OperationCanceledException(token);while(reader.TryRead(out _)){}test.Retry();}
   if(last==null||s.Time-last.Value>=TimeSpan.FromMilliseconds(100)){progress(test.Instruction,test.Progress);last=s.Time;}
   if(test.Result!=null)return test.Result;
  }throw new IOException(AppText.T("Test interrotto"));
 }
}
