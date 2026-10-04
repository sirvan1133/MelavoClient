using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 readonly CancellationTokenSource pingCancellation=new();bool pingTesting;readonly HashSet<string> latencyFailed=new();
 async Task CheckLatency(bool all=false){
  if(preview||busy||updating||pingTesting||(!all&&servers.SelectedItems.Count==0))return;
  var targets=(all?Profiles.ToArray():new[]{Profiles[(int)servers.SelectedItems[0].Tag!]}).Select(p=>(Profile:p,Key:ProfileKey(p))).ToArray();
  if(targets.Length==0)return;
  pingTesting=true;SetControls();int completed=0,responding=0;using var gate=new SemaphoreSlim(4);
  try{await Task.WhenAll(targets.Select(async target=>{
   await gate.WaitAsync(pingCancellation.Token);
   try{var ms=await MeasureIcmpProfile(target.Profile,pingCancellation.Token);latency[target.Key]=ms;latencyFailed.Remove(target.Key);responding++;}
   catch(OperationCanceledException){throw;}catch{latency.Remove(target.Key);latencyFailed.Add(target.Key);}finally{gate.Release();}
   completed++;if(!IsDisposed){FillServers();status.Text=L($"ICMP ping: {completed}/{targets.Length} tested · {responding} responding",$"پینگ ICMP: {completed}/{targets.Length} بررسی شد · {responding} پاسخ داد");}
  }));}catch(OperationCanceledException){}finally{pingTesting=false;if(!IsDisposed)SetControls();}
 }
 static string IcmpHost(JsonObject profile){
  var settings=Outbound(profile)?["settings"];
  var host=settings?["vnext"]?[0]?["address"]?.ToString()??settings?["servers"]?[0]?["address"]?.ToString();
  if(string.IsNullOrWhiteSpace(host)){var endpoint=settings?["peers"]?[0]?["endpoint"]?.ToString();if(Uri.TryCreate("udp://"+endpoint,UriKind.Absolute,out var uri))host=uri.Host;}
  if(string.IsNullOrWhiteSpace(host))throw new InvalidOperationException("Server address is missing.");
  return host.Trim('[',']');
 }
 static async Task<long> MeasureIcmpProfile(JsonObject profile,CancellationToken cancel){
  var addresses=await Dns.GetHostAddressesAsync(IcmpHost(profile),cancel);var results=new List<long>();
  foreach(var address in addresses.OrderBy(a=>a.AddressFamily==AddressFamily.InterNetwork?0:1)){
   using var ping=new System.Net.NetworkInformation.Ping();
   for(int i=0;i<3;i++){cancel.ThrowIfCancellationRequested();try{
    var reply=await ping.SendPingAsync(address,TimeSpan.FromSeconds(2),new byte[32],new System.Net.NetworkInformation.PingOptions(128,false),cancel);
    if(reply.Status==System.Net.NetworkInformation.IPStatus.Success)results.Add(reply.RoundtripTime);
   }catch(OperationCanceledException){throw;}catch(System.Net.NetworkInformation.PingException){}}
   if(results.Count>0)break;
  }
  if(results.Count==0)throw new InvalidOperationException("No ICMP reply.");
  return results.OrderBy(v=>v).ElementAt(results.Count/2);
 }
 static async Task<long> MeasureTunnel(int port,CancellationToken cancel){
  // Always proxy through the selected configuration. No direct fallback.
  var results=new List<long>();Exception? last=null;
  using var handler=new SocketsHttpHandler{UseProxy=true,Proxy=new WebProxy($"socks5://127.0.0.1:{port}"),ConnectTimeout=TimeSpan.FromSeconds(6),PooledConnectionLifetime=TimeSpan.Zero};using var http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(8)};
  foreach(var endpoint in new[]{"https://www.gstatic.com/generate_204","https://www.cloudflare.com/cdn-cgi/trace"}){
   for(int sample=0;sample<3;sample++){cancel.ThrowIfCancellationRequested();try{using var request=new HttpRequestMessage(HttpMethod.Get,endpoint);request.Headers.ConnectionClose=true;var watch=Stopwatch.StartNew();using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancel);response.EnsureSuccessStatusCode();results.Add(Math.Max(1,watch.ElapsedMilliseconds));}catch(OperationCanceledException)when(cancel.IsCancellationRequested){throw;}catch(Exception e){last=e;if(results.Count==0)break;}}
   if(results.Count>0)break;
  }
  if(results.Count==0)throw new Exception("No HTTPS response was received through the tunnel.",last);return results.OrderBy(v=>v).ElementAt(results.Count/2);
 }
 static async Task<long> MeasureProfile(JsonObject profile,CancellationToken cancel){
  var folder=SessionFiles.Create();var path=Path.Combine(folder,"probe.json");Process? process=null;using var probeJob=new CoreJob();
  try{cancel.ThrowIfCancellationRequested();var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();var prepared=await Prepare(profile,port);cancel.ThrowIfCancellationRequested();var outbound=Outbound(prepared.Config) as JsonObject??throw new Exception("The tunnel outbound is missing.");var tag=outbound["tag"]?.ToString();if(string.IsNullOrWhiteSpace(tag)){tag="melavo-latency";outbound["tag"]=tag;}var inboundTag=prepared.Config["inbounds"]![0]!["tag"]!.ToString();prepared.Config["routing"]=new JsonObject{["domainStrategy"]="AsIs",["rules"]=new JsonArray(new JsonObject{["type"]="field",["inboundTag"]=new JsonArray(inboundTag),["outboundTag"]=tag})};File.WriteAllText(path,prepared.Config.ToJsonString());var info=new ProcessStartInfo(Core("xray.exe")){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(Core("xray.exe")),RedirectStandardOutput=true,RedirectStandardError=true,ArgumentList={"run","-c",path}};process=CoreSecurity.Start(info);probeJob.Add(process);var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();bool ready=false;for(int i=0;i<50;i++){cancel.ThrowIfCancellationRequested();if(process.HasExited)throw new Exception("The tunnel test engine did not start.");try{using var socket=new TcpClient();await socket.ConnectAsync(IPAddress.Loopback,port,cancel);ready=true;break;}catch(SocketException){await Task.Delay(100,cancel);}}if(!ready)throw new Exception("The tunnel test port did not become ready.");return await MeasureTunnel(port,cancel);
  }finally{if(process!=null){try{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}catch{}process.Dispose();}SessionFiles.RemoveDirectory(folder);}
 }
 public static async Task<int> LatencyChecks(string file){var profiles=Parse(File.ReadAllText(file));var report=new List<string>();int failures=0;foreach(var protocol in new[]{"vless","vmess","wireguard"}){var profile=profiles.FirstOrDefault(p=>Outbound(p)?["protocol"]?.ToString()==protocol);if(profile==null)continue;try{var original=profile.ToJsonString();var ms=await MeasureProfile(profile,CancellationToken.None);if(original!=profile.ToJsonString())throw new Exception("Test modified original profile");report.Add($"PASS: {protocol} tunnel HTTPS median = {ms} ms");}catch(Exception e){failures++;report.Add($"FAIL: {protocol}: {SafeError(e.Message)}");}}var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int unusedPort=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();try{await MeasureTunnel(unusedPort,CancellationToken.None);failures++;report.Add("FAIL: direct fallback detected");}catch{report.Add("PASS: unavailable proxy fails without direct fallback");}File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"latency-checks.txt"),report);return failures;}
}
