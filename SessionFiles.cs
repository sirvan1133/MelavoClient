namespace MelavoClient;
static class SessionFiles {
 static string Root=>Path.GetFullPath(Path.Combine(SubscriptionStore.DirectoryPath,"sessions"));
 public static string Create(){var root=Root;Directory.CreateDirectory(root);if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new IOException("The session directory must not be a link.");var folder=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);return folder;}
 public static void RemoveDirectory(string directory){try{var full=Path.GetFullPath(directory);if(!full.StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Guid.TryParseExact(Path.GetFileName(full),"N",out _)||!Directory.Exists(full)||(File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)return;Directory.Delete(full,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
 public static void RemoveFile(string path){try{File.Delete(path);}catch(IOException){}catch(UnauthorizedAccessException){}}
 public static void CleanupStale(){
  var root=Path.GetFullPath(Path.Combine(SubscriptionStore.DirectoryPath,"sessions"));
  try{if(!Directory.Exists(root)||(File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)return;foreach(var directory in Directory.GetDirectories(root)){
   var full=Path.GetFullPath(directory);
   if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Guid.TryParseExact(Path.GetFileName(full),"N",out _)||(File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)continue;
   try{Directory.Delete(full,true);}catch(IOException){}catch(UnauthorizedAccessException){}
  }}catch(IOException){}catch(UnauthorizedAccessException){}
 }
}
