package org.melavo.vpn;
import android.app.*;
import android.content.*;
import android.os.*;
import org.json.*;
import java.net.*;
import java.io.*;
import java.util.*;
import java.util.concurrent.*;
import libv2ray.*;

public final class SmokeRunner extends Instrumentation {
 Bundle options;StringBuilder report=new StringBuilder();
 @Override public void onCreate(Bundle args){options=args;super.onCreate(args);start();}
 void check(boolean result,String name)throws Exception{if(!result)throw new Exception(name);report.append("PASS: ").append(name).append('\n');}
 @Override public void onStart(){int result=Activity.RESULT_OK;try{if(options!=null&&"true".equals(options.getString("vpn")))runVpnChecks();else runChecks();}catch(Throwable e){result=Activity.RESULT_CANCELED;report.append("FAIL: ").append(e).append('\n');for(StackTraceElement frame:e.getStackTrace())report.append(frame).append('\n');}Bundle b=new Bundle();b.putString("stream",report.toString());finish(result,b);}
 void runChecks()throws Exception{
  Context context=getTargetContext();go.Seq.setContext(context);check(Libv2ray.checkVersionX().contains("Xray"),"Native Xray loads on API "+Build.VERSION.SDK_INT+" / "+Build.SUPPORTED_ABIS[0]);
  check(ConfigCodec.decode("a+b").equals("a+b"),"URI literal plus preserved");
  String link="vless://11111111-1111-1111-1111-111111111111@127.0.0.1:19043?type=ws&security=tls&sni=example.invalid&alpn=h2,http/1.1&path=%2Fa%2Bb#Offline";
  JSONObject parsed=ConfigCodec.parse(link).getJSONObject(0);check(ConfigCodec.outbound(parsed).getJSONObject("streamSettings").getJSONObject("wsSettings").getString("path").equals("/a+b"),"VLESS transport and URI decoding");check(ConfigCodec.share(parsed).equals(link),"Complete original share link retained");
  JSONObject vmess=new JSONObject().put("v","2").put("id","11111111-1111-1111-1111-111111111111").put("add","127.0.0.1").put("port","443").put("net","grpc").put("path","service").put("tls","tls").put("sni","example.invalid").put("alpn","h2").put("grpcSettings",new JSONObject().put("serviceName","service").put("authority","authority").put("multiMode",true));
  JSONObject vm=ConfigCodec.parse("vmess://"+android.util.Base64.encodeToString(vmess.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8),android.util.Base64.NO_WRAP)).getJSONObject(0);check(ConfigCodec.outbound(vm).getJSONObject("streamSettings").getJSONObject("grpcSettings").getBoolean("multiMode"),"VMess gRPC fields preserved");
  String encoded=android.util.Base64.encodeToString((link+"\n"+"trojan://test-password@127.0.0.1:443?security=tls#Offline").getBytes(java.nio.charset.StandardCharsets.UTF_8),android.util.Base64.NO_WRAP);check(ConfigCodec.parse(encoded).length()==2,"Base64 subscription parses VLESS and Trojan");
  SecureStore store=new SecureStore(context);JSONObject original=new JSONObject(store.data.toString());try{store.data.put("test","test-secret-marker");store.save();check(new SecureStore(context).data.getString("test").equals("test-secret-marker"),"Encrypted storage round-trip");check(!context.getSharedPreferences("vault",0).getString("encrypted","").contains("test-secret-marker"),"Secrets not persisted as plaintext");}finally{store.data=original;store.save();}
  check(MainActivity.icmp("127.0.0.1")>=0,"Actual loopback ICMP measurement");
  java.net.ServerSocket echo=new java.net.ServerSocket(0,1,InetAddress.getByName("127.0.0.1"));ExecutorService executor=Executors.newSingleThreadExecutor();Future<?> echoTask=executor.submit(()->{try(java.net.Socket peer=echo.accept()){byte[] request=new byte[4];new DataInputStream(peer.getInputStream()).readFully(request);peer.getOutputStream().write(request);peer.getOutputStream().flush();}catch(Exception e){throw new RuntimeException(e);}});
  Libv2ray.initCoreEnv(context.getFilesDir().getAbsolutePath(),"");CoreController core=Libv2ray.newCoreController(new CoreCallbackHandler(){public long startup(){return 0;}public long shutdown(){return 0;}public long onEmitStatus(long code,String text){return 0;}});
  try{JSONObject config=new JSONObject("{\"log\":{\"loglevel\":\"warning\"},\"inbounds\":[{\"listen\":\"127.0.0.1\",\"port\":17891,\"protocol\":\"socks\",\"settings\":{\"auth\":\"noauth\"}}],\"outbounds\":[{\"tag\":\"proxy\",\"protocol\":\"freedom\"}],\"stats\":{},\"policy\":{\"system\":{\"statsOutboundUplink\":true,\"statsOutboundDownlink\":true}}}");core.startLoop(config.toString(),0);check(core.getIsRunning(),"Native core starts");try(java.net.Socket socket=new java.net.Socket(new java.net.Proxy(java.net.Proxy.Type.SOCKS,new InetSocketAddress("127.0.0.1",17891)))){socket.connect(new InetSocketAddress("127.0.0.1",echo.getLocalPort()),4000);socket.setSoTimeout(4000);socket.getOutputStream().write(new byte[]{1,2,3,4});socket.getOutputStream().flush();byte[] reply=new byte[4];new DataInputStream(socket.getInputStream()).readFully(reply);check(Arrays.equals(reply,new byte[]{1,2,3,4}),"Data traverses native SOCKS outbound");}echoTask.get(5,TimeUnit.SECONDS);Thread.sleep(150);String counters=core.queryAllOutboundTrafficStats();check(counters.contains("downlink")&&counters.contains("uplink"),"Native traffic counters record real transfer");}finally{core.stopLoop();echo.close();executor.shutdownNow();}
  check(!core.getIsRunning(),"Core stops cleanly");
  MainActivity activity=(MainActivity)startActivitySync(new Intent(context,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));Thread.sleep(1200);
  check(js(activity,"typeof ready!=='undefined' && ready") .equals("true"),"Bundled mobile UI receives native state");
  check(js(activity,"document.documentElement.scrollWidth<=innerWidth+1").equals("true"),"Mobile dashboard fits viewport");
  check(js(activity,"document.querySelectorAll('.nav-menu .nav-item').length===2 && document.querySelector('#page-logs').closest('#settingsHub')!==null").equals("true"),"Settings contains update and logs pages");
  for(String language:new String[]{"en","fa"}){js(activity,"setLang('"+language+"',null,false);showPage('settings')");check(js(activity,"document.documentElement.dir==='"+(language.equals("fa")?"rtl":"ltr")+"'").equals("true"),"Language direction: "+language);js(activity,"showPage('servers')");}
  activity.directIp="—";activity.status="Disconnected";activity.sendState();js(activity,"document.getElementById('toastContainer').replaceChildren();setLang('en',null,false);showPage('servers')");
  report.append("ABI: ").append(Arrays.toString(Build.SUPPORTED_ABIS)).append("\nPage size: ").append(android.system.Os.sysconf(android.system.OsConstants._SC_PAGESIZE)).append('\n');
 }
 @android.annotation.SuppressLint("UnspecifiedRegisterReceiverFlag") // API 33+ uses explicit flags; the old overload is used only on legacy Android.
 void runVpnChecks()throws Exception{
  Context context=getTargetContext();MainActivity activity=(MainActivity)startActivitySync(new Intent(context,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));Thread.sleep(800);JSONObject original=new JSONObject(activity.store.data.toString());
  java.util.concurrent.atomic.AtomicBoolean probePassed=new java.util.concurrent.atomic.AtomicBoolean();android.content.BroadcastReceiver probeReceiver=new android.content.BroadcastReceiver(){public void onReceive(Context c,Intent i){probePassed.set(i.getBooleanExtra("success",false));}};if(Build.VERSION.SDK_INT>=33)context.registerReceiver(probeReceiver,new android.content.IntentFilter("org.melavo.probe.RESULT"),Context.RECEIVER_EXPORTED);else context.registerReceiver(probeReceiver,new android.content.IntentFilter("org.melavo.probe.RESULT"));
  try{JSONObject config=ConfigCodec.parse("vless://11111111-1111-1111-1111-111111111111@127.0.0.1:19043?type=tcp&security=none#VPN%20test").getJSONObject(0);JSONObject node=SecureStore.nodeFor(config).put("single",true);JSONObject group=new JSONObject().put("id",UUID.randomUUID().toString()).put("subscription",false).put("name","VPN test").put("url","").put("nodes",new JSONArray().put(node));activity.store.data.put("groups",new JSONArray().put(group));activity.store.save();activity.command(new JSONObject().put("action","connect").put("nodeId",node.getString("id")));
   long deadline=SystemClock.elapsedRealtime()+60000;while(!MelavoVpnService.running){if(!MelavoVpnService.error.isEmpty())throw new Exception(MelavoVpnService.error);if(SystemClock.elapsedRealtime()>deadline)throw new Exception("VPN consent/start timed out");Thread.sleep(150);}check(MelavoVpnService.running,"Android VpnService established a real TUN interface");
   runOnMainSync(()->activity.startActivity(new Intent().setClassName("org.melavo.probe","org.melavo.probe.ProbeActivity")));deadline=SystemClock.elapsedRealtime()+25000;while(!probePassed.get()||MelavoVpnService.totalDownload==0){if(SystemClock.elapsedRealtime()>deadline)throw new Exception("No TUN traffic measured");Thread.sleep(200);}check(MelavoVpnService.totalDownload>0&&MelavoVpnService.totalUpload>0,"Traffic from a separate Android UID traverses VPN and native proxy");
  }finally{activity.startService(new Intent(activity,MelavoVpnService.class).setAction("stop"));long deadline=SystemClock.elapsedRealtime()+15000;while(MelavoVpnService.running&&SystemClock.elapsedRealtime()<deadline)Thread.sleep(100);activity.store.data=original;activity.store.save();context.unregisterReceiver(probeReceiver);}check(!MelavoVpnService.running,"VPN disconnect closes the TUN interface");
 }
 String js(MainActivity activity,String code)throws Exception{CountDownLatch done=new CountDownLatch(1);String[] output=new String[1];runOnMainSync(()->activity.web.evaluateJavascript(code,value->{output[0]=value;done.countDown();}));if(!done.await(8,TimeUnit.SECONDS))throw new Exception("WebView timeout");return output[0];}
}
