namespace MelavoClient;
static partial class UpdateService {
 public static void CleanupStage(string directory){
  var root=Path.GetFullPath(TempRoot);var target=Path.GetFullPath(directory);
  if(!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Guid.TryParseExact(Path.GetFileName(target),"N",out _))return;
  try{if(!Directory.Exists(target)||(File.GetAttributes(target)&FileAttributes.ReparsePoint)!=0)return;var guard=Path.Combine(target,"worker.lock");if(File.Exists(guard)){using var locked=new FileStream(guard,FileMode.Open,FileAccess.ReadWrite,FileShare.None);}Directory.Delete(target,true);}catch(IOException){}catch(UnauthorizedAccessException){}
 }
 public static List<bool> ReadResults(){
  var results=new List<bool>();try{if(!Directory.Exists(TempRoot))return results;
   foreach(var directory in Directory.GetDirectories(TempRoot)){
    var result=Path.Combine(directory,"result.txt");
    if(File.Exists(result)){try{using var file=new FileStream(result,FileMode.Open,FileAccess.Read,FileShare.Read);if(file.Length<4096){using var reader=new StreamReader(file);results.Add(reader.ReadToEnd().Trim()=="Update installed");}}catch(IOException){}CleanupStage(directory);}
    else if(!Directory.Exists(Path.Combine(directory,"backup-app"))&&Directory.GetLastWriteTimeUtc(directory)<DateTime.UtcNow.AddDays(-1))CleanupStage(directory);
   }
  }catch(IOException){}catch(UnauthorizedAccessException){}return results;
 }
}
sealed partial class Client {
 void ReadUpdateResult(){var results=UpdateService.ReadResults();if(results.Count==0)return;bool success=results.All(x=>x);updateStatus.Text=success?L("Update installed successfully.","به‌روزرسانی با موفقیت نصب شد."):L("The previous update failed; the previous version was restored.","به‌روزرسانی قبلی ناموفق بود؛ نسخهٔ قبلی بازگردانده شد.");if(!success){details.Text=updateStatus.Text;MessageBox.Show(this,updateStatus.Text,L("Update result","نتیجهٔ به‌روزرسانی"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
}
