using System.Text.Json.Nodes;
namespace MelavoClient;
sealed record AppRelease(string Version,string Url,string Sha256,long Size);
static partial class UpdateService {
 public const string AppRepository="sirvan1133/MelavoClient";
 public static async Task<PackagePlan> PrepareOfficialPackage(string archive,CancellationToken cancellation=default){
  string version;
  using(var zip=System.IO.Compression.ZipFile.OpenRead(archive)){
   var entry=zip.GetEntry("release.json")??throw new Exception("The update manifest is missing.");
   if(entry.Length>50000)throw new Exception("The update manifest is too large.");
   using var reader=new StreamReader(entry.Open());version=JsonNode.Parse(await reader.ReadToEndAsync(cancellation))?["version"]?.ToString()??"";
  }
  if(!Version.TryParse(version,out var available)||available<=Version.Parse(AppVersion))throw new Exception("Select a newer official Melavo release.");
  using var http=Http();var json=JsonNode.Parse(await http.GetStringAsync($"https://api.github.com/repos/{AppRepository}/releases/tags/v{version}",cancellation))!.AsObject();
  var release=ParseAppRelease(json)??throw new Exception("The package is not a published official Windows release.");
  var digest=await Task.Run(()=>Hash(archive),cancellation);
  if(!digest.Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new Exception("The selected ZIP does not match the official GitHub release.");
  var plan=await Task.Run(()=>PreparePackage(archive),cancellation);
  if(plan.Version!=release.Version)throw new Exception("The package version does not match the official release.");
  return plan;
 }
 public static AppRelease? ParseAppRelease(JsonObject release){
  if(release["draft"]?.GetValue<bool>()==true||release["prerelease"]?.GetValue<bool>()==true)return null;
  var version=release["tag_name"]?.ToString().TrimStart('v')??"";
  if(!Version.TryParse(version,out var candidate)||candidate<=Version.Parse(AppVersion))return null;
#if !CUSTOMER_EDITION
  return null;
#else
  var asset=release["assets"]?.AsArray().FirstOrDefault(a=>a?["name"]?.ToString()==$"Melavo-VPN-{version}-Windows-x64.zip"&&a?["state"]?.ToString()=="uploaded");
  if(asset==null)return null;
  var url=asset["browser_download_url"]?.ToString()??"";var digest=asset["digest"]?.ToString()??"";var size=asset["size"]?.GetValue<long>()??0;
  if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith($"/{AppRepository}/releases/download/",StringComparison.Ordinal)||uri.UserInfo.Length>0||size<=0||size>500_000_000||!digest.StartsWith("sha256:",StringComparison.Ordinal)||digest.Length!=71||!digest[7..].All(Uri.IsHexDigit))throw new Exception("The release download metadata could not be verified.");
  return new(version,url,digest[7..],size);
#endif
 }
 public static async Task<AppRelease?> LatestApp(CancellationToken cancellation=default){using var http=Http();var json=await http.GetStringAsync($"https://api.github.com/repos/{AppRepository}/releases/latest",cancellation);return ParseAppRelease(JsonNode.Parse(json)!.AsObject());}
 public static async Task<PackagePlan> DownloadApp(AppRelease release,IProgress<int>? progress=null,CancellationToken cancellation=default,HttpClient? testHttp=null){
  var folder=NewStage();var archive=Path.Combine(folder,"release.zip");
  try{
   using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromMinutes(15));var token=timeout.Token;
   using var ownedHttp=testHttp==null?Http():null;var http=testHttp??ownedHttp!;using var response=await http.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
   using(var input=await response.Content.ReadAsStreamAsync(token))using(var output=File.Create(archive)){
    var buffer=new byte[65536];long total=0;int count;while((count=await input.ReadAsync(buffer,token))>0){total+=count;if(total>release.Size||total>500_000_000)throw new Exception("Unexpected update download size.");await output.WriteAsync(buffer.AsMemory(0,count),token);progress?.Report((int)(total*100/release.Size));}if(total!=release.Size)throw new Exception("The update download is incomplete.");
   }
   if(!Hash(archive).Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new Exception("The update download checksum does not match GitHub.");
   var plan=PreparePackage(archive);if(plan.Version!=release.Version)throw new Exception("The package version does not match the GitHub release.");return plan;
  }finally{Directory.Delete(folder,true);}
 }
}
