namespace DriftLab.Core;
public sealed record ControllerIdentity(string Serial,uint Firmware,uint Hardware);
public readonly record struct StickSample(TimeSpan Time,double LX,double LY,double RX,double RY)
{
 public double[] Axes=>[LX,LY,RX,RY];
 public bool Finite=>Axes.All(x=>double.IsFinite(x)&&Math.Abs(x)<=1);
}
public sealed class CalibrationData
{
 private readonly ushort[] values;
 public ushort[] Values=>(ushort[])values.Clone();
 public CalibrationData(ushort[] data){if(data.Length!=12)throw new ArgumentException(AppText.T("Servono 12 parametri"));values=(ushort[])data.Clone();}
 public CalibrationData WithCenter(int index,ushort value){if(index!=8&&index!=9)throw new ArgumentException(AppText.T("Solo centro sinistro"));var a=Values;a[index]=value;return new(a);}
 public bool SameAs(CalibrationData other)=>values.SequenceEqual(other.values);
}
public enum NvStatus { Unknown,Locked,Unlocked,PendingReboot }
// USB axes are eight-bit: adjacent codes differ by 2/255; zero lies between 127 and 128.
public static class SignalLimits
{
 public const double Center=2d/255+1e-6,TargetCenter=1d/255+1e-6;
 public const double Delta=2d/255+1e-6,MicroSpread=2d/255+1e-6,Direction=4d/255+1e-6;
 public const double SpikeDeviation=4d/255+1e-6;
}
public enum DriftKind { Normal,StableOffset,Progressive,Noisy,Insufficient,DirectionDependent,MicroTremor,Spikes }
public sealed record AxisStats(double Median,double Delta,double Spread,double PeakDeviation=0,int SpikeCount=0);
public sealed record DriftResult(DriftKind Kind,AxisStats[] Axes,int Count)
{
 public bool Stable=>Kind is DriftKind.Normal or DriftKind.StableOffset;
 public bool RightNormal=>Axes.Skip(2).All(x=>Math.Abs(x.Median)<=SignalLimits.Center&&Math.Abs(x.Delta)<=SignalLimits.Delta&&x.Spread<=SignalLimits.MicroSpread&&x.SpikeCount==0);
}
public sealed record AnalysisOptions(double Duration=8,int Minimum=200,double Center=SignalLimits.Center,double Delta=SignalLimits.Delta,double Spread=.025,double MaxGap=.25,double MicroSpread=SignalLimits.MicroSpread);
public sealed record GuidedTestResult(long Generation,DriftResult Rest,IReadOnlyList<DriftResult> Cycles)
{
 public DriftResult Overall=>DriftAnalyzer.Combine(new[]{Rest}.Concat(Cycles).ToArray());
}
public sealed record RangeCheckResult(bool Passed);
public sealed record VerificationResult(bool CanSave,string Message);
public static class VerificationPolicy
{
 public static VerificationResult Evaluate(GuidedTestResult before,GuidedTestResult after,RangeCheckResult range)
 {
  bool valid=before.Generation==after.Generation&&before.Cycles.Count==6&&after.Cycles.Count==6&&before.Overall.Kind!=DriftKind.Insufficient;
  bool pass=valid&&after.Overall.Kind==DriftKind.Normal&&after.Rest.RightNormal&&after.Cycles.All(x=>x.RightNormal)&&range.Passed;
  return new(pass,pass?AppText.T("Test dopo calibrazione superato. Salvataggio disponibile."):AppText.T("Calibrazione non verificata: salvataggio disabilitato."));
 }
}
public interface ICalibrationSession
{
 Task WriteTemporaryAsync(CalibrationData data,CancellationToken ct);
 Task<IReadOnlyList<StickSample>> GetFreshSamplesAsync(TimeSpan duration,CancellationToken ct);
}
public sealed record CorrectionAttempt(CalibrationData Data,bool Converged,int Writes);
