using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DriftLab.Core;
namespace DriftLab.Storage;
public sealed record CalibrationBackup(int Version,string Id,DateTimeOffset CreatedUtc,ControllerIdentity Identity,ushort[] Values,string Kind,string Hash)
{
 [JsonIgnore]public CalibrationData Data=>new(Values);
 public static CalibrationBackup Create(ControllerIdentity id,CalibrationData data,string kind){var b=new CalibrationBackup(1,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,id,data.Values,kind,"");return b with{Hash=Digest(b)};}
 public static string Digest(CalibrationBackup b){var text=string.Join("|",b.Version,b.Id,b.CreatedUtc.ToUniversalTime().ToString("O"),b.Identity.Serial,b.Identity.Firmware,b.Identity.Hardware,string.Join(",",b.Values),b.Kind);return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));}
}
public static class BackupValidator
{
 public static void Validate(CalibrationBackup b,ControllerIdentity identity){if(b.Version!=1||!Guid.TryParseExact(b.Id,"N",out _)||b.CreatedUtc.Offset!=TimeSpan.Zero||b.CreatedUtc.Year<2024||b.Identity!=identity||b.Identity==null||!IdentityRules.ValidSerial(b.Identity.Serial)||b.Values==null||b.Values.Length!=12||b.Kind is not ("Initial" or "Saved" or "Restored")||b.Hash==null||b.Hash.Length!=64||!string.Equals(b.Hash,CalibrationBackup.Digest(b),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(AppText.T("Backup corrotto, incompatibile o di un altro controller"));}
}
public sealed class BackupRepository(string root)
{
 private static readonly JsonSerializerOptions Options=new(){UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,AllowDuplicateProperties=false};
 private string DirectoryFor(ControllerIdentity identity)=>Path.Combine(root,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.Serial))));
 public async Task SaveAsync(CalibrationBackup backup,CancellationToken ct){BackupValidator.Validate(backup,backup.Identity);var dir=DirectoryFor(backup.Identity);Directory.CreateDirectory(dir);await ExportAsync(backup,Path.Combine(dir,backup.Id+".json"),ct);}
 public static async Task ExportAsync(CalibrationBackup b,string path,CancellationToken ct){BackupValidator.Validate(b,b.Identity);string target=Path.GetFullPath(path),temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";try{await using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough|FileOptions.Asynchronous)){await JsonSerializer.SerializeAsync(stream,b,Options,ct);await stream.FlushAsync(ct);stream.Flush(true);}ct.ThrowIfCancellationRequested();File.Move(temp,target,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
 public async Task<CalibrationBackup> ReadAsync(string path,CancellationToken ct){var info=new FileInfo(path);if(info.Length>65536)throw new InvalidDataException(AppText.T("Backup oltre 64 KiB"));var bytes=await File.ReadAllBytesAsync(path,ct);if(bytes.Length>65536)throw new InvalidDataException(AppText.T("Backup troppo grande"));var b=JsonSerializer.Deserialize<CalibrationBackup>(bytes,Options)??throw new InvalidDataException(AppText.T("Backup vuoto"));BackupValidator.Validate(b,b.Identity);return b;}
 public async Task<IReadOnlyList<CalibrationBackup>> ListAsync(ControllerIdentity identity,CancellationToken ct){var dir=DirectoryFor(identity);if(!Directory.Exists(dir))return [];var result=new List<CalibrationBackup>();foreach(var file in Directory.EnumerateFiles(dir,"*.json")){try{var b=await ReadAsync(file,ct);BackupValidator.Validate(b,identity);result.Add(b);}catch(InvalidDataException){}catch(JsonException){}}return result.OrderByDescending(b=>b.CreatedUtc).ToArray();}
}
