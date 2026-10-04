using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 public static int ManagementChecks(string sourceFile){
  string? liveUrl=null;var personal=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MelavoClient");SubscriptionStore.CheckDirectory=personal;try{liveUrl=SubscriptionStore.Read().FirstOrDefault()?.Url;}catch{}finally{SubscriptionStore.CheckDirectory=null;}
  int failures=0;var report=new List<string>();var isolated=Path.Combine(Path.GetTempPath(),"Melavo-management-"+Guid.NewGuid());SubscriptionStore.CheckDirectory=isolated;
  try{using var client=new Client{ShowInTaskbar=false,Opacity=0};client.Shown+=async(_,_)=>{
   try{void Assert(bool ok,string name){if(!ok)throw new Exception(name);report.Add("PASS: "+name);}
    var source=Parse(File.ReadAllText(sourceFile));
    Assert(!await client.AddSubscription("Bad","invalid"),"invalid URL rejected");
    Assert(!await client.AddSubscription("Failure","https://example.invalid",fetcher:_=>Task.FromException<List<JsonObject>>(new Exception("fixture fetch failure"))),"fetch failure keeps previous state");
    Assert(client.subscriptions.Count==0&&SubscriptionStore.Read().Count==0,"failed additions do not persist");
    Assert(await client.AddSubscription("Test subscription","https://example.invalid/sub/test",fetcher:_=>Task.FromResult(source)),"add subscription flow succeeds");
    Assert(SubscriptionStore.Read().Count==1&&client.groups.Items.Count==1&&client.servers.Items.Count==source.Count,"added subscription is persisted and visible");
    Assert(!await client.AddSubscription("Duplicate","https://example.invalid/sub/test"),"duplicate URL rejected visibly");
    var subscription=client.subscriptions[0];Assert(await client.SaveSubscriptionEdit(subscription,"Renamed subscription",subscription.Url),"subscription can be renamed without fetching");Assert(SubscriptionStore.Read()[0].Name=="Renamed subscription","subscription name edit persisted");
    subscription=client.subscriptions[0];Assert(await client.SaveSubscriptionEdit(subscription,"Renamed subscription","https://example.invalid/sub/changed",fetcher:_=>Task.FromResult(source.Select(p=>(JsonObject)p.DeepClone()).ToList()),feedback:_=>{}),"subscription URL edit fetches before saving");Assert(SubscriptionStore.Read()[0].Url.EndsWith("/changed")&&client.groups.Items.Count==1,"edited subscription remains visible");
    subscription=client.subscriptions[0];Assert(!await client.SaveSubscriptionEdit(subscription,"Failed edit","https://example.invalid/sub/fail",fetcher:_=>Task.FromException<List<JsonObject>>(new Exception("fixture fetch failure")),feedback:_=>{}),"failed subscription edit is rejected");Assert(SubscriptionStore.Read()[0].Url.EndsWith("/changed"),"failed subscription edit preserves saved link");
    client.AddSingleConfig((JsonObject)source[0].DeepClone());Assert(SubscriptionStore.Read().Count==2,"single configuration saved beside subscriptions");client.SetConfigScope(true);Assert(client.groups.Items.Count==1&&client.servers.Items.Count==1&&client.Profiles[0]["remarks"]!.ToString()==source[0]["remarks"]!.ToString(),"single configuration scope is separate");Assert(!client.groups.Items.Cast<SubscriptionGroup>().Any(IsSubscription),"single scope excludes subscription groups");client.SetConfigScope(false);Assert(client.groups.Items.Count==1&&client.groups.SelectedItem is SubscriptionGroup visible&&IsSubscription(visible),"subscription scope excludes single configurations");var qr=CreateQrPng(source[0].ToJsonString());Assert(qr.Length>8&&qr.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"configuration QR exports as PNG");
    var group=client.subscriptions.First(IsSubscription);var edit=(JsonObject)group.Profiles[0].DeepClone();edit["remarks"]="Edited local configuration";await client.ValidateEdit(edit);client.SaveConfigEdit(group,0,edit);
    var saved=SubscriptionStore.Read()[0];Assert(saved.Profiles[0]["remarks"]!.ToString()=="Edited local configuration","validated edit persisted");saved.SourceProfiles=source.Select(x=>(JsonObject)x.DeepClone()).ToList();ApplyOverrides(saved);Assert(saved.Profiles[0]["remarks"]!.ToString()=="Edited local configuration","local edit survives subscription refresh");
    bool invalid=false;try{await client.ValidateEdit(new JsonObject());}catch{invalid=true;}Assert(invalid,"invalid configuration rejected before save");
    client.CommitGroups(new(),null);Assert(SubscriptionStore.Read().Count==0&&client.groups.Items.Count==0&&client.servers.Items.Count==0,"delete last subscription clears storage and list");
    if(liveUrl!=null){Assert(await client.AddSubscription("Isolated live fetch",liveUrl),"live subscription HTTPS fetch, parse and save");Assert(SubscriptionStore.Read().Count==1,"live fetched subscription persisted in isolated profile");client.CommitGroups(new(),null);}
    SubscriptionStore.SelfTest();Assert(true,"encrypted persistence roundtrip");
   }catch(Exception e){failures++;report.Add("FAIL: "+SafeError(e.Message));}finally{client.Close();}
  };Application.Run(client);}finally{SubscriptionStore.CheckDirectory=null;if(Directory.Exists(isolated))Directory.Delete(isolated,true);File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"management-checks.txt"),report);}return failures;
 }
}
