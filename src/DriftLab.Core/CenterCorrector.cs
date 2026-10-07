namespace DriftLab.Core;
public static class CenterCorrector
{
 public static async Task<CorrectionAttempt> CorrectAsync(CalibrationData original,DriftResult stable,ICalibrationSession session,CancellationToken ct)
 {
  if(!stable.Stable)throw new InvalidOperationException(AppText.T("Il segnale non è stabile"));var current=original;int writes=0;bool success=false;
  async Task Write(CalibrationData data){await session.WriteTemporaryAsync(data,ct);writes++;current=data;}
  async Task<DriftResult> Measure(){var s=await session.GetFreshSamplesAsync(TimeSpan.FromSeconds(.5),ct);var r=DriftAnalyzer.Analyze(s,new(Duration:.5,Minimum:32));if(!r.Stable||!r.RightNormal)throw new InvalidOperationException(AppText.T("Segnale instabile durante il tentativo"));return r;}
  try{
   for(int axis=0;axis<2;axis++){
    var measured=await Measure();double error=measured.Axes[axis].Median;if(Math.Abs(error)<=SignalLimits.TargetCenter)continue;
    int index=8+axis,origin=original.Values[index],lower=Math.Max(0,origin-500),upper=Math.Min(65535,origin+500);
    // Bracket the measured center rather than extrapolating a slope from quantized codes.
    // An unchanged code is allowed: it still shrinks the bracket on the same side.
    await Write(current.WithCenter(index,(ushort)lower));double lowerError=(await Measure()).Axes[axis].Median;
    if(Math.Abs(lowerError)<=SignalLimits.TargetCenter)continue;
    await Write(current.WithCenter(index,(ushort)upper));double upperError=(await Measure()).Axes[axis].Median;
    if(Math.Abs(upperError)<=SignalLimits.TargetCenter)continue;
    if(Math.Sign(lowerError)==Math.Sign(upperError))throw new InvalidOperationException(AppText.T("Centro non raggiungibile entro i limiti della regolazione"));
    bool converged=false;
    for(int attempt=0;attempt<10;attempt++){
     if(upper-lower<=1)break;int next=lower+(upper-lower)/2;
     await Write(current.WithCenter(index,(ushort)next));error=(await Measure()).Axes[axis].Median;
     if(Math.Abs(error)<=SignalLimits.TargetCenter){converged=true;break;}
     if(Math.Sign(error)==Math.Sign(lowerError)){lower=next;lowerError=error;}else upper=next;
    }
    if(!converged)throw new InvalidOperationException(AppText.T("Correzione non convergente"));
   }
   success=true;return new(current,true,writes);
  }catch(OperationCanceledException){throw;}catch{ return new(original,false,writes);}
  finally{if(!success){using var recovery=new CancellationTokenSource(TimeSpan.FromSeconds(8));await session.WriteTemporaryAsync(original,recovery.Token);}}
 }
}
