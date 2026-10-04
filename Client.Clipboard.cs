using System.Text;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){
  Control? focused=ActiveControl;while(focused is ContainerControl container&&container.ActiveControl!=null)focused=container.ActiveControl;
  if(currentPage=="Home"&&!busy&&!updating&&!pingTesting&&xray==null){
   if(keyData==(Keys.Control|Keys.V)){
    try{if(Clipboard.ContainsText()){var text=Clipboard.GetText().Trim();
     if(!singleConfigScope&&Uri.TryCreate(text,UriKind.Absolute,out var uri)&&uri.Scheme=="https"){using var dialog=CreateSubscriptionDialog(text);dialog.ShowDialog(this);return true;}
     if(singleConfigScope&&(text.StartsWith("vless://",StringComparison.OrdinalIgnoreCase)||text.StartsWith("vmess://",StringComparison.OrdinalIgnoreCase)||text.StartsWith("trojan://",StringComparison.OrdinalIgnoreCase)||text.StartsWith('{'))){ImportClipboardConfig(text);return true;}
    }}catch(Exception e){MessageBox.Show(this,SafeError(e.Message),L("Clipboard unavailable","کلیپ‌بورد در دسترس نیست"));return true;}
   }
   if(keyData==Keys.Delete&&singleConfigScope&&focused is not TextBoxBase&&focused is not ModernInput){DeleteSubscription();return true;}
  }
  return base.ProcessCmdKey(ref msg,keyData);
 }
 async void ImportClipboardConfig(string text){
  busy=true;SetControls();try{var profiles=Parse(text);if(profiles.Count!=1)throw new Exception(L("Enter exactly one configuration.","فقط یک کانفیگ وارد کنید."));await ValidateEdit(profiles[0]);if(string.IsNullOrWhiteSpace(profiles[0]["remarks"]?.ToString()))profiles[0]["remarks"]=$"Config {SingleConfigGroup?.Profiles.Count+1??1}";busy=false;AddSingleConfig(profiles[0]);}catch(Exception e){MessageBox.Show(this,SafeError(e.Message),L("Invalid configuration","کانفیگ نامعتبر است"));}finally{busy=false;SetControls();}
 }
 static string ShareSnapshot(JsonObject profile){var clone=(JsonObject)profile.DeepClone();clone.Remove("_shareLink");clone.Remove("_shareSnapshot");return clone.ToJsonString();}
 static void RememberShareLink(JsonObject profile,string link){profile["_shareLink"]=link;profile["_shareSnapshot"]=ShareSnapshot(profile);}
 static string ShareText(JsonObject profile){
  if(profile["_shareLink"] is JsonValue original&&profile["_shareSnapshot"]?.ToString()==ShareSnapshot(profile))return original.ToString();
  var outbound=Outbound(profile);if(outbound==null)return ShareSnapshot(profile);
  string S(JsonNode? n)=>n?.ToString()??"";
  var protocol=S(outbound["protocol"]);var endpoint=outbound["settings"]?[protocol=="trojan"?"servers":"vnext"]?[0];
  if(endpoint==null||protocol is not ("vless" or "vmess" or "trojan"))return ShareSnapshot(profile);
  var user=endpoint["users"]?[0];var stream=outbound["streamSettings"]??profile["streamSettings"];var network=S(stream?["network"]);if(network=="")network="tcp";var security=S(stream?["security"]);if(security=="")security="none";
  var transport=stream?[network+"Settings"];var tls=stream?[security+"Settings"];
  var host=S(transport?["headers"]?["Host"]??transport?["host"]);var path=S(transport?["path"]);
  if(protocol=="vmess"){
   var data=new JsonObject{["v"]="2",["ps"]=S(profile["remarks"]),["add"]=S(endpoint["address"]),["port"]=S(endpoint["port"]),["id"]=S(user?["id"]),["aid"]=S(user?["alterId"]),["scy"]=S(user?["security"]),["net"]=network,["type"]=S(transport?["header"]?["type"]),["host"]=host,["path"]=network=="grpc"?S(transport?["serviceName"]):path,["tls"]=security=="none"?"":security,["sni"]=S(tls?["serverName"]),["fp"]=S(tls?["fingerprint"]),["alpn"]=tls?["alpn"] is JsonArray alpn?string.Join(',',alpn.Select(S)):""};
   foreach(var key in new[]{network+"Settings","tlsSettings","realitySettings","sockopt"})if(stream?[key] is JsonObject options)data[key]=options.DeepClone();
   return "vmess://"+Convert.ToBase64String(Encoding.UTF8.GetBytes(data.ToJsonString()));
  }
  var query=new Dictionary<string,string>{{"type",network},{"security",security}};
  void Add(string key,JsonNode? value){if(value!=null&&S(value)!="")query[key]=S(value);}
  if(protocol=="vless"){query["encryption"]=S(user?["encryption"]) is {Length:>0} encryption?encryption:"none";Add("flow",user?["flow"]);}
  if(host!="")query["host"]=host;if(path!="")query["path"]=path;Add("serviceName",transport?["serviceName"]);Add("authority",transport?["authority"]);Add("headerType",transport?["header"]?["type"]);
  Add("sni",tls?["serverName"]);Add("fp",tls?["fingerprint"]);Add("pbk",tls?["publicKey"]);Add("sid",tls?["shortId"]);Add("spx",tls?["spiderX"]);Add("allowInsecure",tls?["allowInsecure"]);if(tls?["alpn"] is JsonArray protocols)query["alpn"]=string.Join(',',protocols.Select(S));
  var address=S(endpoint["address"]);if(address.Contains(':'))address="["+address.Trim('[',']')+"]";
  return protocol+"://"+Uri.EscapeDataString(S(protocol=="trojan"?endpoint["password"]:user?["id"]))+"@"+address+":"+S(endpoint["port"])+"?"+string.Join('&',query.Select(pair=>Uri.EscapeDataString(pair.Key)+"="+Uri.EscapeDataString(pair.Value)))+"#"+Uri.EscapeDataString(S(profile["remarks"]));
 }
}
