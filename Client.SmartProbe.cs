using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 sealed class SmartProbe(int port,Process? process=null,CoreJob? job=null,string? folder=null):ISmartProbe {
  public static async Task<ISmartProbe> Open(SmartNode node,CancellationToken token){
   var folder=SessionFiles.Create();Process? process=null;CoreJob? job=null;
   try{int port=FreePort();var prepared=await Prepare(node.Profile,port).WaitAsync(TimeSpan.FromSeconds(10),token);token.ThrowIfCancellationRequested();var outbound=Outbound(prepared.Config)!.AsObject();var tag=outbound["tag"]?.ToString()??"smart-proxy";outbound["tag"]=tag;var inbound=prepared.Config["inbounds"]![0]!["tag"]!.ToString();prepared.Config["routing"]=new JsonObject{["domainStrategy"]="AsIs",["rules"]=new System.Text.Json.Nodes.JsonArray(new JsonObject{["type"]="field",["inboundTag"]=new System.Text.Json.Nodes.JsonArray(inbound),["outboundTag"]=tag})};
    var file=Path.Combine(folder,"smart.json");File.WriteAllText(file,prepared.Config.ToJsonString());job=new CoreJob();process=CoreSecurity.Start(new ProcessStartInfo(Core("xray.exe")){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(Core("xray.exe")),RedirectStandardOutput=true,RedirectStandardError=true,ArgumentList={"run","-c",file}});job.Add(process);_=process.StandardOutput.ReadToEndAsync();_=process.StandardError.ReadToEndAsync();
    for(int i=0;i<50;i++){token.ThrowIfCancellationRequested();if(process.HasExited)throw new IOException("Smart probe engine stopped.");try{using var socket=new TcpClient();await socket.ConnectAsync(IPAddress.Loopback,port,token);SessionFiles.RemoveFile(file);return new SmartProbe(port,process,job,folder);}catch(SocketException){await Task.Delay(100,token);}}throw new TimeoutException("Smart probe startup timed out.");
   }catch{job?.Dispose();if(process!=null){try{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}catch{}process.Dispose();}SessionFiles.RemoveDirectory(folder);throw;}
  }
  static async Task Exact(Stream stream,Memory<byte> buffer,CancellationToken token){await stream.ReadExactlyAsync(buffer,token);}
  public async Task<(double Delay,double Handshake)> Http204(CancellationToken token){
   // TLS to the HTTPS test endpoint through SOCKS; never a direct connection.
   var watch=Stopwatch.StartNew();using var socket=new TcpClient();await socket.ConnectAsync(IPAddress.Loopback,port,token);using var stream=socket.GetStream();await stream.WriteAsync(new byte[]{5,1,0},token);var reply=new byte[2];await Exact(stream,reply,token);if(reply[0]!=5||reply[1]!=0)throw new IOException("SOCKS negotiation failed.");
   const string host="www.gstatic.com";var address=Encoding.ASCII.GetBytes(host);await stream.WriteAsync(new byte[]{5,1,0,3,(byte)address.Length}.Concat(address).Concat(new byte[]{1,187}).ToArray(),token);var header=new byte[4];await Exact(stream,header,token);if(header[0]!=5||header[1]!=0)throw new IOException("SOCKS tunnel failed.");int count=header[3] switch{1=>4,4=>16,3=>await DomainLength(stream,token),_=>throw new IOException("Invalid SOCKS response.")};await Exact(stream,new byte[count+2],token);
   using var tls=new SslStream(stream,false);var handshake=Stopwatch.StartNew();await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions{TargetHost=host},token);double handshakeMs=handshake.Elapsed.TotalMilliseconds;
   await tls.WriteAsync(Encoding.ASCII.GetBytes($"GET /generate_204 HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\nCache-Control: no-cache\r\n\r\n"),token);var bytes=new List<byte>();var one=new byte[1];while(bytes.Count<16384){if(await tls.ReadAsync(one,token)==0)throw new IOException("Incomplete HTTP headers.");bytes.Add(one[0]);int n=bytes.Count;if(n>=4&&bytes[n-4]==13&&bytes[n-3]==10&&bytes[n-2]==13&&bytes[n-1]==10)break;}
   var text=Encoding.ASCII.GetString(bytes.ToArray());var line=text.Split('\r','\n')[0];if(!line.StartsWith("HTTP/1.",StringComparison.Ordinal)||line.Split(' ').ElementAtOrDefault(1)!="204")throw new IOException("Probe did not return HTTP 204.");return(watch.Elapsed.TotalMilliseconds,handshakeMs);
  }
  static async Task<int> DomainLength(Stream stream,CancellationToken token){var one=new byte[1];await Exact(stream,one,token);return one[0];}
  public async Task<double> Throughput(CancellationToken token){
   using var handler=new SocketsHttpHandler{UseProxy=true,Proxy=new WebProxy($"socks5://127.0.0.1:{port}"),AllowAutoRedirect=false};using var http=new HttpClient(handler){Timeout=Timeout.InfiniteTimeSpan};using var request=new HttpRequestMessage(HttpMethod.Get,"https://speed.cloudflare.com/__down?bytes=102400");request.Headers.Range=new(0,102399);request.Headers.TryAddWithoutValidation("Accept-Encoding","identity");var watch=Stopwatch.StartNew();using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength is long size&&size!=102400)throw new IOException("Invalid throughput probe size.");using var input=await response.Content.ReadAsStreamAsync(token);var buffer=new byte[4096];int total=0;while(total<102400){int read=await input.ReadAsync(buffer.AsMemory(0,Math.Min(buffer.Length,102400-total)),token);if(read==0)throw new IOException("Incomplete throughput probe.");total+=read;}return(total/1024d)/Math.Max(.001,watch.Elapsed.TotalSeconds);
  }
  public async ValueTask DisposeAsync(){job?.Dispose();if(process!=null){try{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}catch{}process.Dispose();}if(folder!=null)SessionFiles.RemoveDirectory(folder);}
 }
 static async Task<double?> SmartTcp(SmartNode node,CancellationToken token){try{var host=IcmpHost(node.Profile);var settings=Outbound(node.Profile)?["settings"];var value=settings?["vnext"]?[0]?["port"]??settings?["servers"]?[0]?["port"];if(!int.TryParse(value?.ToString(),out var port))return null;using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(2500);using var socket=new TcpClient();var watch=Stopwatch.StartNew();await socket.ConnectAsync(host,port,deadline.Token);return watch.Elapsed.TotalMilliseconds;}catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}catch{return null;}}
}
