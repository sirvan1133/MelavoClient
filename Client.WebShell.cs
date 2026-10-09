using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace MelavoClient;

sealed partial class Client {
 readonly bool useWebShell;
 WebView2? webShell;
 readonly System.Windows.Forms.Timer webStateTimer=new(){Interval=500};
 readonly Dictionary<string,long> webLatency=new();
 readonly HashSet<string> webNoReply=new();
 string? webSelectedId,webActiveId;string lastWebState="",webLog="";
 bool webReady,webOperation;
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ReleaseCapture();
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr w,IntPtr l);
 string? webReviewDirectory;
 static readonly string[] webThemes={"dark-blue","dark-purple","dark-emerald","dark-rose","dark-carbon","dark-neon","dark-matrix","light"};
 static string WebResource(string name){using var stream=typeof(Client).Assembly.GetManifestResourceStream("MelavoClient.Assets."+name)??throw new InvalidOperationException("UI resource missing.");using var reader=new StreamReader(stream);return reader.ReadToEnd();}
 void InitializeWebShell(){
  foreach(Control child in Controls)child.Visible=false;Padding=Padding.Empty;
  webShell=new WebView2{Dock=DockStyle.Fill,DefaultBackgroundColor=Color.FromArgb(10,14,26)};Controls.Add(webShell);webShell.BringToFront();
  Shown+=async(_,_)=>{try{await LoadWebShell();}catch(Exception e){WebStartupError(e);}};
  webStateTimer.Tick+=(_,_)=>PushWebState();
  FormClosing+=(_,e)=>{if(webOperation){e.Cancel=true;status.Text=L("An operation is running. Please wait.","عملیات در حال اجراست؛ چند لحظه صبر کنید.");}};
  FormClosed+=(_,_)=>{webStateTimer.Dispose();webShell?.Dispose();};
 }
 void WebStartupError(Exception error){
  if(IsDisposed)return;webShell?.Hide();
  var panel=new Panel{Dock=DockStyle.Fill,Padding=new(40),BackColor=Design.Canvas};
  var message=new Label{AutoSize=false,Dock=DockStyle.Fill,ForeColor=Design.Text,Font=Design.Font(12),Text=L("The Melavo interface needs Microsoft Edge WebView2 Runtime. Install the runtime, then reopen Melavo.\n\n","رابط ملـاوو به Microsoft Edge WebView2 Runtime نیاز دارد. آن را نصب کنید و برنامه را دوباره باز کنید.\n\n")+SafeError(error.Message)};
  var install=new Button{Dock=DockStyle.Bottom,Height=44,Text=L("Install WebView2 Runtime","نصب WebView2 Runtime")};install.Click+=(_,_)=>Process.Start(new ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/"){UseShellExecute=true});
  var close=new Button{Dock=DockStyle.Bottom,Height=44,Text=L("Close","بستن")};close.Click+=(_,_)=>Close();panel.Controls.Add(message);panel.Controls.Add(install);panel.Controls.Add(close);Controls.Add(panel);panel.BringToFront();
 }
 async Task LoadWebShell(){
  var folder=webReviewDirectory??(preview?Path.Combine(Path.GetTempPath(),"Melavo-WebReview-"+Guid.NewGuid().ToString("N")):Path.Combine(SubscriptionStore.DirectoryPath,"WebView2"));
  Directory.CreateDirectory(folder);var environment=await CoreWebView2Environment.CreateAsync(userDataFolder:folder);await webShell!.EnsureCoreWebView2Async(environment);
  var core=webShell.CoreWebView2;core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreDevToolsEnabled=preview;core.Settings.AreHostObjectsAllowed=false;core.Settings.IsStatusBarEnabled=false;core.Settings.IsZoomControlEnabled=false;core.Settings.AreBrowserAcceleratorKeysEnabled=false;core.Settings.IsPasswordAutosaveEnabled=false;core.Settings.IsGeneralAutofillEnabled=false;
  core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
  bool initialDocument=true;
  core.NewWindowRequested+=(_,e)=>e.Handled=true;core.NavigationStarting+=(_,e)=>{if(initialDocument){initialDocument=false;}else e.Cancel=true;};
  core.WebMessageReceived+=async(_,e)=>{
   if(e.Source!="about:blank"||e.WebMessageAsJson.Length>5_100_000)return;
   int id=0;try{using var document=JsonDocument.Parse(e.WebMessageAsJson);var message=document.RootElement;if(!message.TryGetProperty("id",out var value)||!value.TryGetInt32(out id))return;var result=await HandleWebCommand(message);PushWebState();WebReply(id,result,null);}
   catch(Exception error){WebReply(id,null,SafeError(error.Message));}finally{PushWebState();}
  };
  core.ProcessFailed+=(_,e)=>{webReady=false;webStateTimer.Stop();if(!IsDisposed)WebStartupError(new Exception(L("The interface process stopped. Reopen Melavo.","پردازش رابط متوقف شد؛ برنامه را دوباره باز کنید.")));};
  await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__melavoErrors=[];window.addEventListener('error',e=>window.__melavoErrors.push(e.message));window.addEventListener('unhandledrejection',e=>window.__melavoErrors.push(String(e.reason)));");
  core.NavigateToString(WebResource("WebShell.html").Replace("/*MELAVO_BRIDGE*/",WebResource("WebShell.js")));
  tun.Checked=preferences.TunMode;var area=Screen.FromControl(this).WorkingArea;MinimumSize=new(Math.Min(1000,area.Width-30),Math.Min(700,area.Height-30));
 }
 void WebReply(int id,object? result,string? error){if(IsDisposed||webShell?.CoreWebView2==null)return;webShell.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type="reply",id,result,error}));}
 static string WebId(SubscriptionGroup group,int index)=>group.Id+":"+SourceKey(group.SourceProfiles.Count>index?group.SourceProfiles[index]:group.Profiles[index])+":"+index;
 (SubscriptionGroup Group,int Index,JsonObject Profile) WebNode(string id){foreach(var group in subscriptions)for(int i=0;i<group.Profiles.Count;i++)if(WebId(group,i)==id)return(group,i,group.Profiles[i]);throw new InvalidOperationException(L("This configuration no longer exists.","این کانفیگ دیگر وجود ندارد."));}
 void SelectWebNode(string id){var node=WebNode(id);if(busy||updating)throw new InvalidOperationException(L("An operation is running. Please wait.","عملیات در حال اجراست؛ چند لحظه صبر کنید."));webSelectedId=id;singleConfigScope=!IsSubscription(node.Group);PopulateGroups(node.Group.Id);var row=servers.Items.FirstOrDefault(item=>item.Tag is int index&&index==node.Index);if(row!=null)row.Selected=true;}
 void RequireWebIdle(){if(busy||updating||pingTesting||xray!=null||downloadingApp)throw new InvalidOperationException(L("Disconnect and wait for the current operation before making changes.","اتصال را قطع کنید و منتظر پایان عملیات جاری بمانید."));}
 async Task<object?> HandleWebCommand(JsonElement message){
  string S(string key)=>message.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
  bool B(string key,bool fallback=false)=>message.TryGetProperty(key,out var value)&&value.ValueKind is JsonValueKind.True or JsonValueKind.False?value.GetBoolean():fallback;
  int N(string key,int fallback)=>message.TryGetProperty(key,out var value)&&value.TryGetInt32(out var number)?number:fallback;
  var action=S("action");
  switch(action){
   case "ready":webReady=true;lastWebState="";webStateTimer.Start();PushWebState();if(!preview)_=LoadDirectIp();return true;
   case "drag":ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);return true;
   case "maximize":ToggleMaximizeWindow();return true;
   case "minimize":WindowState=FormWindowState.Minimized;return true;
   case "close":Close();return true;
   case "github":if(!preview)Process.Start(new ProcessStartInfo("https://github.com/sirvan1133"){UseShellExecute=true});return true;
   case "select":if(!webOperation)SelectWebNode(S("nodeId"));return true;
   case "preferences":
    if(message.TryGetProperty("language",out _)){preferences.Language=S("language")=="fa"?"fa":"en";ApplyLanguage();}
    if(message.TryGetProperty("theme",out _)){if(!webThemes.Contains(S("theme")))throw new ArgumentException("Invalid theme.");preferences.WebTheme=S("theme");}
    if(message.TryGetProperty("appearance",out var appearance)&&appearance.ValueKind==JsonValueKind.Object){var next=new Dictionary<string,string>();foreach(var key in new[]{"blur","opacity","radius","border","accent","bg"})if(appearance.TryGetProperty(key,out var field)&&field.ValueKind==JsonValueKind.String){var text=field.GetString()??"";if(key is "accent" or "bg"){if(!System.Text.RegularExpressions.Regex.IsMatch(text,"^#[0-9a-fA-F]{6}$"))throw new ArgumentException("Invalid color.");}else{int maximum=key=="blur"?40:key=="opacity"?100:key=="radius"?30:3;if(!int.TryParse(text,out var number)||number<0||number>maximum)throw new ArgumentException("Invalid appearance setting.");}next[key]=text;}preferences.WebAppearance=next;}
    if(message.TryGetProperty("tun",out _)){RequireWebIdle();preferences.TunMode=B("tun");tun.Checked=preferences.TunMode;}
    if(message.TryGetProperty("tray",out _))preferences.Tray=B("tray");
    if(message.TryGetProperty("notifications",out _))preferences.Notifications=B("notifications");
    if(message.TryGetProperty("autoUpdate",out _))preferences.AutoAppUpdate=B("autoUpdate");
    SavePreferences();return true;
  }
  if(webOperation)throw new InvalidOperationException(L("An operation is running. Please wait.","عملیات در حال اجراست؛ چند لحظه صبر کنید."));
  webOperation=true;PushWebState();try{
   switch(action){
    case "toggle-connect":case "connect":
     if(preview)throw new InvalidOperationException(L("Connections are disabled in offline preview.","اتصال در پیش‌نمایش آفلاین غیرفعال است."));
     if(xray!=null){if(action=="toggle-connect"){Disconnect();webActiveId=null;SyncUi();return true;}if(webActiveId==S("nodeId"))return true;Disconnect();SyncUi();}
     SelectWebNode(S("nodeId"));webActiveId=webSelectedId;await Connect();if(xray==null){webActiveId=null;throw new InvalidOperationException(status.Text+"\n"+details.Text);}return true;
    case "add-sub":{
     RequireWebIdle();string feedback="";singleConfigScope=false;if(S("name").Trim().Length==0)throw new ArgumentException(L("Enter a subscription name.","نام ساب را وارد کنید."));
     if(preview)throw new InvalidOperationException(L("Subscription downloads are disabled in offline preview.","دریافت ساب در پیش‌نمایش آفلاین غیرفعال است."));
     if(!await AddSubscription(S("name"),S("url"),text=>feedback=text))throw new InvalidOperationException(feedback.Length>0?feedback:status.Text);
     var group=subscriptions.First(g=>g.Url==S("url").Trim());var candidate=CloneGroup(group);candidate.AutoUpdate=B("autoUpdate",true);candidate.AutoUpdateHours=Math.Clamp(N("interval",12),1,168);CommitGroups(subscriptions.Select(g=>g.Id==group.Id?candidate:g).ToList(),group.Id);return true;
    }
    case "edit-sub":{
     RequireWebIdle();var group=subscriptions.FirstOrDefault(g=>g.Id==S("groupId")&&IsSubscription(g))??throw new ArgumentException("Subscription not found.");string feedback="";
     if(preview&&group.Url!=S("url"))throw new InvalidOperationException("Downloads disabled in preview.");
     if(!await SaveSubscriptionEdit(group,S("name"),S("url"),text=>feedback=text,autoUpdate:B("autoUpdate",true)))throw new InvalidOperationException(feedback);
     var edited=CloneGroup(subscriptions.First(g=>g.Id==group.Id));edited.AutoUpdateHours=Math.Clamp(N("interval",12),1,168);CommitGroups(subscriptions.Select(g=>g.Id==group.Id?edited:g).ToList(),group.Id);return true;
    }
    case "delete-sub":RequireWebIdle();if(!subscriptions.Any(g=>g.Id==S("groupId")&&IsSubscription(g)))throw new ArgumentException("Subscription not found.");CommitGroups(subscriptions.Where(g=>g.Id!=S("groupId")).ToList(),null);return true;
    case "add-config":{
     RequireWebIdle();var parsed=Parse(S("text"));if(parsed.Count!=1)throw new ArgumentException(L("Enter exactly one configuration.","فقط یک کانفیگ وارد کنید."));if(!preview)await ValidateEdit(parsed[0]);AddSingleConfig(parsed[0],false);return true;
    }
    case "edit-data":{RequireWebIdle();var node=WebNode(S("nodeId"));return new{name=node.Profile["remarks"]?.ToString()??"",text=ShareSnapshot(node.Profile)};}
    case "edit-node":{RequireWebIdle();var node=WebNode(S("nodeId"));var parsed=Parse(S("text"));if(parsed.Count!=1)throw new ArgumentException("Enter exactly one configuration.");parsed[0]["remarks"]=S("name").Trim();if(!preview)await ValidateEdit(parsed[0]);SaveConfigEdit(node.Group,node.Index,parsed[0]);return true;}
    case "delete-node":{RequireWebIdle();SelectWebNode(S("nodeId"));DeleteSingleConfig((SubscriptionGroup)groups.SelectedItem!,true);if(webSelectedId==S("nodeId"))webSelectedId=null;return true;}
    case "copy":{var node=WebNode(S("nodeId"));Clipboard.SetText(ShareText(node.Profile));return true;}
    case "share":{var node=WebNode(S("nodeId"));var text=ShareText(node.Profile);string? qr=null,error=null;try{qr=Convert.ToBase64String(CreateQrPng(text));}catch(Exception e){error=SafeError(e.Message);}return new{text,qr,qrError=error};}
    case "save-qr":{var node=WebNode(S("nodeId"));var png=CreateQrPng(ShareText(node.Profile));using var picker=new SaveFileDialog{Filter="PNG image (*.png)|*.png",FileName="Melavo-configuration.png"};if(picker.ShowDialog(this)==DialogResult.OK)File.WriteAllBytes(picker.FileName,png);return true;}
    case "refresh":RequireWebIdle();if(preview)throw new InvalidOperationException(L("Downloads are disabled in offline preview.","دریافت در پیش‌نمایش آفلاین غیرفعال است."));var targets=subscriptions.Where(g=>IsSubscription(g)&&(S("groupId")==""||g.Id==S("groupId"))).ToArray();if(targets.Length>0)await UpdateGroups(targets);return true;
    case "ping":case "ping-all":if(busy||updating||pingTesting)throw new InvalidOperationException(L("An operation is running. Please wait.","عملیات در حال اجراست؛ چند لحظه صبر کنید."));if(preview)throw new InvalidOperationException(L("Network tests are disabled in offline preview.","تست شبکه در پیش‌نمایش آفلاین غیرفعال است."));await PingWebNodes(action=="ping"?S("nodeId"):null);return true;
    case "ip":await RefreshIp();return true;
    case "app-update":RequireWebIdle();await CheckAppUpdates(true);return true;
    case "local-update":RequireWebIdle();await UpdateProgram();return true;
    case "core-update":RequireWebIdle();await UpdateXray(true);return true;
    case "clear-logs":webLog="";details.Clear();logs.Clear();return true;
    case "export-logs":using(var picker=new SaveFileDialog{Filter="Text (*.txt)|*.txt",FileName="Melavo-diagnostics.txt"})if(picker.ShowDialog(this)==DialogResult.OK)File.WriteAllText(picker.FileName,SafeError(WebLogs()));return true;
    default:throw new ArgumentException("Unknown action.");
   }
  }finally{webOperation=false;}
 }
 async Task PingWebNodes(string? selected){
  var nodes=subscriptions.SelectMany(g=>g.Profiles.Select((p,i)=>(Id:WebId(g,i),Profile:p))).Where(n=>selected==null||n.Id==selected).ToArray();if(nodes.Length==0)return;
  pingTesting=true;int completed=0,responding=0;using var gate=new SemaphoreSlim(4);try{await Task.WhenAll(nodes.Select(async node=>{await gate.WaitAsync(pingCancellation.Token);try{var result=await MeasureIcmpProfile(node.Profile,pingCancellation.Token);webLatency[node.Id]=result;webNoReply.Remove(node.Id);responding++;}catch(OperationCanceledException){throw;}catch{webLatency.Remove(node.Id);webNoReply.Add(node.Id);}finally{gate.Release();}completed++;status.Text=L($"ICMP ping: {completed}/{nodes.Length} tested · {responding} responding",$"پینگ ICMP: {completed}/{nodes.Length} بررسی شد · {responding} پاسخ داد");PushWebState();}));}catch(OperationCanceledException){}finally{pingTesting=false;SetControls();}
 }
 string WebLogs(){var combined=string.Join('\n',new[]{webLog,details.Text}.Concat(logs.Values.SelectMany(queue=>queue.ToArray())).Where(s=>!string.IsNullOrWhiteSpace(s)));return SafeError(combined.Length>60000?combined[^60000..]:combined);}
 object WebState(){
  string MetricValue(string key)=>metrics.TryGetValue(key,out var labels)&&labels.Count>0?labels[0].Text:"—";
  var active=webActiveId??webSelectedId;JsonObject? profile=null;try{if(active!=null)profile=WebNode(active).Profile;}catch{}
  var groupData=subscriptions.Select(g=>new{id=g.Id,name=IsSubscription(g)?g.Name:L("Single configurations","کانفیگ‌های تکی"),url=g.Url,subscription=IsSubscription(g),autoUpdate=g.AutoUpdate,interval=g.AutoUpdateHours,used=g.UsedBytes.HasValue?Bytes(g.UsedBytes.Value):"—",total=g.TotalBytes.HasValue?Bytes(g.TotalBytes.Value):null,expires=g.ExpiresUnix is >0?DateTimeOffset.FromUnixTimeSeconds(g.ExpiresUnix.Value).ToLocalTime().ToString("yyyy/MM/dd"):null,nodes=g.Profiles.Select((p,i)=>{var id=WebId(g,i);var stream=Outbound(p)?["streamSettings"]??p["streamSettings"];var country=Country(p);return new{id,name=ServerList.StripFlags(p["remarks"]?.ToString()??$"Server {i+1}"),flag=country=="Unknown"?"🌍":country.Split(' ')[0],type=Outbound(p)?["protocol"]?.ToString()?.ToUpperInvariant()??"—",proto=string.Join(" · ",new[]{stream?["network"]?.ToString(),stream?["security"]?.ToString()}.Where(s=>!string.IsNullOrWhiteSpace(s))),ping=webLatency.TryGetValue(id,out var ms)?(long?)ms:null,failed=webNoReply.Contains(id),single=!IsSubscription(g)};}).ToArray()}).ToArray();
  return new{version=UpdateService.AppVersion,groups=groupData,language=preferences.Language,theme=preferences.WebTheme,appearance=preferences.WebAppearance,connected=xray!=null&&!busy,connecting=busy,disconnecting,error=visualState==ConnectionState.Error,busy=busy||updating||pingTesting||webOperation||downloadingApp,selectedId=webSelectedId,activeId=webActiveId,serverName=profile?["remarks"]?.ToString(),download=MetricValue("download"),upload=MetricValue("upload"),duration=MetricValue("duration"),usage=MetricValue("usage"),ping=active!=null&&webLatency.TryGetValue(active,out var ping)?ping+" ms":"—",ip=xray!=null?MetricValue("ip"):"—",directIp=webDirectIp,country=profile!=null?Country(profile).Split(' ').Last():"—",isp="—",socks=xray!=null?"127.0.0.1:"+localProxyPort:"—",tun=tun.Checked,tray=preferences.Tray,notifications=preferences.Notifications,autoUpdate=preferences.AutoAppUpdate,engines=preview?"Xray + sing-box · "+L("Offline preview","پیش‌نمایش آفلاین"):installedInfo.Text,updateStatus=updateStatus.Text,updateReady=readyAppUpdate!=null,status=status.Text,logs=WebLogs()};
 }
 string webDirectIp="—";
 async Task LoadDirectIp(){if(preview)return;try{using var handler=new HttpClientHandler{UseProxy=false};using var http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(8)};var content=await http.GetStringAsync("https://www.cloudflare.com/cdn-cgi/trace");var address=content.Split('\n').FirstOrDefault(s=>s.StartsWith("ip="))?[3..].Trim();if(xray==null&&System.Net.IPAddress.TryParse(address,out _))webDirectIp=address!;}catch{} }
 void PushWebState(){if(!webReady||IsDisposed||webShell?.CoreWebView2==null)return;var serialized=JsonSerializer.Serialize(new{type="state",state=WebState()});if(serialized==lastWebState)return;lastWebState=serialized;webShell.CoreWebView2.PostWebMessageAsJson(serialized);}
}
