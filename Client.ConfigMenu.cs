namespace MelavoClient;
sealed partial class Client {
 readonly ConfigurationMenu configMenu=new();
 readonly System.Windows.Forms.Timer autoUpdateTimer=new(){Interval=60000};
 void InitializeAutoUpdate(){
  autoUpdateTimer.Tick+=async(_,_)=>{if(preview||busy||updating||pingTesting||xray!=null)return;var targets=subscriptions.Where(g=>IsSubscription(g)&&g.AutoUpdate&&(!g.Updated.HasValue||DateTime.UtcNow-g.Updated.Value>=TimeSpan.FromHours(1))).ToArray();if(targets.Length>0)await UpdateGroups(targets);};
  if(!preview)autoUpdateTimer.Start();FormClosed+=(_,_)=>autoUpdateTimer.Dispose();
 }
 void InitializeConfigMenu(){
  configMenu.AddAction("\uE768",async()=>await Connect());
  configMenu.AddAction("\uE72D",()=>ShowConfigShare());
  configMenu.AddAction("\uE70F",async()=>await ShowConfigEditor());
  configMenu.AddAction("\uE74D",()=>DeleteSelectedConfig(),true);
  configMenu.AddAction("\uE9D9",async()=>await CheckLatency());
  servers.MouseUp+=(_,e)=>{if(e.Button!=MouseButtons.Right||!servers.SelectAt(e.Location))return;
   var idle=!busy&&!updating&&!pingTesting&&xray==null;
   var title=ServerList.StripFlags(Profiles[(int)servers.SelectedItems[0].Tag!]["remarks"]?.ToString()??L("Configuration","کانفیگ"));
   configMenu.Prepare(title,Fa,new[]{L("Connect","اتصال"),L("Share","اشتراک‌گذاری"),L("Edit","ویرایش"),L("Delete","حذف"),L("Ping","پینگ")},new[]{idle,!busy&&!updating,idle,idle,!busy&&!updating&&!pingTesting});
   configMenu.Show(servers,e.Location);
  };
  FormClosed+=(_,_)=>configMenu.Dispose();
 }
 void DeleteSelectedConfig(){
  if(busy||updating||pingTesting||xray!=null||groups.SelectedItem is not SubscriptionGroup group)return;
  try{DeleteSingleConfig(group);}catch(Exception e){MessageBox.Show(this,SafeError(e.Message),L("Could not save changes","ذخیره تغییرات انجام نشد"));}
 }
}
