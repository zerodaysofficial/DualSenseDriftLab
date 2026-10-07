using System.Buffers.Binary;
using DriftLab.Core;
namespace DriftLab.Protocol;
public static class FeatureCodec
{
 public static CalibrationData ReadCalibration(ReadOnlySpan<byte> b){if(b.Length<28||b[0]!=0x81||b[1]!=12||(b[2]!=2&&b[2]!=4)||b[3]!=2)throw new InvalidDataException(AppText.T("Risposta calibrazione inattesa"));var values=new ushort[12];for(int i=0;i<12;i++)values[i]=BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(4+i*2,2));return new(values);}
 public static byte[] Encode(CalibrationData data){var b=new byte[26];b[0]=12;b[1]=1;var v=data.Values;for(int i=0;i<12;i++)BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(2+i*2,2),v[i]);return b;}
 public static bool ValidSerial(string serial)=>serial.Length is >=10 and <=32&&serial.All(char.IsAsciiLetterOrDigit)&&!serial.Contains("error",StringComparison.OrdinalIgnoreCase)&&!serial.Contains("unknown",StringComparison.OrdinalIgnoreCase);
 public static NvStatus Nv(ReadOnlySpan<byte> b){if(b.Length<5||b[0]!=0x81)return NvStatus.Unknown;return BinaryPrimitives.ReadUInt32BigEndian(b.Slice(1,4)) switch{0x03030201=>NvStatus.Locked,0x03030200=>NvStatus.Unlocked,0x15010100=>NvStatus.PendingReboot,_=>NvStatus.Unknown};}
}
public sealed record BatteryInfo(int? Percent,string Status,bool Error);
public static class UsbInputDecoder
{
 public static bool IsSupported(ushort vendor,ushort product,ushort usagePage,ushort usage,int length)=>vendor==0x54c&&product==0xce6&&usagePage==1&&usage==5&&length==64;
 public static bool TryDecode(ReadOnlySpan<byte> b,TimeSpan time,out StickSample sample,out BatteryInfo battery){sample=default;battery=new(null,AppText.T("Sconosciuta"),false);if(b.Length!=64||b[0]!=1)return false;sample=new(time,(b[1]-127.5)/127.5,(b[2]-127.5)/127.5,(b[3]-127.5)/127.5,(b[4]-127.5)/127.5);int state=b[53]>>4,level=b[53]&15;battery=state switch{0=>new(Math.Min(100,level*10+5),AppText.T("Batteria"),false),1=>new(Math.Min(100,level*10+5),AppText.T("In carica USB"),false),2=>new(100,AppText.T("Carica completa"),false),_=>new(null,AppText.T("Stato alimentazione anomalo"),true)};return true;}
}
