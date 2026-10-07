using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
namespace DriftLab.WindowsHid;
internal static class NativeMethods
{
 [StructLayout(LayoutKind.Sequential)]internal struct Attributes{public int Size;public ushort Vendor,Product,Version;}
 [StructLayout(LayoutKind.Sequential)]internal struct Caps{public ushort Usage,UsagePage,InputLength,OutputLength,FeatureLength;[MarshalAs(UnmanagedType.ByValArray,SizeConst=17)]public ushort[] Reserved;public ushort LinkNodes,InputButtons,InputValues,InputIndices,OutputButtons,OutputValues,OutputIndices,FeatureButtons,FeatureValues,FeatureIndices;}
 [StructLayout(LayoutKind.Sequential)]internal struct InterfaceData{public int Size;public Guid ClassGuid;public int Flags;public IntPtr Reserved;}
 [DllImport("hid.dll")]internal static extern void HidD_GetHidGuid(out Guid guid);
 [DllImport("hid.dll")] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool HidD_GetAttributes(SafeFileHandle handle,ref Attributes attributes);
 [DllImport("hid.dll")] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool HidD_GetPreparsedData(SafeFileHandle handle,out IntPtr data);
 [DllImport("hid.dll")] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool HidD_FreePreparsedData(IntPtr data);
 [DllImport("hid.dll")]internal static extern int HidP_GetCaps(IntPtr data,out Caps caps);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool HidD_GetFeature(SafeFileHandle handle,[In,Out]byte[] buffer,int length);
 [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool HidD_SetFeature(SafeFileHandle handle,[In]byte[] buffer,int length);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]internal static extern IntPtr SetupDiGetClassDevsW(ref Guid guid,string? enumerator,IntPtr parent,int flags);
 [DllImport("setupapi.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr info,IntPtr device,ref Guid guid,int index,ref InterfaceData data);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr info,ref InterfaceData data,IntPtr detail,int size,out int required,IntPtr deviceInfo);
 [DllImport("setupapi.dll")] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr info);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]internal static extern SafeFileHandle CreateFileW(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
 [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]internal static extern bool CancelIoEx(SafeFileHandle handle,IntPtr overlapped);
}
