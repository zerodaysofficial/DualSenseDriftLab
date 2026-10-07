namespace DriftLab.Core;
public sealed class GuidedTest(long generation)
{
 private int phase,cycle;private TimeSpan? since;private readonly List<StickSample> samples=[];private DriftResult? rest;private readonly List<DriftResult> cycles=[];
 private readonly string[] directions=[AppText.T("basso"),AppText.T("basso"),AppText.T("basso"),AppText.T("alto"),AppText.T("destra"),AppText.T("sinistra")];
 public bool Failed{get;private set;}public bool RetryNeeded{get;private set;}public GuidedTestResult? Result{get;private set;}
 public string Instruction=>Failed?AppText.T("Mancato rientro entro 15 secondi: test non valido."):RetryNeeded?AppText.T("Movimento o dati insufficienti. Ripeti il ciclo."):Result!=null?AppText.T("Test completato."):phase==0?AppText.T("Lascia entrambi gli stick liberi per 10 secondi."):phase==1?AppText.Format("Porta lo stick SINISTRO tutto in {0} ({1}/6).",directions[cycle],cycle+1):phase==2?AppText.T("Rilascia lo stick senza toccarlo."):AppText.T("Non toccare gli stick: misuro il rientro per 8 secondi.");
 public double Progress=>Result!=null?1:Math.Clamp((cycle+(phase==0?0:.1))/7.0,0,1);
 public void Retry(){if(RetryNeeded){RetryNeeded=false;phase=cycle==0&&rest==null?0:1;since=null;samples.Clear();}}
 public void Accept(StickSample s)
 {
  if(Failed||RetryNeeded||Result!=null)return;since??=s.Time;double elapsed=(s.Time-since.Value).TotalSeconds;
  switch(phase){
   case 0:
    if(Math.Abs(s.LX)>.25||Math.Abs(s.LY)>.25||Math.Abs(s.RX)>.25||Math.Abs(s.RY)>.25){RetryNeeded=true;return;}
    samples.Add(s);if(elapsed>=10){rest=DriftAnalyzer.Analyze(samples,new(Duration:10));if(rest.Kind==DriftKind.Insufficient){RetryNeeded=true;rest=null;return;}phase=1;since=null;samples.Clear();}break;
   case 1:
    bool moved=cycle<3?s.LY>=.8:cycle==3?s.LY<=-.8:cycle==4?s.LX>=.8:s.LX<=-.8;
    if(moved){phase=2;since=s.Time;}break;
   case 2:
    if(Math.Abs(s.LX)<=.25&&Math.Abs(s.LY)<=.25){phase=3;since=s.Time;}
    else if(elapsed>=15)Failed=true;break;
   case 3:
    if(Math.Abs(s.LX)>.25||Math.Abs(s.LY)>.25){RetryNeeded=true;break;}
    if(elapsed>=.5){phase=4;since=s.Time;samples.Clear();samples.Add(s);}break;
   case 4:
    if(Math.Abs(s.LX)>.25||Math.Abs(s.LY)>.25){RetryNeeded=true;break;}
    samples.Add(s);if(elapsed>=8){var r=DriftAnalyzer.Analyze(samples);if(r.Kind==DriftKind.Insufficient){RetryNeeded=true;break;}cycles.Add(r);cycle++;phase=1;since=null;samples.Clear();if(cycle==6)Result=new(generation,rest!,cycles.ToArray());}break;
  }
 }
}
public sealed class RangeCapture
{
 private readonly double[] min=Enumerable.Repeat(1d,4).ToArray(),max=Enumerable.Repeat(-1d,4).ToArray();private TimeSpan? center,last;private int count;
 public bool Passed{get;private set;}
 public void Accept(StickSample s){if(!s.Finite)return;var a=s.Axes;for(int i=0;i<4;i++){min[i]=Math.Min(min[i],a[i]);max[i]=Math.Max(max[i],a[i]);}
  bool full=min.All(v=>v<=-.95)&&max.All(v=>v>=.95);bool ok=full&&a.All(v=>Math.Abs(v)<=SignalLimits.Center)&&(last==null||(s.Time-last.Value).TotalSeconds<=.25);last=s.Time;
  if(!ok){center=null;count=0;return;}center??=s.Time;count++;Passed=count>=20&&(s.Time-center.Value).TotalSeconds>=.5;
 }
}
