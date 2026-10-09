using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed record CoreRelease(string Version,string Url,string Sha256);
sealed record PackagePlan(string Version,string Stage,string[] Files,Dictionary<string,string> Hashes);
static partial class UpdateService {
 public const string AppVersion="0.9.0";
 public static string TempRoot=>Path.Combine(SubscriptionStore.DirectoryPath,"updates");
 public static string NewStage(){var path=Path.Combine(TempRoot,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;}
 static HttpClient Http(){var http=new HttpClient{Timeout=TimeSpan.FromMinutes(3)};http.DefaultRequestHeaders.UserAgent.ParseAdd("MelavoClient/0.9.0");return http;}
 public static async Task<CoreRelease> LatestCore(){using var http=Http();var json=JsonNode.Parse(await http.GetStringAsync("https://api.github.com/repos/XTLS/Xray-core/releases/latest"))!;var asset=json["assets"]!.AsArray().FirstOrDefault(a=>a?["name"]?.ToString()=="Xray-windows-64.zip")??throw new Exception("فایل ویندوز در انتشار رسمی موجود نیست.");var url=asset["browser_download_url"]!.ToString();var digest=asset["digest"]?.ToString()??"";if(!url.StartsWith("https://github.com/XTLS/Xray-core/releases/download/",StringComparison.OrdinalIgnoreCase)||!digest.StartsWith("sha256:")||digest.Length!=71)throw new Exception("اطلاعات تأیید دانلود رسمی کامل نیست.");return new(json["tag_name"]!.ToString().TrimStart('v'),url,digest[7..]);}
 public static async Task<string> DownloadCore(CoreRelease release,IProgress<int> progress){var stage=NewStage();try{var archive=Path.Combine(stage,"download.zip");using var http=Http();using var response=await http.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead);response.EnsureSuccessStatusCode();using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(archive)){var buffer=new byte[65536];long total=0;int read;using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));while((read=await input.ReadAsync(buffer,timeout.Token))>0){total+=read;if(total>150_000_000)throw new Exception("اندازهٔ دانلود غیرمنتظره است.");await output.WriteAsync(buffer.AsMemory(0,read),timeout.Token);if(response.Content.Headers.ContentLength is long length&&length>0)progress.Report((int)(total*100/length));}}
  using var lockedArchive=new FileStream(archive,FileMode.Open,FileAccess.Read,FileShare.Read);if(!Convert.ToHexString(SHA256.HashData(lockedArchive)).Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new Exception("SHA256 دانلود با انتشار رسمی مطابقت ندارد.");var core=Path.Combine(stage,"core");Directory.CreateDirectory(core);lockedArchive.Position=0;using var zip=new ZipArchive(lockedArchive,ZipArchiveMode.Read,true);var verifiedHashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var allowed=new HashSet<string>(new[]{"xray.exe","libcronet.dll","geoip.dat","geosite.dat","LICENSE"},StringComparer.OrdinalIgnoreCase);foreach(var entry in zip.Entries){var relative=Relative(entry.FullName);if(entry.Name.Length==0)continue;if(relative.Contains('/')||!allowed.Contains(relative))continue;if(entry.Length>80_000_000)throw new Exception("فایل استخراج‌شده بیش از حد بزرگ است.");var name=relative.Equals("LICENSE",StringComparison.OrdinalIgnoreCase)?"LICENSE-Xray.txt":relative;if(Path.GetExtension(name) is ".exe" or ".dll"){using var trustedEntry=entry.Open();verifiedHashes[name]=Convert.ToHexString(SHA256.HashData(trustedEntry)).ToLowerInvariant();}entry.ExtractToFile(Path.Combine(core,name),true);}ValidateExe(Path.Combine(core,"xray.exe"));CoreSecurity.ApproveCandidate(core,verifiedHashes);return core;}catch{CleanupStage(stage);throw;}
 }
 public static string Relative(string path){path=path.Replace('\\','/');if(string.IsNullOrWhiteSpace(path)||path.StartsWith('/')||path.Contains(':')||path.Split('/').Any(p=>p==".."||p=="."))throw new Exception("مسیر نامعتبر در بستهٔ آپدیت.");return path.TrimEnd('/');}
 public static string Hash(string path){using var input=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();}
 public static void ValidateExe(string file){using var stream=File.OpenRead(file);using var reader=new BinaryReader(stream);if(stream.Length<128||reader.ReadUInt16()!=0x5A4D)throw new Exception("فایل اجرایی معتبر نیست.");stream.Position=60;int offset=reader.ReadInt32();if(offset<64||offset>stream.Length-6)throw new Exception("ساختار فایل اجرایی معتبر نیست.");stream.Position=offset;if(reader.ReadUInt32()!=0x4550||reader.ReadUInt16()!=0x8664)throw new Exception("فایل اجرایی باید Windows x64 باشد.");}
 public static string CoreVersion(string file){var info=new ProcessStartInfo(file){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};info.ArgumentList.Add("version");using var process=CoreSecurity.Start(info);var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(5000)){process.Kill(true);throw new Exception("خواندن نسخهٔ هسته طول کشید.");}return output.GetAwaiter().GetResult().Split('\n').FirstOrDefault()?.Trim()??"نامشخص";}
 public static void ApplyCore(string candidate,string destination,bool rememberTrust=false){destination=Path.GetFullPath(destination);Directory.CreateDirectory(destination);var backup=Path.Combine(NewStage(),"backup-core");Directory.CreateDirectory(backup);var completed=new List<(string Path,string? Backup)>();try{foreach(var file in Directory.GetFiles(candidate).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase)){var target=Path.GetFullPath(Path.Combine(destination,Path.GetFileName(file)));if(!target.StartsWith(destination+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("مسیر نصب نامعتبر است.");string? saved=null;if(File.Exists(target)){saved=Path.Combine(backup,Path.GetFileName(file));File.Copy(target,saved,true);}completed.Add((target,saved));File.Copy(file,target,true);}if(rememberTrust)CoreSecurity.RememberInstalled(candidate,destination);}catch{foreach(var item in completed.AsEnumerable().Reverse()){if(item.Backup!=null)File.Copy(item.Backup,item.Path,true);else File.Delete(item.Path);}throw;}finally{CleanupStage(Path.GetDirectoryName(backup)!);}}
 static bool AllowedPackageFile(string path)=>path=="MelavoClient.exe"||path=="راهنما.txt"||path=="Design-System.md"||path.StartsWith("core/",StringComparison.Ordinal)&&!path[5..].Contains('/')&&new[]{"xray.exe","sing-box.exe","wintun.dll","libcronet.dll","geoip.dat","geosite.dat","LICENSE","LICENSE-Xray.txt","LICENSE-sing-box.txt","LICENSE-wintun.txt","README.md"}.Contains(path[5..],StringComparer.OrdinalIgnoreCase);
 public static PackagePlan PreparePackage(string archive,bool allowSame=false){using var zip=ZipFile.OpenRead(archive);foreach(var e in zip.Entries)Relative(e.FullName);var manifestEntry=zip.GetEntry("release.json")??throw new Exception("این ZIP بستهٔ آپدیت برنامه نیست؛ release.json ندارد.");if(manifestEntry.Length>50000)throw new Exception("اطلاعات نسخه نامعتبر است.");using var text=new StreamReader(manifestEntry.Open());var manifest=JsonNode.Parse(text.ReadToEnd())!.AsObject();if(manifest["app"]?.ToString()!="MelavoClient")throw new Exception("این بسته متعلق به برنامه نیست.");if((manifest["edition"]?.ToString()??"standard")!=SubscriptionStore.Edition)throw new Exception("بستهٔ آپدیت باید مخصوص همین نسخهٔ مشتری یا شخصی باشد.");var version=manifest["version"]?.ToString()??"";if(!Version.TryParse(version,out var newer)||(!allowSame&&newer<=Version.Parse(AppVersion)))throw new Exception("بسته نسخهٔ جدیدتری از برنامه نیست.");var hashes=manifest["files"]?.AsObject()??throw new Exception("لیست فایل‌های آپدیت موجود نیست.");if(!hashes.ContainsKey("MelavoClient.exe"))throw new Exception("فایل برنامه در بسته موجود نیست.");var stage=NewStage();try{foreach(var item in hashes){var relative=Relative(item.Key);if(!AllowedPackageFile(relative))throw new Exception("فایل غیرمجاز در بستهٔ آپدیت.");var entry=zip.GetEntry(relative)??throw new Exception("فایل اعلام‌شده در بسته وجود ندارد.");if(entry.Length>250_000_000)throw new Exception("فایل آپدیت بیش از حد بزرگ است.");var target=Path.GetFullPath(Path.Combine(stage,relative.Replace('/',Path.DirectorySeparatorChar)));if(!target.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("مسیر بسته نامعتبر است.");Directory.CreateDirectory(Path.GetDirectoryName(target)!);entry.ExtractToFile(target,true);if(!Hash(target).Equals(item.Value?.ToString(),StringComparison.OrdinalIgnoreCase))throw new Exception("هش فایل‌های بسته صحیح نیست.");}ValidateExe(Path.Combine(stage,"MelavoClient.exe"));var manifestPath=Path.Combine(stage,"release.json");manifestEntry.ExtractToFile(manifestPath,true);var installedHashes=hashes.ToDictionary(k=>k.Key,k=>k.Value!.ToString());installedHashes["release.json"]=Hash(manifestPath);return new(version,stage,installedHashes.Keys.ToArray(),installedHashes);}catch{CleanupStage(stage);throw;}}
 public static string WorkerScript(PackagePlan plan,string destination,int pid,bool restart=true){
  static string Quote(string value)=>"'"+value.Replace("'","''")+"'";
  var header="$targetRoot="+Quote(Path.GetFullPath(destination))+"\n$stageRoot="+Quote(Path.GetFullPath(plan.Stage))+"\n$parentId="+pid+"\n$files=@("+string.Join(",",plan.Files.Select(Quote))+")\n";
  header+="$expected=@{"+string.Join(";",plan.Hashes.Select(h=>Quote(h.Key)+"="+Quote(h.Value)))+"}\n";
  var script=header+"""
 $ErrorActionPreference='Stop'
 $backupRoot=Join-Path $stageRoot 'backup-app'
 $applied=New-Object System.Collections.Generic.List[string]
 $workerLock=[IO.File]::Open((Join-Path $stageRoot 'worker.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 $canClean=$false
 function CheckedTarget([string]$relative){
   $absolute=[IO.Path]::GetFullPath((Join-Path $targetRoot $relative))
   if(-not $absolute.StartsWith($targetRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid update target'}
   return $absolute
 }
 try {
   Wait-Process -Id $parentId -Timeout 60 -ErrorAction SilentlyContinue
   if(Get-Process -Id $parentId -ErrorAction SilentlyContinue){throw 'Application did not exit'}
   foreach($relative in $files){$source=[IO.Path]::GetFullPath((Join-Path $stageRoot $relative));if(-not $source.StartsWith($stageRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid update source'};$verifyStream=[IO.File]::OpenRead($source);$verifyHash=[Security.Cryptography.SHA256]::Create();try{$verified=[BitConverter]::ToString($verifyHash.ComputeHash($verifyStream)).Replace('-','')}finally{$verifyStream.Dispose();$verifyHash.Dispose()};if($verified -ne $expected[$relative]){throw 'Staged update checksum mismatch'}}
   New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
   foreach($relative in $files){
     $target=CheckedTarget $relative
     $source=[IO.Path]::GetFullPath((Join-Path $stageRoot $relative))
     if(-not $source.StartsWith($stageRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid update source'}
     if(Test-Path -LiteralPath $target){$saved=Join-Path $backupRoot $relative;New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($saved)) -Force | Out-Null;Copy-Item -LiteralPath $target -Destination $saved -Force}
     New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
     $applied.Add($relative)
     $inputStream=[IO.File]::Open($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
     try{
       $hasher=[Security.Cryptography.SHA256]::Create()
       try{$actual=[BitConverter]::ToString($hasher.ComputeHash($inputStream)).Replace('-','')}finally{$hasher.Dispose()}
       if($actual -ne $expected[$relative]){throw 'Staged update checksum mismatch'}
       $inputStream.Position=0
       $outputStream=[IO.File]::Open($target,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
       try{$inputStream.CopyTo($outputStream)}finally{$outputStream.Dispose()}
     }finally{$inputStream.Dispose()}
   }
   Set-Content -LiteralPath (Join-Path $stageRoot 'result.txt') -Value 'Update installed' -Encoding UTF8
   $canClean=$true
 } catch {
   foreach($relative in $applied){$target=CheckedTarget $relative;$saved=Join-Path $backupRoot $relative;if(Test-Path -LiteralPath $saved){Copy-Item -LiteralPath $saved -Destination $target -Force}else{Remove-Item -LiteralPath $target -ErrorAction SilentlyContinue}}
   Set-Content -LiteralPath (Join-Path $stageRoot 'result.txt') -Value ('Update failed: '+$_.Exception.Message) -Encoding UTF8
   $canClean=$true
 } finally {
   if($canClean){foreach($relative in $files){$source=[IO.Path]::GetFullPath((Join-Path $stageRoot $relative));if($source.StartsWith($stageRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $source -Force -ErrorAction SilentlyContinue}};if([IO.Path]::GetFullPath($backupRoot).StartsWith($stageRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $backupRoot -Recurse -Force -ErrorAction SilentlyContinue}}
   $workerLock.Dispose()
 }
 
 """;return restart?script+"\nStart-Process -FilePath (Join-Path $targetRoot 'MelavoClient.exe') -WindowStyle Normal\n":script;
 }
 public static void StartPackageInstall(PackagePlan plan,string destination,bool restart=true){
  var command=WorkerScript(plan,destination,Environment.ProcessId,restart);
  var encoded=Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command));
  var shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
  var info=new ProcessStartInfo(shell){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-EncodedCommand",encoded}};
  using var worker=Process.Start(info)??throw new Exception("The update installer could not start.");
 }
}
