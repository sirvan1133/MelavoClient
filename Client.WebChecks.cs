using System.Text.Json;
using Microsoft.Web.WebView2.Core;
namespace MelavoClient;
sealed partial class Client {
 public static int WebChecks(bool layoutOnly=false){
  var folder=Path.Combine(Path.GetTempPath(),"Melavo-WebChecks-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(folder);using var client=new Client(true,true){ShowInTaskbar=false,Opacity=0,webReviewDirectory=folder};
  var report=new List<string>();client.Show();var task=Run();while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(10);}try{task.GetAwaiter().GetResult();File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"web-checks.txt"),report);return 0;}catch(Exception e){report.Add("FAIL: "+e);File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"web-checks.txt"),report);return 1;}finally{client.webOperation=false;client.Close();}
  async Task Run(){
   async Task Until(Func<bool> condition,int timeout=15000){var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(clock.ElapsedMilliseconds>timeout)throw new Exception("Web UI timeout. "+client.status.Text);await Task.Delay(30);}}
   try{await Until(()=>client.webReady,25000);}catch{if(client.webShell?.CoreWebView2!=null){var diagnostic=client.webShell.CoreWebView2.ExecuteScriptAsync("JSON.stringify({errors:window.__melavoErrors,body:document.body?.innerText?.slice(-1200),ready:typeof ready})");while(!diagnostic.IsCompleted){Application.DoEvents();Thread.Sleep(10);}report.Add("Frontend: "+diagnostic.GetAwaiter().GetResult());}throw;}var core=client.webShell!.CoreWebView2;
   async Task<string> Script(string code)=>await core.ExecuteScriptAsync(code);
   async Task Assert(string code,string name){var value=await Script(code);if(value!="true"){report.Add(await Script("JSON.stringify({viewport:[innerWidth,innerHeight],body:[document.documentElement.scrollWidth,document.documentElement.scrollHeight],parts:Array.from(document.querySelectorAll('.top-bar,#page-servers>*')).map(e=>({id:e.id,cls:e.className,height:e.clientHeight,scroll:e.scrollHeight,top:e.getBoundingClientRect().top,bottom:e.getBoundingClientRect().bottom}))})"));throw new Exception(name+": "+value);}report.Add("PASS: "+name);}
   async Task Capture(string name){await Task.Delay(550);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,output);}
   async Task<object?> Command(object message){using var doc=JsonDocument.Parse(JsonSerializer.Serialize(message));return await client.HandleWebCommand(doc.RootElement);}
   if(layoutOnly){
    var fixture=new SubscriptionGroup{Name="Scroll review",Url="https://example.invalid/review"};for(int i=0;i<100;i++){var item=Parse("vless://11111111-1111-1111-1111-111111111111@127.0.0.1:443?security=none&type=tcp#Offline")[0];item["remarks"]=$"Server {i+1:D3}";fixture.Profiles.Add(item);}client.CommitGroups(new(){fixture},fixture.Id);
    foreach(var size in new[]{new Size(1380,980),new Size(1240,780),new Size(1000,700)})foreach(var lang in new[]{"en","fa"}){
     client.Size=size;client.preferences.Language=lang;client.ApplyLanguage();client.PushWebState();await Script("setLang("+JsonSerializer.Serialize(lang)+",null,false);showPage('servers');filterView('all')");await Capture($"review-layout-{size.Width}-{lang}");
     await Assert("document.documentElement.scrollHeight<=innerHeight+1 && getComputedStyle(document.querySelector('#serverListContainer')).overflowY==='auto'","Only server list scrolls inside a fixed dashboard: "+size+" / "+lang);
     await Assert("document.querySelectorAll('.node-item').length===100 && !document.querySelector('#serverPager')","All 100 configurations present without pagination");
     await Script("document.querySelector('#serverListContainer').style.scrollBehavior='auto';document.querySelector('#serverListContainer').scrollTop=100000");await Task.Delay(160);await Assert("document.querySelector('#serverListContainer').scrollTop>0 && document.querySelector('.node-item:last-child').getBoundingClientRect().bottom<=document.querySelector('#serverListContainer').getBoundingClientRect().bottom+2","Last configuration reachable by internal scrolling");
     await Assert("getComputedStyle(document.body).userSelect==='none' && getComputedStyle(document.querySelector('#subNameInput')).userSelect==='text'","UI text cannot be selected; editable text remains selectable");
     await Assert("document.querySelectorAll('.nav-menu .nav-item').length===2 && document.querySelector('#page-update').closest('#settingsHub')!==null && document.querySelector('#page-logs').closest('#settingsHub')!==null","Updates and logs nested under Settings");
     await Script("showPage('update')");await Assert("document.querySelector('.nav-item[data-page=settings]').classList.contains('active') && document.querySelector('.settings-tab[data-page=update]').classList.contains('active')","Settings subtab navigation is synchronized");
    }
    var point=JsonSerializer.Deserialize<JsonElement>(await Script("(()=>{const r=document.querySelector('.top-bar-title h1').getBoundingClientRect();return {x:r.left+10,y:r.top+8};})()"));var mouseX=point.GetProperty("x").GetDouble();var mouseY=point.GetProperty("y").GetDouble();await core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type="mousePressed",x=mouseX,y=mouseY,button="right",buttons=2,clickCount=1}));await core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type="mouseMoved",x=mouseX+100,y=mouseY+90,button="right",buttons=2}));await core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",JsonSerializer.Serialize(new{type="mouseReleased",x=mouseX+100,y=mouseY+90,button="right",buttons=0,clickCount=1}));await Assert("window.getSelection().toString()===''","Right-button dragging does not select interface text");
    client.Size=new(1380,980);client.CommitGroups(new(),null);client.preferences.Language="en";client.ApplyLanguage();client.status.Text="Disconnected";client.PushWebState();await Script("setLang('en',null,false);setTheme('dark-blue',null,false);showPage('servers')");await Script("document.querySelector('#toastContainer').replaceChildren()");await Capture("review-web-customer");await Script("showPage('singbox')");await Capture("review-web-settings-hub");return;
   }
   client.CommitGroups(new(),null);client.PushWebState();await Task.Delay(180);
   await Assert("allNodes().length===0 && document.querySelectorAll('.node-item').length===0","Customer starts without bundled subscriptions or configurations");
   await Capture("review-web-customer");
   client.LoadPreview();client.PushWebState();await Task.Delay(200);
   await Assert("document.querySelectorAll('.sub-group[data-sub-id]').length===1 && document.querySelectorAll('.node-item').length>0 && allNodes().length===3","Actual host profiles populate the accordion");
   var groupId=client.subscriptions.First(IsSubscription).Id;
   await Script("toggleSubGroup("+JsonSerializer.Serialize(groupId)+")");await Task.Delay(380);
   await Assert("!document.querySelector('.sub-group').classList.contains('open') && getComputedStyle(document.querySelector('.sub-group-nodes-list')).gridTemplateRows==='0px'","Accordion collapses to zero height with its CSS transition");
   client.Metric("download","125 KB/s");client.PushWebState();await Task.Delay(180);
   await Assert("!document.querySelector('.sub-group').classList.contains('open')","Statistics update preserves accordion state");
   await Script("toggleSubGroup("+JsonSerializer.Serialize(groupId)+")");await Task.Delay(380);
   foreach(var language in new[]{"en","fa"}){
    client.preferences.Language=language;client.ApplyLanguage();client.PushWebState();await Script("setLang("+JsonSerializer.Serialize(language)+",null,false)");
    foreach(var page in new[]{"servers","singbox","update","logs","settings"}){
     await Script("showPage("+JsonSerializer.Serialize(page)+")");await Task.Delay(80);
     await Assert("document.documentElement.scrollWidth<=innerWidth+1","No horizontal overflow: "+language+" / "+page);
     if(language=="en")await Assert("!/[\\u0600-\\u06ff]/.test(document.body.innerText)","English page contains no Persian UI: "+page);
     await Capture("review-web-"+page+"-"+language);
    }
    await Script("showPage('servers')");
    foreach(var modal in new[]{"addSubModal","addConfigModal","editConfigModal"}){
     await Script("openModal("+JsonSerializer.Serialize(modal)+")");await Task.Delay(150);
     await Assert("document.querySelector('.modal-overlay.show .modal').getBoundingClientRect().bottom<=innerHeight+1","Modal fits viewport: "+language+" / "+modal);
     if(language=="en")await Assert("!/[\\u0600-\\u06ff]/.test(document.querySelector('.modal-overlay.show').innerText)","English modal contains no Persian UI: "+modal);
     await Capture("review-web-"+modal+"-"+language);await Script("closeModal("+JsonSerializer.Serialize(modal)+")");
    }
   }
   client.preferences.Language="en";client.ApplyLanguage();await Script("setLang('en',null,false);showPage('settings')");
   foreach(var theme in webThemes){await Script("setTheme("+JsonSerializer.Serialize(theme)+",null,false)");await Assert("document.documentElement.style.getPropertyValue('--accent')==='' && document.documentElement.style.getPropertyValue('--bg-primary')===''","Theme clears previous custom colors: "+theme);await Capture("review-web-theme-"+theme);}
   await Script("setTheme('dark-blue',null,false);showPage('servers')");
   var first=client.subscriptions.First(IsSubscription);first.Profiles[0]=Parse("vless://11111111-1111-1111-1111-111111111111@127.0.0.1:443?security=none&type=tcp#Offline%20share")[0];var nodeId=WebId(first,0);
   await Command(new{action="select",nodeId});client.PushWebState();
   var share=await Command(new{action="share",nodeId});var shareJson=JsonSerializer.Serialize(share);using(var shared=JsonDocument.Parse(shareJson)){var text=shared.RootElement.GetProperty("text").GetString()!;if(Parse(text).Count!=1||shared.RootElement.GetProperty("qr").GetString()?.Length<100)throw new Exception("Share payload/QR invalid.");}
   report.Add("PASS: share bridge returns the complete parseable configuration and generated QR");
   await Script("void showShare("+JsonSerializer.Serialize(nodeId)+")");await Task.Delay(650);await Assert("document.querySelector('#shareModal').classList.contains('show') && document.querySelector('#shareText').value.startsWith('vless://')","Share opens its HTML dialog through the real RPC bridge");await Capture("review-web-share");await Script("closeModal('shareModal')");
   await Script("showContextMenu({preventDefault(){},stopPropagation(){},clientX:innerWidth-5,clientY:innerHeight-5},"+JsonSerializer.Serialize(nodeId)+")");
   await Assert("document.querySelector('#contextMenu').getBoundingClientRect().right<=innerWidth && document.querySelector('#contextMenu').getBoundingClientRect().bottom<=innerHeight","Context menu stays within window edges");await Capture("review-web-context");
   await Script("document.querySelector('#contextMenu').classList.remove('show')");
   await Command(new{action="add-config",text="vless://11111111-1111-1111-1111-111111111111@127.0.0.1:443?security=none&type=tcp#Offline"});
   var single=client.SingleConfigGroup!;var singleId=WebId(single,0);await Command(new{action="select",nodeId=singleId});await Command(new{action="edit-node",nodeId=singleId,name="<img src=x onerror=alert(1)>",text=ShareSnapshot(single.Profiles[0])});client.PushWebState();await Task.Delay(180);
   await Script("filterView('single')");await Assert("!document.querySelector('#serverListContainer img') && document.querySelector('#serverListContainer').textContent.includes('<img src=x onerror=alert(1)>')","Profile names are rendered as text, never executable markup");
   await Command(new{action="delete-node",nodeId=singleId});if(client.SingleConfigGroup!=null)throw new Exception("Single config delete failed.");report.Add("PASS: import, edit and delete use native configuration storage logic");
   await Command(new{action="edit-sub",groupId=first.Id,name="Design review",url=first.Url,autoUpdate=false,interval=24});var edited=client.subscriptions.First(g=>g.Id==first.Id);if(edited.AutoUpdate||edited.AutoUpdateHours!=24)throw new Exception("Subscription edit preferences not saved.");report.Add("PASS: edit subscription stores update toggle and interval");
   await Command(new{action="preferences",theme="dark-purple",appearance=new Dictionary<string,string>{{"accent","#8b5cf6"},{"bg","#0f0517"},{"blur","30"},{"opacity","70"},{"radius","20"},{"border","1"}},tray=false,autoUpdate=false,tun=false});
   var roundtrip=JsonSerializer.Deserialize<UiPreferences>(JsonSerializer.Serialize(client.preferences))!;if(roundtrip.WebTheme!="dark-purple"||roundtrip.WebAppearance["blur"]!="30"||roundtrip.TunMode)throw new Exception("Preferences roundtrip failed.");report.Add("PASS: language, theme, custom material and real switches persist in portable preferences");
   client.Size=new(1000,700);await Script("filterView('all')");await Task.Delay(550);await Assert("document.documentElement.scrollWidth<=innerWidth+1","Responsive layout fits minimum viewport");await Assert("document.documentElement.scrollHeight<=innerHeight+1 && getComputedStyle(document.querySelector('#serverListContainer')).overflowY==='auto'","Dashboard remains fixed while server list scrolls");await Capture("review-web-minimum");
   await core.CallDevToolsProtocolMethodAsync("Emulation.setEmulatedMedia","{\"features\":[{\"name\":\"prefers-reduced-motion\",\"value\":\"reduce\"}]}");await Assert("getComputedStyle(document.querySelector('.orb')).animationName==='none'","Reduced-motion preference disables continuous animations");
   client.Size=new(1380,980);client.CommitGroups(new(),null);client.status.Text="Disconnected";client.PushWebState();await Script("setTheme('dark-blue',null,false);setLang('en',null,false);showPage('servers')");await core.CallDevToolsProtocolMethodAsync("Emulation.setEmulatedMedia","{\"features\":[]}");await Capture("review-web-customer");
  }
 }
}
