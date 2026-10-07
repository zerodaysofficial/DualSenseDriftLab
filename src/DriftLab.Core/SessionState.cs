namespace DriftLab.Core;
public sealed class SessionState
{
 public bool Connected{get;set;}public bool Busy{get;set;}public bool Verified{get;set;}public bool RestorePending{get;set;}public bool IsPersistentWrite{get;set;}
 public bool CanSave=>Connected&&!Busy&&(Verified||RestorePending);
 public bool CanRestore=>Connected&&!Busy;public bool CanReboot=>Connected&&!Busy;public bool CanCancel=>Busy&&!IsPersistentWrite;public bool CanClose=>!IsPersistentWrite;
 public void Invalidate(){Connected=false;Verified=false;RestorePending=false;}
}
