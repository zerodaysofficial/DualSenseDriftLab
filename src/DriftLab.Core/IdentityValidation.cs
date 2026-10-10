namespace DriftLab;
public static class IdentityRules
{
 public static bool ValidSerial(string serial)=>serial!=null&&serial.Length is >=10 and <=32&&serial.All(char.IsAsciiLetterOrDigit)&&!serial.Contains("error",StringComparison.OrdinalIgnoreCase)&&!serial.Contains("unknown",StringComparison.OrdinalIgnoreCase);
}
