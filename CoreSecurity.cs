using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;
namespace MelavoClient;
static class CoreSecurity {
 static readonly Dictionary<string,string> baseline=Load();
 static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Dictionary<string,string>> candidates=new(StringComparer.OrdinalIgnoreCase);
 static Dictionary<string,string> Load(){using var stream=typeof(CoreSecurity).Assembly.GetManifestResourceStream("MelavoClient.Assets.CoreHashes.json")!;return new(JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!,StringComparer.OrdinalIgnoreCase);}
 static string TrustKey(string folder)=>@"SOFTWARE\MelavoVPN\CoreTrust\"+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(folder).ToUpperInvariant())));
 public static void ApproveCandidate(string folder,Dictionary<string,string> expected){
  foreach(var entry in expected)if(UpdateService.Hash(Path.Combine(folder,entry.Key))!=entry.Value)throw new Exception("Downloaded core integrity check failed.");
  candidates[Path.GetFullPath(folder)]=new(expected,StringComparer.OrdinalIgnoreCase);
 }
 public static void ForgetCandidate(string folder)=>candidates.TryRemove(Path.GetFullPath(folder),out _);
 public static void RememberInstalled(string candidate,string destination){
  if(!candidates.TryGetValue(Path.GetFullPath(candidate),out var expected))throw new Exception("The core download has not been verified.");
  foreach(var entry in expected)if(UpdateService.Hash(Path.Combine(destination,entry.Key))!=entry.Value)throw new Exception("Installed core integrity check failed.");
  using var key=Registry.LocalMachine.CreateSubKey(TrustKey(destination),true);
  var merged=key.GetValue("VerifiedHashes") is string previous?JsonSerializer.Deserialize<Dictionary<string,string>>(previous)!:new Dictionary<string,string>();foreach(var entry in expected)merged[entry.Key]=entry.Value;key.SetValue("VerifiedHashes",JsonSerializer.Serialize(merged),RegistryValueKind.String);
 }
 public static List<FileStream> LockAndVerify(string executable){
  var folder=Path.GetFullPath(Path.GetDirectoryName(executable)!);var locks=new List<FileStream>();
  try{
   candidates.TryGetValue(folder,out var approved);using var registered=approved==null?Registry.LocalMachine.OpenSubKey(TrustKey(folder),false):null;
   Dictionary<string,string>? registeredHashes=null;if(registered?.GetValue("VerifiedHashes") is string receipt)registeredHashes=JsonSerializer.Deserialize<Dictionary<string,string>>(receipt);
   var paths=Directory.GetFiles(folder).Where(p=>Path.GetExtension(p).Equals(".dll",StringComparison.OrdinalIgnoreCase)).Append(Path.GetFullPath(executable)).Distinct(StringComparer.OrdinalIgnoreCase);
   foreach(var path in paths){
    var name=Path.GetFileName(path);var expected=approved?.GetValueOrDefault(name)??registeredHashes?.GetValueOrDefault(name)??baseline.GetValueOrDefault(name);
    if(expected==null)throw new Exception("An untrusted core dependency was found.");
    var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);locks.Add(stream);
    var digest=Convert.ToHexString(SHA256.HashData(stream));if(!digest.Equals(expected,StringComparison.OrdinalIgnoreCase)&&(approved!=null||!digest.Equals(baseline.GetValueOrDefault(name),StringComparison.OrdinalIgnoreCase)))throw new Exception("Core integrity check failed. Reinstall the official Melavo package.");
   }
   return locks;
  }catch{foreach(var file in locks)file.Dispose();throw;}
 }
 public static Process Start(ProcessStartInfo info){var process=new Process{StartInfo=info};try{Start(process);return process;}catch{process.Dispose();throw;}}
 public static void Start(Process process){
  var files=LockAndVerify(process.StartInfo.FileName);int released=0;
  void Release(){if(Interlocked.Exchange(ref released,1)==0)foreach(var file in files)file.Dispose();}
  process.EnableRaisingEvents=true;process.Exited+=(_,_)=>Release();
  try{if(!process.Start())throw new Exception("Core did not start.");if(process.HasExited)Release();}catch{Release();throw;}
 }
}
