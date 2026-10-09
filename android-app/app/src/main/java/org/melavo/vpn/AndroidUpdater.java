package org.melavo.vpn;
import android.content.*;
import android.content.pm.*;
import android.os.Build;
import org.json.*;
import java.io.*;
import java.net.*;
import java.security.MessageDigest;
import java.util.Arrays;

final class AndroidUpdater {
 static boolean check(Context context,boolean download)throws Exception{
  JSONObject release=new JSONObject(MainActivity.https("https://api.github.com/repos/sirvan1133/MelavoClient/releases/latest").text);
  String current=context.getPackageManager().getPackageInfo(context.getPackageName(),0).versionName;
  if(release.optString("tag_name").equals("v"+current))return false;
  JSONArray assets=release.getJSONArray("assets");JSONObject asset=null;for(int i=0;i<assets.length();i++)if(assets.getJSONObject(i).getString("name").endsWith("-Android-universal.apk"))asset=assets.getJSONObject(i);
  if(asset==null)return false;if(!download)return true;String digest=asset.optString("digest");if(!digest.startsWith("sha256:"))throw new Exception("Update checksum unavailable");
  URL url=new URL(asset.getString("browser_download_url"));if(!url.getProtocol().equals("https")||!url.getHost().equals("github.com"))throw new Exception("Invalid update origin");
  File folder=new File(context.getFilesDir(),"updates");if(!folder.exists()&&!folder.mkdirs())throw new IOException("Could not create update folder");File pending=new File(folder,"pending.apk"),ready=new File(folder,"ready.apk");
  HttpURLConnection connection=(HttpURLConnection)url.openConnection(java.net.Proxy.NO_PROXY);connection.setConnectTimeout(15000);connection.setReadTimeout(30000);
  try{if(connection.getResponseCode()!=200)throw new IOException("Update download failed");MessageDigest sha=MessageDigest.getInstance("SHA-256");long total=0;try(InputStream input=connection.getInputStream();OutputStream output=new FileOutputStream(pending)){byte[] buffer=new byte[32768];int n;while((n=input.read(buffer))!=-1){total+=n;if(total>160_000_000)throw new IOException("Update too large");sha.update(buffer,0,n);output.write(buffer,0,n);}}
   StringBuilder hex=new StringBuilder();for(byte b:sha.digest())hex.append(String.format(java.util.Locale.ROOT,"%02x",b));if(!digest.substring(7).equals(hex.toString()))throw new SecurityException("Update checksum mismatch");
   PackageManager pm=context.getPackageManager();int flags=Build.VERSION.SDK_INT>=28?PackageManager.GET_SIGNING_CERTIFICATES:PackageManager.GET_SIGNATURES;PackageInfo installed=pm.getPackageInfo(context.getPackageName(),flags),candidate=pm.getPackageArchiveInfo(pending.getAbsolutePath(),flags);
   if(candidate==null||!context.getPackageName().equals(candidate.packageName)||candidate.versionCode<=installed.versionCode)throw new SecurityException("Invalid update package/version");
   android.content.pm.Signature[] oldSignatures=Build.VERSION.SDK_INT>=28?installed.signingInfo.getApkContentsSigners():installed.signatures,newSignatures=Build.VERSION.SDK_INT>=28?candidate.signingInfo.getApkContentsSigners():candidate.signatures;
   if(oldSignatures.length!=newSignatures.length)throw new SecurityException("Update signer mismatch");for(int i=0;i<oldSignatures.length;i++)if(!Arrays.equals(oldSignatures[i].toByteArray(),newSignatures[i].toByteArray()))throw new SecurityException("Update signer mismatch");
   if(ready.exists()&&!ready.delete())throw new IOException("Could not replace cached update");if(!pending.renameTo(ready))throw new IOException("Could not stage update");return true;
  }finally{connection.disconnect();if(pending.exists())pending.delete();}
 }
 static void install(android.app.Activity activity){File file=new File(activity.getFilesDir(),"updates/ready.apk");if(!file.exists()){boolean fa=false;try{fa=new SecureStore(activity).preferences().optString("language","en").equals("fa");}catch(Exception ignored){}android.widget.Toast.makeText(activity,fa?"ابتدا آپدیت را بررسی کنید":"Check for updates first",android.widget.Toast.LENGTH_LONG).show();return;}if(Build.VERSION.SDK_INT>=26&&!activity.getPackageManager().canRequestPackageInstalls()){activity.startActivity(new Intent(android.provider.Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,android.net.Uri.parse("package:"+activity.getPackageName())));return;}activity.startActivity(new Intent(Intent.ACTION_VIEW).setDataAndType(android.net.Uri.parse("content://org.melavo.vpn.updates/apk"),"application/vnd.android.package-archive").addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION));}
}
