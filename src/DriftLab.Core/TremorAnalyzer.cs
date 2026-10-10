namespace DriftLab.Core;
public sealed record TremorAxisReport(double Center,double Oscillation,double Peak,int Spikes,int RapidChanges);
public sealed record TremorReport(bool Valid,DriftKind Kind,TremorAxisReport[] Axes,int Samples)
{
 public bool HasTremor=>Valid&&Axes.Any(a=>a.Oscillation>SignalLimits.MicroSpread+1e-10||a.Spikes>0||a.RapidChanges>0);
 public bool HasOffset=>Valid&&Axes.Any(a=>Math.Abs(a.Center)>SignalLimits.Center+1e-10);
 public bool HasInstability=>HasTremor||HasOffset||Kind==DriftKind.Progressive;
 public double LeftOscillation=>Axes.Take(2).Max(a=>a.Oscillation);
 public double RightOscillation=>Axes.Skip(2).Max(a=>a.Oscillation);
 public int SpikeCount=>Axes.Sum(a=>a.Spikes);
}
public static class TremorAnalyzer
{
 public static TremorReport Analyze(IReadOnlyList<StickSample> samples,TimeSpan duration)
 {
  TremorReport Invalid()=>new(false,DriftKind.Insufficient,[],samples.Count);
  if(duration.TotalSeconds<8||samples.Count<350||samples.Any(s=>!s.Finite||s.Axes.Any(a=>Math.Abs(a)>.3)))return Invalid();
  double elapsed=(samples[^1].Time-samples[0].Time).TotalSeconds;
  if(elapsed<duration.TotalSeconds-.03)return Invalid();
  var result=DriftAnalyzer.Analyze(samples,new(Duration:duration.TotalSeconds,Minimum:350));
  if(result.Kind==DriftKind.Insufficient)return Invalid();
  var axes=new TremorAxisReport[4];
  for(int axis=0;axis<4;axis++)
  {
   int rapid=0;
   for(int i=1;i<samples.Count;i++)
   {
    if(Math.Abs(samples[i].Axes[axis]-samples[i-1].Axes[axis])>SignalLimits.MicroSpread+1e-10)rapid++;
   }
   var s=result.Axes[axis];
   axes[axis]=new(s.Median,s.Spread,s.PeakDeviation,s.SpikeCount,rapid);
  }
  return new(true,result.Kind,axes,samples.Count);
 }
}
