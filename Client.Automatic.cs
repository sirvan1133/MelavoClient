using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 Dictionary<string,SmartHistory> smartHistory=new();List<SmartMetric> smartMetrics=new();
 readonly System.Windows.Forms.Timer smartTimer=new(){Interval=1000};CancellationTokenSource? smartCancellation;bool smartChecking,smartRecoverPending;readonly SmartHealth smartHealth=new();DateTime smartLastCheck,smartLastRank;string? smartKey;
 JsonArray? smartBaseRules;string? smartCurrentTag,smartInbound;
 string SmartHistoryPath=>Path.Combine(SubscriptionStore.DirectoryPath,"smart-history.json");
 static string SmartKey(JsonObject profile){var copy=(JsonObject)profile.DeepClone();foreach(var key in new[]{"remarks","_shareLink","_shareSnapshot"})copy.Remove(key);return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(copy.ToJsonString())));}
 SmartNode[] SmartNodes()=>subscriptions.SelectMany(g=>g.Profiles.Select((p,i)=>new SmartNode(WebId(g,i),SmartKey(p),(JsonObject)p.DeepClone()))).GroupBy(n=>n.Key).Select(g=>g.First()).ToArray();
 void InitializeSmart(){if(preview)return;smartHistory=SmartSelection.Load(SmartHistoryPath);smartTimer.Tick+=async(_,_)=>await MonitorSmart();smartTimer.Start();FormClosed+=(_,_)=>{smartCancellation?.Cancel();smartTimer.Dispose();};}
 void SaveSmart(){if(preview)return;try{SmartSelection.Save(SmartHistoryPath,smartHistory);}catch{details.Text=L("Could not save smart-selection history.","ذخیرهٔ سابقهٔ انتخاب هوشمند انجام نشد.");}}
 async Task RankSmart(CancellationToken token,bool silent=false){smartMetrics=await SmartSelection.Run(SmartNodes(),smartHistory,SmartProbe.Open,SmartTcp,(stage,count,total)=>{if(silent)return;status.Text=L($"Smart · {stage} · {count}/{total}",$"هوشمند · {stage switch{"filter"=>"غربال","stability"=>"پایداری",_=>"امتیاز"}} · {count}/{total}");PushWebState();},token);smartLastRank=DateTime.UtcNow;SaveSmart();}
 async Task ConnectAutomatic(){
  if(preview)throw new InvalidOperationException(L("Smart tests are unavailable in offline preview.","تست هوشمند در پیش‌نمایش آفلاین اجرا نمی‌شود."));
  if(SmartNodes().Length==0)throw new InvalidOperationException(L("Add a subscription or configuration first.","ابتدا ساب یا کانفیگ اضافه کنید."));
  smartCancellation?.Dispose();smartCancellation=CancellationTokenSource.CreateLinkedTokenSource(pingCancellation.Token);smartCancellation.CancelAfter(TimeSpan.FromMinutes(5));pingTesting=true;SetControls();
  try{await RankSmart(smartCancellation.Token);}finally{pingTesting=false;SetControls();}
  foreach(var candidate in EligibleSmart()){
   SelectWebNode(candidate.Id);webActiveId=candidate.Id;await Connect();
   if(xray!=null){try{using var deadline=new CancellationTokenSource(2500);await using var probe=new SmartProbe(localProxyPort);await probe.Http204(deadline.Token);smartKey=candidate.Key;smartHealth.Reset();smartLastCheck=DateTime.UtcNow;status.Text=L("Connected · smart selection","متصل · انتخاب هوشمند");SyncUi();return;}catch{}}
   NoteSmartFailure(candidate.Key);Disconnect();webActiveId=null;
  }throw new InvalidOperationException(L("No server passed the smart tunnel tests.","هیچ سروری تست هوشمند تونل را پاس نکرد."));
 }
 List<SmartMetric> EligibleSmart()=>SmartSelection.Ranked(smartMetrics).Where(m=>smartHistory[m.Key].CooldownUntil<=DateTime.UtcNow).ToList();
 void NoteSmartFailure(string? key){if(key==null)return;if(!smartHistory.TryGetValue(key,out var entry))smartHistory[key]=entry=new();entry.Fail(DateTime.UtcNow);SaveSmart();}
 void StopSmartSession(bool unexpected=false){if(unexpected&&preferences.AutomaticServer)NoteSmartFailure(smartKey);smartCancellation?.Cancel();smartRecoverPending=false;smartKey=null;smartHealth.Reset();smartBaseRules=null;smartCurrentTag=smartInbound=null;}
 void ConfigureSmart(JsonObject config){
  smartKey=null;if(!preferences.AutomaticServer)return;
  config["api"]!["services"]=new JsonArray("StatsService","HandlerService","RoutingService");
  smartInbound=config["inbounds"]![0]!["tag"]!.ToString();var outbound=Outbound(config)!.AsObject();smartCurrentTag=outbound["tag"]?.ToString()??"smart-initial";outbound["tag"]=smartCurrentTag;
  smartBaseRules=(JsonArray)config["routing"]!["rules"]!.DeepClone();config["routing"]!["rules"]=SmartRules(smartCurrentTag);
 }
 JsonArray SmartRules(string tag){var rules=(JsonArray)smartBaseRules!.DeepClone();rules.Insert(0,new JsonObject{["type"]="field",["inboundTag"]=new JsonArray(smartInbound!),["outboundTag"]=tag});return rules;}
 async Task SmartApi(string operation,string file,CancellationToken token){var info=new ProcessStartInfo(Core("xray.exe")){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(Core("xray.exe")),RedirectStandardError=true,RedirectStandardOutput=true,ArgumentList={"api",operation,"--server=127.0.0.1:"+statsPort,"--timeout=3",file}};using var process=CoreSecurity.Start(info);using var ownedJob=new CoreJob();ownedJob.Add(process);var error=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();try{await process.WaitForExitAsync(token);}catch{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}throw;}if(process.ExitCode!=0)throw new IOException("Xray hot-swap API failed: "+SafeError(await error));await output;}
 async Task<bool> HotSwap(SmartMetric candidate,CancellationToken token){
  var active=xray;if(active==null||smartCurrentTag==null||smartBaseRules==null)return false;var previousTag=smartCurrentTag;var folder=SessionFiles.Create();var file=Path.Combine(folder,"route.json");bool changed=false;
  try{var node=SmartNodes().First(n=>n.Id==candidate.Id);await using(var warm=await SmartProbe.Open(node,token)){using var limit=CancellationTokenSource.CreateLinkedTokenSource(token);limit.CancelAfter(2500);await warm.Http204(limit.Token);}
   var prepared=await Prepare(node.Profile,localProxyPort);var outbound=(JsonObject)Outbound(prepared.Config)!.DeepClone();var tag="smart-"+Guid.NewGuid().ToString("N");outbound["tag"]=tag;
   File.WriteAllText(file,new JsonObject{["outbounds"]=new JsonArray(outbound)}.ToJsonString());await SmartApi("ado",file,token);token.ThrowIfCancellationRequested();if(!ReferenceEquals(active,xray))return false;
   File.WriteAllText(file,new JsonObject{["routing"]=new JsonObject{["rules"]=SmartRules(tag)}}.ToJsonString());await SmartApi("adrules",file,token);changed=true;
   using(var limit=CancellationTokenSource.CreateLinkedTokenSource(token)){limit.CancelAfter(2500);await using var check=new SmartProbe(localProxyPort);await check.Http204(limit.Token);}
   smartCurrentTag=tag;smartKey=candidate.Key;webActiveId=candidate.Id;SelectWebNode(candidate.Id);activeProfileKey=ProfileKey(WebNode(candidate.Id).Profile);smartHealth.Reset();smartLastCheck=DateTime.UtcNow;_ = RefreshIp();return true;
  }catch{if(changed&&ReferenceEquals(active,xray)){try{File.WriteAllText(file,new JsonObject{["routing"]=new JsonObject{["rules"]=SmartRules(previousTag)}}.ToJsonString());using var rollback=new CancellationTokenSource(5000);await SmartApi("adrules",file,rollback.Token);}catch{status.Text=L("Smart route recovery failed; reconnect required.","بازیابی مسیر هوشمند ناموفق بود؛ دوباره متصل شوید.");}}return false;}
  finally{SessionFiles.RemoveDirectory(folder);}
 }
 async Task MonitorSmart(){
  if(smartRecoverPending&&!webOperation&&!busy&&!updating){await RecoverSmart();return;}
  if(preview||smartChecking||!preferences.AutomaticServer||smartKey==null||xray==null||busy||updating||pingTesting||webOperation)return;
  bool stalled=smartHealth.Observe(totalRx,totalTx);
  if((DateTime.UtcNow-smartLastCheck).TotalSeconds<15&&!stalled)return;
  smartChecking=true;bool ownsOperation=false;var current=xray;var key=smartKey;smartCancellation?.Dispose();smartCancellation=new CancellationTokenSource(TimeSpan.FromMinutes(5));var token=smartCancellation.Token;
  try{smartLastCheck=DateTime.UtcNow;try{using var limit=CancellationTokenSource.CreateLinkedTokenSource(token);limit.CancelAfter(2500);await using var probe=new SmartProbe(localProxyPort);await probe.Http204(limit.Token);smartHealth.Check(true);}catch{if(!ReferenceEquals(current,xray))return;smartHealth.Check(false);}
   bool failed=smartHealth.ConsecutiveFailures>=2;if(failed){NoteSmartFailure(key);smartHealth.Check(true);}
   if(failed||DateTime.UtcNow-smartLastRank>TimeSpan.FromMinutes(5)){
    if(webOperation)return;webOperation=true;ownsOperation=true;
    if(failed&&await TrySmartAlternatives(key,token))return;
    await RankSmart(token,true);if(!ReferenceEquals(current,xray))return;
    if(failed){if(!await TrySmartAlternatives(key,token))status.Text=L("No healthy replacement found; smart mode will retry.","جایگزین سالم پیدا نشد؛ حالت هوشمند دوباره بررسی می‌کند.");}
    else{var best=EligibleSmart().FirstOrDefault(m=>m.Key!=key);var currentScore=smartHistory.GetValueOrDefault(key)?.Ewma;if(best?.Score is double score&&currentScore is double cost&&SmartSelection.ShouldSwitch(score,cost))await HotSwap(best,token);}
   }
  }catch(OperationCanceledException){}catch{status.Text=L("Smart monitoring will retry.","پایش هوشمند دوباره تلاش می‌کند.");}finally{smartChecking=false;if(ownsOperation)webOperation=false;PushWebState();}
 }
 async Task RecoverSmart(){if(webOperation||IsDisposed||busy||updating||!preferences.AutomaticServer)return;smartRecoverPending=false;webOperation=true;try{await ConnectAutomatic();}catch(Exception e){status.Text=L("Smart recovery failed: ","بازیابی هوشمند ناموفق بود: ")+SafeError(e.Message);}finally{webOperation=false;PushWebState();}}
 async Task<bool> TrySmartAlternatives(string key,CancellationToken token){foreach(var candidate in EligibleSmart().Where(m=>m.Key!=key)){if(await HotSwap(candidate,token)){status.Text=L("Connected · smart failover","متصل · جابه‌جایی هوشمند");return true;}NoteSmartFailure(candidate.Key);}return false;}
}
