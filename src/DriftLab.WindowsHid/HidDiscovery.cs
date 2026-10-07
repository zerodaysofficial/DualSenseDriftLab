using DriftLab.Core;
using System.Runtime.InteropServices;
using DriftLab.Protocol;
namespace DriftLab.WindowsHid;
public sealed record HidDeviceInfo(string Path,int InputLength,int FeatureLength)
{
 public override string ToString()=>Path.Length>85?Path[..85]+"…":Path;
}
public static class HidDiscovery
{
 // Presence comes from SetupAPI alone. A temporarily inaccessible HID handle
 // must never be mistaken for physical disappearance during reboot.
 public static bool IsPresent(string expectedPath)
 {
  NativeMethods.HidD_GetHidGuid(out var guid);var info=NativeMethods.SetupDiGetClassDevsW(ref guid,null,IntPtr.Zero,0x12);if(info==new IntPtr(-1))throw new System.ComponentModel.Win32Exception();
  try{for(int i=0;;i++){
   var data=new NativeMethods.InterfaceData{Size=Marshal.SizeOf<NativeMethods.InterfaceData>()};
   if(!NativeMethods.SetupDiEnumDeviceInterfaces(info,IntPtr.Zero,ref guid,i,ref data)){int error=Marshal.GetLastWin32Error();if(error==259)return false;throw new System.ComponentModel.Win32Exception(error);}
   NativeMethods.SetupDiGetDeviceInterfaceDetailW(info,ref data,IntPtr.Zero,0,out int length,IntPtr.Zero);if(length<8||length>65536)throw new IOException(AppText.T("Dimensione interfaccia HID inattesa"));
   var detail=Marshal.AllocHGlobal(length);try{Marshal.WriteInt32(detail,IntPtr.Size==8?8:6);if(!NativeMethods.SetupDiGetDeviceInterfaceDetailW(info,ref data,detail,length,out _,IntPtr.Zero))throw new System.ComponentModel.Win32Exception();var path=Marshal.PtrToStringUni(IntPtr.Add(detail,4));if(string.Equals(path,expectedPath,StringComparison.OrdinalIgnoreCase))return true;}finally{Marshal.FreeHGlobal(detail);}
  }}finally{NativeMethods.SetupDiDestroyDeviceInfoList(info);}
 }
 public static IReadOnlyList<HidDeviceInfo> FindDualSenseUsb()
 {
  NativeMethods.HidD_GetHidGuid(out var guid);IntPtr info=NativeMethods.SetupDiGetClassDevsW(ref guid,null,IntPtr.Zero,0x12);if(info==new IntPtr(-1))throw new System.ComponentModel.Win32Exception();var result=new List<HidDeviceInfo>();
  try{for(int i=0;;i++){
   var data=new NativeMethods.InterfaceData{Size=Marshal.SizeOf<NativeMethods.InterfaceData>()};if(!NativeMethods.SetupDiEnumDeviceInterfaces(info,IntPtr.Zero,ref guid,i,ref data))break;
   NativeMethods.SetupDiGetDeviceInterfaceDetailW(info,ref data,IntPtr.Zero,0,out int length,IntPtr.Zero);if(length<8||length>65536)continue;
   IntPtr detail=Marshal.AllocHGlobal(length);try{
    Marshal.WriteInt32(detail,IntPtr.Size==8?8:6);if(!NativeMethods.SetupDiGetDeviceInterfaceDetailW(info,ref data,detail,length,out _,IntPtr.Zero))continue;
    string? path=Marshal.PtrToStringUni(IntPtr.Add(detail,4));if(string.IsNullOrEmpty(path)||!path.Contains("vid_054c&pid_0ce6",StringComparison.OrdinalIgnoreCase))continue;
    using var handle=NativeMethods.CreateFileW(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero);if(handle.IsInvalid)continue;var attrs=new NativeMethods.Attributes{Size=Marshal.SizeOf<NativeMethods.Attributes>()};if(!NativeMethods.HidD_GetAttributes(handle,ref attrs)||!NativeMethods.HidD_GetPreparsedData(handle,out var prep))continue;
    try{if(NativeMethods.HidP_GetCaps(prep,out var caps)!=0x110000)continue;if(UsbInputDecoder.IsSupported(attrs.Vendor,attrs.Product,caps.UsagePage,caps.Usage,caps.InputLength)&&caps.FeatureLength>=28)result.Add(new(path,caps.InputLength,caps.FeatureLength));}finally{NativeMethods.HidD_FreePreparsedData(prep);}
   }finally{Marshal.FreeHGlobal(detail);}
  }}finally{NativeMethods.SetupDiDestroyDeviceInfoList(info);}return result;
 }
}
