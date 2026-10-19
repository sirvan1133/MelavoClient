using System.Text.Json;
using Microsoft.Web.WebView2.Core;
namespace MelavoClient;
sealed partial class Client {
 public static int PortraitChecks(){
  using var client=new Client(true,true){ShowInTaskbar=false,Opacity=0,webReviewDirectory=Path.Combine(Path.GetTempPath(),"Melavo-Portrait-"+Guid.NewGuid().ToString("N"))};
  var report=new List<string>();client.Show();var task=Run();while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(10);}try{task.GetAwaiter().GetResult();return 0;}catch(Exception e){report.Add("FAIL: "+e);return 1;}finally{File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"portrait-checks.txt"),report);client.webOperation=false;client.Close();}
  async Task Run(){
   var wait=System.Diagnostics.Stopwatch.StartNew();while(!client.webReady){if(wait.ElapsedMilliseconds>25000)throw new Exception("Portrait UI did not initialize.");await Task.Delay(40);}
   var core=client.webShell!.CoreWebView2;
   async Task Check(string expression,string name){if(await core.ExecuteScriptAsync(expression)!="true")throw new Exception(name+": "+await core.ExecuteScriptAsync("JSON.stringify({h:innerHeight,list:(()=>{let e=document.querySelector('#serverListContainer');return [e.clientHeight,e.scrollHeight,e.scrollTop,e.getBoundingClientRect().bottom]})(),last:document.querySelector('.node-item:last-child')?.getBoundingClientRect().bottom})"));report.Add("PASS: "+name);}
   foreach(int dpi in new[]{96,120,144,192})foreach(var area in new[]{new Rectangle(0,0,1280,696),new Rectangle(0,0,1920,1040),new Rectangle(-2560,0,2560,1400),new Rectangle(0,0,3840,2100)}){
    var size=PortraitSize(area,dpi);if(size.Width>area.Width||size.Height>area.Height||size.Width>=size.Height)throw new Exception("Monitor size bounds");}report.Add("PASS: portrait sizing fits four monitor work areas at 100/125/150/200 percent");
   client.CommitGroups(new(),null);client.PushWebState();await Task.Delay(150);
   foreach(var viewport in new[]{new Size(410,860),new Size(360,640),new Size(320,568)})foreach(string language in new[]{"en","fa"}){
    await core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride",JsonSerializer.Serialize(new{width=viewport.Width,height=viewport.Height,deviceScaleFactor=1,mobile=false}));
    await core.ExecuteScriptAsync($"setLang('{language}',null,false);portraitHomeView()");await Task.Delay(400);
    await Check("document.documentElement.scrollWidth<=innerWidth+1 && document.documentElement.scrollHeight<=innerHeight+1","No document overflow "+viewport+" "+language);
    await Check("(()=>{let r=document.querySelector('.portrait-footer').getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight})()","Home actions visible "+viewport+" "+language);
    await core.ExecuteScriptAsync("portraitOpenServers()");await Task.Delay(400);
    await Check("(()=>{let r=document.querySelector('#serverListContainer').getBoundingClientRect();return r.height>100&&r.bottom<=innerHeight+1})()","Server sheet has usable scroll space "+viewport+" "+language);
    await core.ExecuteScriptAsync("document.querySelector('#portraitSettings').click()");await Task.Delay(250);
    await Check("document.documentElement.scrollWidth<=innerWidth+1&&document.querySelector('#settingsHub').getBoundingClientRect().bottom<=innerHeight+1","Settings fit "+viewport+" "+language);
    await core.ExecuteScriptAsync("openModal('addSubModal')");await Task.Delay(250);
    await Check("document.querySelector('#addSubModal .modal').getBoundingClientRect().bottom<=innerHeight+1","Add subscription dialog fits "+viewport+" "+language);
    await core.ExecuteScriptAsync("closeModal('addSubModal');portraitHomeView()");
   }
   var fixture=new SubscriptionGroup{Name="Scroll fixture",Url="https://example.invalid/sub"};for(int i=0;i<100;i++){var profile=Parse("vless://11111111-1111-1111-1111-111111111111@127.0.0.1:443?security=none&type=tcp#Fixture")[0];profile["remarks"]="Server "+i;fixture.Profiles.Add(profile);}client.CommitGroups(new(){fixture},fixture.Id);client.PushWebState();await Task.Delay(200);await core.ExecuteScriptAsync("portraitOpenServers();document.querySelector('#searchInput').blur()");await Task.Delay(450);
   await Check("document.querySelectorAll('.node-item').length===100","All real host rows available in portrait sheet");
   await core.ExecuteScriptAsync("document.querySelector('#serverListContainer').style.scrollBehavior='auto';document.querySelector('#serverListContainer').scrollTop=100000");await Task.Delay(200);
   await Check("document.querySelector('#serverListContainer').scrollTop>0&&document.querySelector('.node-item:last-child').getBoundingClientRect().bottom<=document.querySelector('#serverListContainer').getBoundingClientRect().bottom+2","Last of 100 servers reachable");
   await core.CallDevToolsProtocolMethodAsync("Emulation.clearDeviceMetricsOverride","{}");
   using(var shot=File.Create(Path.Combine(AppContext.BaseDirectory,"review-portrait-servers.png")))await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,shot);
   client.CommitGroups(new(),null);client.PushWebState();await core.ExecuteScriptAsync("setLang('en',null,false);portraitHomeView()");await Task.Delay(400);
   await Check("window.__melavoErrors.length===0","No JavaScript errors");
   using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"review-portrait-customer.png"));await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,file);
  }
 }
}
