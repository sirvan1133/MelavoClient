using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 sealed record AccountInfo(long? Used,long? Total,long? Expires);
 static readonly ConditionalWeakTable<List<JsonObject>,AccountInfo> accountHeaders=new();
 static List<JsonObject> ReadAccountHeader(HttpResponseMessage response,List<JsonObject> profiles){
  long? used=null,total=null,expires=null;
  if(response.Headers.TryGetValues("subscription-userinfo",out var headers)){
   var data=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
   foreach(var part in string.Join(";",headers).Split(';')){var pair=part.Trim().Split('=',2);if(pair.Length==2&&long.TryParse(pair[1],out var value)&&value>=0)data[pair[0].Trim()]=value;}
   if(data.TryGetValue("upload",out var up)&&data.TryGetValue("download",out var down)&&up<=long.MaxValue-down)used=up+down;
   if(data.TryGetValue("total",out var capacity))total=capacity;
   if(data.TryGetValue("expire",out var expiration)&&expiration<=253402300799)expires=expiration;
  }
  accountHeaders.Add(profiles,new(used,total,expires));return profiles;
 }
 static void ApplyAccount(SubscriptionGroup group){if(accountHeaders.TryGetValue(group.Profiles,out var info)){group.UsedBytes=info.Used;group.TotalBytes=info.Total;group.ExpiresUnix=info.Expires;}}
 public static async Task<int> SubscriptionChecks(string[] addresses){var report=new List<string>();int failures=0;for(int i=0;i<addresses.Length;i++)try{var profiles=await Fetch(addresses[i]);int checkedProfiles=0;if(File.Exists(Core("xray.exe"))&&File.Exists(Core("sing-box.exe")))foreach(var profile in profiles){var path=Path.Combine(Path.GetTempPath(),"Melavo-check-"+Guid.NewGuid().ToString("N")+".json");try{var prepared=await Prepare(profile,17890);File.WriteAllText(path,prepared.Config.ToJsonString());if(await Task.Run(()=>Check("xray.exe",path,"run","-test"))!=0)throw new Exception("Xray کانفیگ دریافتی را نپذیرفت.");var tun=TConfig(17890);tun["inbounds"]![0]!["route_exclude_address"]=new JsonArray(prepared.Exclusions.Select(x=>(JsonNode?)JsonValue.Create(x)).ToArray());File.WriteAllText(path,tun.ToJsonString());if(await Task.Run(()=>Check("sing-box.exe",path,"check"))!=0)throw new Exception("sing-box کانفیگ TUN را نپذیرفت.");checkedProfiles++;}finally{try{File.Delete(path);}catch{}}}var protocols=profiles.SelectMany(p=>p["outbounds"]?.AsArray().Select(x=>x?["protocol"]?.ToString()??"")??Enumerable.Empty<string>()).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x);var validation=checkedProfiles>0?$" · Xray + TUN valid: {checkedProfiles}/{profiles.Count}":"";report.Add($"Endpoint {i+1}: PASS · {profiles.Count} config(s) · {string.Join(", ",protocols)}{validation}");}catch(Exception e){failures++;report.Add($"Endpoint {i+1}: FAIL · {SafeError(e.Message)}");}File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"subscription-checks.txt"),report);return failures;}
}
