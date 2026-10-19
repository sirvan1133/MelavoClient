using System.Text.Json;
namespace MelavoClient;
sealed class SmartHistory {
 public double? Ewma {get;set;} public double FailCount {get;set;} public DateTime FailureAt {get;set;} public DateTime CooldownUntil {get;set;}
 public double DecayedFailures(DateTime now)=>FailCount*Math.Pow(.5,Math.Max(0,(now-FailureAt).TotalMinutes)/10);
 public void Fail(DateTime now){FailCount=DecayedFailures(now)+1;FailureAt=now;CooldownUntil=now.AddMinutes(2);}
}
sealed class SmartMetric {
 public string Id {get;init;}="";public string Key {get;init;}="";public List<double?> Samples {get;}=new();
 public double? TcpPing {get;set;} public double HandshakeMs {get;set;} public double? ThroughputKbps {get;set;} public string State {get;set;}="TESTING";
 public double Loss=>Samples.Count==0?1:Samples.Count(x=>!x.HasValue)/(double)Samples.Count;
 public double P90 {get{var values=Samples.Where(x=>x.HasValue).Select(x=>x!.Value).Order().ToArray();return values.Length==0?double.PositiveInfinity:values[Math.Max(0,(int)Math.Ceiling(values.Length*.9)-1)];}}
 public double Jitter {get{var pairs=Samples.Zip(Samples.Skip(1)).Where(x=>x.First.HasValue&&x.Second.HasValue).Select(x=>Math.Abs(x.First!.Value-x.Second!.Value)).ToArray();return pairs.Length==0?0:pairs.Average();}}
 public double BaseLatency=>P90+Jitter*2.5+HandshakeMs*.4;
 public double Preliminary(double failures)=>BaseLatency*Math.Pow(1+Loss*4,3)+failures*800;
 public double RawCost(double failures)=>Preliminary(failures)+5000/(ThroughputKbps!.Value+1);
 public double? Score {get;set;} public int Quality=>Score.HasValue?(int)Math.Clamp(Math.Round(100-Score.Value/15,MidpointRounding.AwayFromZero),0,100):0;
}
interface ISmartProbe:IAsyncDisposable {Task<(double Delay,double Handshake)> Http204(CancellationToken token);Task<double> Throughput(CancellationToken token);}
sealed class SmartHealth {
 public int ConsecutiveFailures {get;private set;}public int StalledSeconds {get;private set;}long rx,tx;
 public bool Observe(long received,long sent){StalledSeconds=received==rx&&sent>tx?StalledSeconds+1:0;rx=received;tx=sent;return StalledSeconds>=3;}
 public bool Check(bool success){ConsecutiveFailures=success?0:ConsecutiveFailures+1;return ConsecutiveFailures>=2;}
 public void Reset(){ConsecutiveFailures=StalledSeconds=0;rx=tx=0;}
}
sealed record SmartNode(string Id,string Key,System.Text.Json.Nodes.JsonObject Profile);
static class SmartSelection {
 public static bool ShouldSwitch(double candidate,double current)=>candidate<current*.75;
 public static IEnumerable<SmartMetric> Ranked(IEnumerable<SmartMetric> metrics)=>metrics.Where(m=>m.Score.HasValue&&double.IsFinite(m.Score.Value)).OrderBy(m=>m.State=="THROTTLED"?1:0).ThenBy(m=>m.Score);
 public static async Task<List<SmartMetric>> Run(SmartNode[] nodes,Dictionary<string,SmartHistory> history,Func<SmartNode,CancellationToken,Task<ISmartProbe>> open,Func<SmartNode,CancellationToken,Task<double?>> tcp,Action<string,int,int>? progress,CancellationToken token){
  var now=DateTime.UtcNow;var metrics=new List<SmartMetric>();using var gate=new SemaphoreSlim(10);int done=0;
  foreach(var node in nodes)history.TryAdd(node.Key,new SmartHistory());
  await Task.WhenAll(nodes.Select(async node=>{await gate.WaitAsync(token);try{
   var metric=new SmartMetric{Id=node.Id,Key=node.Key};lock(metrics)metrics.Add(metric);
   if(history[node.Key].CooldownUntil>now){metric.State="COOLDOWN";return;}
   try{await using var probe=await open(node,token);using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(2500);var first=await probe.Http204(deadline.Token);metric.Samples.Add(first.Delay);metric.HandshakeMs=first.Handshake;metric.State="VALID";}
   catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
   catch(Exception error){metric.Samples.Add(null);metric.TcpPing=await tcp(node,token);metric.State=(error is OperationCanceledException or TimeoutException)&&metric.TcpPing is <80?"FAKE_PING":"ZOMBIE";}
  }finally{gate.Release();progress?.Invoke("filter",Interlocked.Increment(ref done),nodes.Length);}}));
  var top5=metrics.Where(m=>m.State=="VALID").OrderBy(m=>m.Samples[0]).Take(5).ToArray();
  await Task.WhenAll(top5.Select(async metric=>{
   ISmartProbe? probe=null;try{probe=await open(nodes.First(n=>n.Id==metric.Id),token);
    for(int i=0;i<3;i++){await Task.Delay(100,token);using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(2500);try{metric.Samples.Add((await probe.Http204(deadline.Token)).Delay);}catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}catch{metric.Samples.Add(null);}}
   }catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}catch{while(metric.Samples.Count<4)metric.Samples.Add(null);}finally{if(probe!=null)await probe.DisposeAsync();}
  }));progress?.Invoke("stability",top5.Length,top5.Length);
  var finalists=top5.OrderBy(m=>m.Preliminary(history[m.Key].DecayedFailures(now))).Take(2).ToArray();
  foreach(var metric in finalists){token.ThrowIfCancellationRequested();try{await using var probe=await open(nodes.First(n=>n.Id==metric.Id),token);using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(8));metric.ThroughputKbps=await probe.Throughput(deadline.Token);}catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}catch{metric.ThroughputKbps=0;}
   metric.State=metric.ThroughputKbps<150?"THROTTLED":"READY";var old=history[metric.Key];var raw=metric.RawCost(old.DecayedFailures(DateTime.UtcNow));metric.Score=old.Ewma.HasValue?.35*raw+.65*old.Ewma.Value:raw;old.Ewma=metric.Score;
  }progress?.Invoke("score",finalists.Length,finalists.Length);return metrics;
 }
 public static Dictionary<string,SmartHistory> Load(string path){try{var info=new FileInfo(path);return info.Exists&&info.Length<2000000?JsonSerializer.Deserialize<Dictionary<string,SmartHistory>>(File.ReadAllText(path))??new():new();}catch{return new();}}
 public static void Save(string path,Dictionary<string,SmartHistory> history){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(history));File.Move(temp,path,true);}
}
