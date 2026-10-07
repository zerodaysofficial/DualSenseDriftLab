namespace DriftLab.Core;
public static class DriftAnalyzer
{
 public static double Percentile(IEnumerable<double> values,double p){var a=values.Order().ToArray();if(a.Length==0)return double.NaN;double k=(a.Length-1)*p;int i=(int)k;return a[i]+(a[Math.Min(i+1,a.Length-1)]-a[i])*(k-i);}
 public static DriftResult Analyze(IReadOnlyList<StickSample> samples,AnalysisOptions? options=null)
 {
  var o=options??new();var empty=new DriftResult(DriftKind.Insufficient,Enumerable.Repeat(new AxisStats(double.NaN,double.NaN,double.NaN),4).ToArray(),samples.Count);
  if(samples.Count<o.Minimum||samples.Any(s=>!s.Finite))return empty;
  double start=samples[0].Time.TotalSeconds,end=samples[^1].Time.TotalSeconds;
  if(end-start<o.Duration-.02)return empty;
  for(int i=1;i<samples.Count;i++){double gap=(samples[i].Time-samples[i-1].Time).TotalSeconds;if(gap<=0||gap>o.MaxGap)return empty;}
  var first=samples.Where(s=>s.Time.TotalSeconds<=start+1).ToArray();var last=samples.Where(s=>s.Time.TotalSeconds>=end-1).ToArray();
  if(first.Length<2||last.Length<2)return empty;
  var stats=Enumerable.Range(0,4).Select(a=>{
   var values=samples.Select(s=>s.Axes[a]).ToArray();double median=Percentile(values,.5);
   return new AxisStats(median,Percentile(last.Select(s=>s.Axes[a]),.5)-Percentile(first.Select(s=>s.Axes[a]),.5),Percentile(values,.95)-Percentile(values,.05),values.Max(v=>Math.Abs(v-median)),values.Count(v=>Math.Abs(v-median)>=SignalLimits.SpikeDeviation));
  }).ToArray();
  DriftKind k=stats.Any(s=>Math.Abs(s.Delta)>o.Delta+1e-10)?DriftKind.Progressive:
   stats.Any(s=>s.SpikeCount>0&&s.SpikeCount<=samples.Count*.05)?DriftKind.Spikes:
   stats.Any(s=>s.Spread>o.Spread+1e-10)?DriftKind.Noisy:
   stats.Any(s=>s.Spread>o.MicroSpread+1e-10)?DriftKind.MicroTremor:
   stats.Any(s=>s.SpikeCount>0)?DriftKind.Spikes:
   stats.Any(s=>Math.Abs(s.Median)>o.Center+1e-10)?DriftKind.StableOffset:DriftKind.Normal;
  return new(k,stats,samples.Count);
 }
 public static DriftResult Combine(IReadOnlyList<DriftResult> results)
 {
  if(results.Count==0)return Analyze([]);
  var stats=Enumerable.Range(0,4).Select(i=>new AxisStats(Percentile(results.Select(r=>r.Axes[i].Median),.5),results.Max(r=>Math.Abs(r.Axes[i].Delta)),results.Max(r=>r.Axes[i].Spread),results.Max(r=>r.Axes[i].PeakDeviation),results.Sum(r=>r.Axes[i].SpikeCount))).ToArray();
  foreach(var k in new[]{DriftKind.Insufficient,DriftKind.Progressive,DriftKind.Spikes,DriftKind.Noisy,DriftKind.MicroTremor,DriftKind.DirectionDependent})if(results.Any(r=>r.Kind==k))return new(k,stats,results.Sum(r=>r.Count));
  bool direction=Enumerable.Range(0,4).Any(i=>results.Max(r=>r.Axes[i].Median)-results.Min(r=>r.Axes[i].Median)>SignalLimits.Direction+1e-10);
  return new(direction?DriftKind.DirectionDependent:results.Any(r=>r.Kind==DriftKind.StableOffset)?DriftKind.StableOffset:DriftKind.Normal,stats,results.Sum(r=>r.Count));
 }
}
