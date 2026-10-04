namespace MelavoClient;
sealed partial class Client {
 readonly System.Windows.Forms.Timer appUpdateTimer=new(){Interval=6*60*60*1000};
 readonly CancellationTokenSource appUpdateCancellation=new();
 bool downloadingApp;PackagePlan? readyAppUpdate;
 readonly ModernButton checkAppUpdate=new(){Text="Check for updates",Width=190};
 void InitializeAppUpdates(){
  Shown+=async(_,_)=>{if(!preview&&preferences.AutoAppUpdate)await CheckAppUpdates();};
  appUpdateTimer.Tick+=async(_,_)=>{if(preferences.AutoAppUpdate)await CheckAppUpdates();};if(!preview)appUpdateTimer.Start();
  FormClosed+=(_,_)=>{appUpdateCancellation.Cancel();appUpdateTimer.Dispose();if(preferences.AutoAppUpdate&&readyAppUpdate!=null){try{UpdateService.StartPackageInstall(readyAppUpdate,AppContext.BaseDirectory,false);}catch{UpdateService.CleanupStage(readyAppUpdate.Stage);}}else if(readyAppUpdate!=null)UpdateService.CleanupStage(readyAppUpdate.Stage);};
  checkAppUpdate.Click+=async(_,_)=>await CheckAppUpdates(true);
 }
 async Task CheckAppUpdates(bool manual=false){
  if(preview||downloadingApp||updating||readyAppUpdate!=null)return;
  downloadingApp=true;checkAppUpdate.Enabled=false;
  try{
   updateStatus.Text=L("Checking GitHub for updates…","بررسی نسخهٔ جدید در گیت‌هاب…");
   var release=await UpdateService.LatestApp(appUpdateCancellation.Token);if(release==null){updateStatus.Text=L("Melavo is up to date.","ملـاوو به‌روز است.");return;}
   var progress=new Progress<int>(percent=>{if(!IsDisposed)updateStatus.Text=L($"Downloading Melavo {release.Version} · {percent}%",$"دریافت ملـاوو {release.Version} · {percent}٪");});
   var plan=await UpdateService.DownloadApp(release,progress,appUpdateCancellation.Token);if(IsDisposed||(!manual&&!preferences.AutoAppUpdate)){UpdateService.CleanupStage(plan.Stage);return;}
   readyAppUpdate=plan;updateStatus.Text=L($"Version {plan.Version} is ready. It will install after the app closes.",$"نسخهٔ {plan.Version} آماده است؛ پس از بسته‌شدن برنامه نصب می‌شود.");
   if(!preferences.AutoAppUpdate&&manual)updateStatus.Text=L($"Version {plan.Version} downloaded. Enable automatic updates to install on exit.",$"نسخهٔ {plan.Version} دریافت شد؛ برای نصب هنگام خروج، به‌روزرسانی خودکار را روشن کنید.");
  }catch(OperationCanceledException){}catch(Exception e){if(!IsDisposed)updateStatus.Text=L("Could not download the update: ","دریافت به‌روزرسانی انجام نشد: ")+SafeError(e.Message);}
  finally{downloadingApp=false;if(!IsDisposed)checkAppUpdate.Enabled=true;}
 }
}
