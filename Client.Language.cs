namespace MelavoClient;
sealed partial class Client {
 bool Fa=>preferences.Language=="fa";
 string L(string en,string fa)=>Fa?fa:en;
 readonly Dictionary<Control,string> originalText=new();
 static readonly Dictionary<string,string> English=new(){
 ["سریع. خصوصی. بدون مرز."]="Fast. Private. Borderless.",["آپدیت"]="Update",["Language / زبان"]="Language",["ذخیره"]="Save",["سرور"]="Server",
 ["ویرایش ساب"]="Edit subscription",["ساب‌ها"]="Subscriptions",["کانفیگ‌های تکی"]="Single configurations",["اشتراک‌گذاری"]="Share",["افزودن ساب"]="Add subscription",["افزودن کانفیگ"]="Add config",["حذف کانفیگ"]="Remove config",["اتصال"]="Connect",["قطع اتصال"]="Disconnect",["آپدیت گروه"]="Update subscription",["بازکردن"]="Open",["خروج"]="Exit",["بازکردن فهرست"]="Open list",["فهرست سرورها"]="Server list",["اتصال VPN"]="Connect VPN",["قطع اتصال VPN"]="Disconnect VPN",["نام گروه"]="Group name",["لینک سابسکریپشن HTTPS"]="HTTPS subscription URL",["ناموجود"]="Unavailable",
 ["دریافت خودکار به‌روزرسانی برنامه"]="Download app updates automatically",["بررسی به‌روزرسانی"]="Check for updates",["سبک ظاهری"]="Visual style",["خانه"]="Home",["تنظیمات"]="Settings",["درباره"]="About",["اتصال امن، با یک انتخاب"]="Your connection, at a glance",["اشتراک من"]="My subscription",["افزودن اشتراک"]="Add subscription",["حذف"]="Remove",["به‌روزرسانی"]="Update",["محبوب"]="Favorite",["★ محبوب"]="★ Favorite",["پینگ همه"]="Ping all",["پینگ تونل"]="Server ping",["پینگ"]="Ping",["آی‌پی خروجی"]="Check IP",["ویرایش"]="Edit config",["پروتکل"]="Protocol",["کشور"]="Country",["IP خروجی"]="Public IP",["مدت اتصال"]="Duration",["دانلود"]="Download",["آپلود"]="Upload",["ترافیک نشست"]="Traffic",["تنظیمات ظاهر و رفتار برنامه"]="Appearance and app behavior",["تم تاریک"]="Dark theme",["اعلان تغییر وضعیت اتصال"]="Connection notifications",["حالت TUN"]="TUN mode",["جزئیات وضعیت و خطا"]="Diagnostics",["کپی جزئیات خطا"]="Copy diagnostics",["آپدیت Xray"]="Update Xray",["آپدیت برنامه از ZIP"]="Update app from ZIP",["Xray از GitHub رسمی؛ برنامه از فایل نسخهٔ جدید"]="Xray from its official GitHub release; app updates from release ZIP",["متصل"]="Connected",["در حال اتصال"]="Connecting",["در حال قطع اتصال"]="Disconnecting",["خطای اتصال"]="Connection error",["متصل نیست"]="Disconnected",["اتصال فعال است"]="Connection active",["برای اتصال دکمه را بزنید"]="Click to connect",["به‌روزرسانی ساب‌ها…"]="Updating subscriptions…",["یک سرور از بخش سرورها انتخاب کنید"]="Select a server from the list",["هنوز اشتراکی اضافه نشده"]="No subscription yet",["ساب را برای نمایش اطلاعات حساب اضافه کنید"]="Add a subscription to see its account details",["حجم اشتراک توسط ساب گزارش نشده"]="Traffic allowance is not provided by this subscription",["تاریخ انقضا توسط ساب گزارش نشده"]="Expiry date is not provided by this subscription",["قطع است"]="Disconnected",["نامشخص"]="Unknown",["تونل فعال؛ اینترنت هنوز تأیید نشده"]="Tunnel active; internet check not confirmed",["خطای اتصال؛ جزئیات در «درباره»"]="Connection failed; see About for diagnostics",["برای شروع، یک سرور انتخاب کنید"]="Select a server to get started",["هیچ سروری انتخاب نشده"]="No server selected",
 ["اطلاعات حجم در ساب موجود نیست"]="Traffic allowance unavailable",["تاریخ انقضا نامشخص"]="Expiry unavailable",["گروه‌های شما با هر بار بازشدن خودکار آپدیت می‌شوند"]="Subscriptions update automatically when the app opens"
 };
 void TranslateTree(Control root){
  if(root==serverSummary)return;
  if(English.TryGetValue(root.Text,out var english)){originalText[root]=root.Text;if(!Fa)root.Text=english;}
  else if(Fa&&originalText.TryGetValue(root,out var fa)&&English.TryGetValue(fa,out var en)&&root.Text==en)root.Text=fa;
  foreach(Control child in root.Controls)TranslateTree(child);
  if(root is ModernInput input&&English.TryGetValue(input.PlaceholderText,out var placeholder))input.PlaceholderText=L(placeholder,input.PlaceholderText);
  if(root.AccessibleName is string accessible&&English.TryGetValue(accessible,out var accessibleEnglish))root.AccessibleName=L(accessibleEnglish,accessible);
 }
 void ApplyLanguage(){
  RightToLeft=Fa?RightToLeft.Yes:RightToLeft.No;titleBar?.Configure(Fa);
  if(Controls.OfType<TableLayoutPanel>().FirstOrDefault() is TableLayoutPanel shell){shell.RightToLeft=RightToLeft.No;var sidebar=shell.Controls.OfType<Card>().FirstOrDefault();if(sidebar!=null&&pageHost!=null){shell.SuspendLayout();shell.SetColumn(sidebar,Fa?1:0);shell.SetColumn(pageHost,Fa?0:1);shell.ColumnStyles[0].SizeType=Fa?SizeType.Percent:SizeType.Absolute;shell.ColumnStyles[0].Width=Fa?100:212;shell.ColumnStyles[1].SizeType=Fa?SizeType.Absolute:SizeType.Percent;shell.ColumnStyles[1].Width=Fa?212:100;shell.ResumeLayout();}}
  void Direction(Control control){if(control is not ModernInput&&control!=serverSummary&&control!=details&&!(control is Label l&&metrics.Values.Any(values=>values.Contains(l))))control.RightToLeft=RightToLeft;foreach(Control child in control.Controls)Direction(child);}Direction(this);if(Controls.OfType<TableLayoutPanel>().FirstOrDefault() is TableLayoutPanel fixedShell)fixedShell.RightToLeft=RightToLeft.No;
  url.RightToLeft=details.RightToLeft=RightToLeft.No;servers.RightToLeft=RightToLeft;
  groups.PlaceholderText=L("Select a subscription","انتخاب ساب");search.PlaceholderText=L("Search servers…","جستجوی سرورها…");search.RightToLeft=RightToLeft;
  int selected=filter.SelectedIndex;filter.Items.Clear();filter.Items.AddRange(Fa?new object[]{"همهٔ سرورها","علاقه‌مندی‌ها","اخیراً استفاده‌شده"}:new object[]{"All servers","Favorites","Recently used"});filter.SelectedIndex=selected;filter.Invalidate();
  string[] headings=Fa?new[]{"سرور","کشور","پروتکل","پینگ","وضعیت","بار","★"}:new[]{"Server","Country","Protocol","Ping","Status","Load","★"};for(int i=0;i<servers.Columns.Count;i++)servers.Columns[i].Text=headings[i];servers.EmptyText=L("No servers found","سروری پیدا نشد");
  creatorCredit.Text=L("Made with love ♥","ساخته‌شده با عشق ♥");creatorCredit.RightToLeft=RightToLeft;creatorLink.RightToLeft=RightToLeft.No;
  TranslateTree(this);ApplyTheme(this);creatorLink.LinkColor=Design.Muted;creatorLink.ActiveLinkColor=Design.Accent;creatorLink.VisitedLinkColor=Design.Muted;FillServers();SyncConfigScope();SetControls();
  editSubscription.Text=L("Edit subscription","ویرایش ساب");subscriptionsTab.Text=L("Subscriptions","ساب‌ها");singleConfigsTab.Text=L("Single configurations","کانفیگ‌های تکی");shareConfig.Text=L("Share","اشتراک‌گذاری");
  if(tray.ContextMenuStrip!=null){var captions=Fa?new[]{"بازکردن","قطع اتصال","خروج"}:new[]{"Open","Disconnect","Exit"};for(int i=0;i<Math.Min(captions.Length,tray.ContextMenuStrip.Items.Count);i++)tray.ContextMenuStrip.Items[i].Text=captions[i];}
 }
}




