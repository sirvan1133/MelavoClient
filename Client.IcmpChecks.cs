using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 public static async Task<int> IcmpChecks(){
  var report=new List<string>();int failures=0;
  void Assert(bool value,string name){report.Add((value?"PASS: ":"FAIL: ")+name);if(!value)failures++;}
  foreach(var protocol in new[]{"vless","vmess","trojan","shadowsocks","wireguard"}){
   var settings=protocol=="wireguard"?new JsonObject{["peers"]=new JsonArray(new JsonObject{["endpoint"]="[::1]:51820"})}:new JsonObject{[protocol is "vless" or "vmess"?"vnext":"servers"]=new JsonArray(new JsonObject{["address"]="127.0.0.1"})};
   var fixture=new JsonObject{["outbounds"]=new JsonArray(new JsonObject{["protocol"]=protocol,["settings"]=settings})};
   Assert(IcmpHost(fixture)==(protocol=="wireguard"?"::1":"127.0.0.1"),protocol+" server extraction");
   try{var ms=await MeasureIcmpProfile(fixture,CancellationToken.None);Assert(ms>=0&&ms<1000,protocol+" actual loopback ICMP reply");}catch{Assert(false,protocol+" actual loopback ICMP reply");}
   using var cancel=new CancellationTokenSource();cancel.Cancel();try{await MeasureIcmpProfile(fixture,cancel.Token);Assert(false,"cancellation");}catch(OperationCanceledException){Assert(true,"cancellation");}
  }
  File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"icmp-checks.txt"),report);return failures;
 }
}
