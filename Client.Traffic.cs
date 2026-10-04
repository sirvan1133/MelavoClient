using System.Net;
using System.Text;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed partial class Client {
 int statsPort;string statsInboundTag="melavo-socks";bool samplingTraffic;long socksRx,socksTx;
 static int FreePort(){var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return port;}
 static void EnableStats(JsonObject config,int port){
  config["stats"]=new JsonObject();config["api"]=new JsonObject{["tag"]="melavo-api",["services"]=new JsonArray("StatsService")};
  if(config["policy"] is not JsonObject)config["policy"]=new JsonObject();if(config["policy"]!["system"] is not JsonObject)config["policy"]!["system"]=new JsonObject();config["policy"]!["system"]!["statsInboundUplink"]=true;config["policy"]!["system"]!["statsInboundDownlink"]=true;
  if(string.IsNullOrWhiteSpace(config["inbounds"]![0]!["tag"]?.ToString()))config["inbounds"]![0]!["tag"]="melavo-socks";
  config["inbounds"]!.AsArray().Add(new JsonObject{["tag"]="melavo-api-in",["listen"]="127.0.0.1",["port"]=port,["protocol"]="dokodemo-door",["settings"]=new JsonObject{["address"]="127.0.0.1"}});
  if(config["routing"] is not JsonObject)config["routing"]=new JsonObject();
  if(config["routing"]!["rules"] is not JsonArray)config["routing"]!["rules"]=new JsonArray();
  config["routing"]!["rules"]!.AsArray().Insert(0,new JsonObject{["type"]="field",["inboundTag"]=new JsonArray("melavo-api-in"),["outboundTag"]="melavo-api"});
 }
 static Dictionary<string,long> DecodeStats(byte[] data){
  var values=new Dictionary<string,long>();int cursor=0;
  ulong Varint(ref int at,int end){ulong value=0;for(int shift=0;shift<64;shift+=7){if(at>=end)throw new InvalidDataException("Invalid statistics response.");byte b=data[at++];value|=(ulong)(b&127)<<shift;if(b<128)return value;}throw new InvalidDataException();}
  void Skip(int wire,ref int at,int end){switch(wire){case 0:Varint(ref at,end);break;case 1:at+=8;break;case 2:var count=checked((int)Varint(ref at,end));at+=count;break;case 5:at+=4;break;default:throw new InvalidDataException();}if(at>end)throw new InvalidDataException();}
  while(cursor<data.Length){if(data.Length-cursor<5||data[cursor++]!=0)throw new InvalidDataException();int length=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(cursor,4));cursor+=4;int end=checked(cursor+length);if(end>data.Length||length<0)throw new InvalidDataException();
   while(cursor<end){var tag=Varint(ref cursor,end);if(tag!=10){Skip((int)(tag&7),ref cursor,end);continue;}int statLength=checked((int)Varint(ref cursor,end));int statEnd=checked(cursor+statLength);if(statEnd>end)throw new InvalidDataException();string name="";long value=0;
    while(cursor<statEnd){var field=Varint(ref cursor,statEnd);if(field==10){int count=checked((int)Varint(ref cursor,statEnd));if(cursor+count>statEnd)throw new InvalidDataException();name=Encoding.UTF8.GetString(data,cursor,count);cursor+=count;}else if(field==16)value=checked((long)Varint(ref cursor,statEnd));else Skip((int)(field&7),ref cursor,statEnd);}
    if(name!="")values[name]=value;
   }
  }return values;
 }
 static async Task<(long Rx,long Tx)> QueryTraffic(int port,string inboundTag="melavo-socks"){
  using var handler=new SocketsHttpHandler{UseProxy=false};using var http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(2)};
  // QueryStatsRequest: an empty pattern returns counters without resetting them.
  using var request=new HttpRequestMessage(HttpMethod.Post,$"http://127.0.0.1:{port}/xray.app.stats.command.StatsService/QueryStats"){Version=HttpVersion.Version20,VersionPolicy=HttpVersionPolicy.RequestVersionExact,Content=new ByteArrayContent(new byte[5])};
  request.Content.Headers.ContentType=new("application/grpc");request.Headers.TryAddWithoutValidation("TE","trailers");
  using var response=await http.SendAsync(request);response.EnsureSuccessStatusCode();var bytes=await response.Content.ReadAsByteArrayAsync();if(bytes.Length>100000)throw new InvalidDataException();
  if(response.TrailingHeaders.TryGetValues("grpc-status",out var statuses)&&statuses.FirstOrDefault()!="0")throw new InvalidDataException("Statistics API failed.");
  var counters=DecodeStats(bytes);return(counters.GetValueOrDefault($"inbound>>>{inboundTag}>>>traffic>>>downlink"),counters.GetValueOrDefault($"inbound>>>{inboundTag}>>>traffic>>>uplink"));
 }
 async void SampleSocksTraffic(){
  if(samplingTraffic||xray==null||statsPort==0)return;samplingTraffic=true;var current=xray;int port=statsPort;var tag=statsInboundTag;
  try{var totals=await QueryTraffic(port,tag);if(IsDisposed||!ReferenceEquals(current,xray))return;var now=DateTime.UtcNow;double seconds=Math.Max(.1,(now-sampleAt).TotalSeconds);
   Metric("download",sampled?Bytes((long)(Math.Max(0,totals.Rx-socksRx)/seconds))+"/s":"0 B/s");Metric("upload",sampled?Bytes((long)(Math.Max(0,totals.Tx-socksTx)/seconds))+"/s":"0 B/s");
   totalRx=socksRx=totals.Rx;totalTx=socksTx=totals.Tx;sampleAt=now;sampled=true;Metric("usage",Bytes(totalRx+totalTx));Metric("rx",Bytes(totalRx));Metric("tx",Bytes(totalTx));
  }catch{if(!IsDisposed&&ReferenceEquals(current,xray)){Metric("download","—");Metric("upload","—");}}finally{samplingTraffic=false;}
 }
 void ResetTraffic(){statsPort=0;sampled=false;totalRx=totalTx=socksRx=socksTx=lastRx=lastTx=0;foreach(var key in new[]{"download","upload","usage","rx","tx"})Metric(key,"—");}
}
