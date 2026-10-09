package org.melavo.vpn;
import android.net.Uri;
import android.util.Base64;
import org.json.*;
import java.util.*;
import java.nio.charset.StandardCharsets;

final class ConfigCodec {
 static String decode(String value){return Uri.decode(value==null?"":value);}
 static byte[] unbase(String text){return Base64.decode(text.replace('-','+').replace('_','/'),Base64.DEFAULT);}
 static JSONArray parse(String text)throws Exception{
  text=text.trim().replace("\uFEFF","");if(text.isEmpty())throw new Exception("Empty configuration / کانفیگ خالی است");
  if(text.startsWith("{")||text.startsWith("[")){JSONArray a=text.startsWith("[")?new JSONArray(text):new JSONArray().put(new JSONObject(text));for(int i=0;i<a.length();i++)if(a.getJSONObject(i).optJSONArray("outbounds")==null)throw new Exception("Xray JSON must include outbounds");return a;}
  JSONArray configs=new JSONArray();for(String line:text.split("[\r\n]+")){line=line.trim();if(line.startsWith("vless://")||line.startsWith("vmess://")||line.startsWith("trojan://"))configs.put(link(line));}
  if(configs.length()>0)return configs;
  return parse(new String(unbase(text),StandardCharsets.UTF_8));
 }
 static JSONObject link(String text)throws Exception{
  JSONObject outbound,stream=new JSONObject();String remarks;
  if(text.startsWith("vmess://")){
   JSONObject v=new JSONObject(new String(unbase(text.substring(8)),StandardCharsets.UTF_8));String host=v.optString("add"),id=v.optString("id");UUID.fromString(id);int port=Integer.parseInt(v.optString("port"));if(port<1||port>65535||host.isEmpty())throw new Exception("Invalid VMess endpoint");
   outbound=new JSONObject().put("protocol","vmess").put("settings",new JSONObject().put("vnext",new JSONArray().put(new JSONObject().put("address",host).put("port",port).put("users",new JSONArray().put(new JSONObject().put("id",id).put("alterId",v.optInt("aid",0)).put("security",v.optString("scy","auto")))))));
   String network=v.optString("net","tcp"),security=v.optString("tls","");if(security.isEmpty())security="none";stream.put("network",network).put("security",security);String path=v.optString("path"),headerHost=v.optString("host");
   if(network.equals("ws"))stream.put("wsSettings",new JSONObject().put("path",path).put("headers",new JSONObject().put("Host",headerHost)));
   if(network.equals("grpc"))stream.put("grpcSettings",new JSONObject().put("serviceName",path).put("authority",headerHost).put("multiMode",v.optString("type").equals("multi")));
   if(network.equals("httpupgrade"))stream.put("httpupgradeSettings",new JSONObject().put("path",path).put("host",headerHost));
   if(network.equals("tcp")&&v.optString("type").equals("http"))stream.put("tcpSettings",new JSONObject().put("header",new JSONObject().put("type","http").put("request",new JSONObject().put("path",new JSONArray().put(path.isEmpty()?"/":path)).put("headers",new JSONObject().put("Host",new JSONArray().put(headerHost))))));
   if(security.equals("tls")){JSONObject tls=new JSONObject().put("serverName",v.optString("sni",headerHost.isEmpty()?host:headerHost));if(v.has("fp"))tls.put("fingerprint",v.get("fp"));if(!v.optString("alpn").isEmpty())tls.put("alpn",new JSONArray(Arrays.asList(v.optString("alpn").split(","))));stream.put("tlsSettings",tls);}
   for(String key:new String[]{network+"Settings","tlsSettings","realitySettings","sockopt"})if(v.optJSONObject(key)!=null)stream.put(key,v.getJSONObject(key));remarks=v.optString("ps",host+":"+port);
  }else{
   Uri uri=Uri.parse(text);String host=uri.getHost();int port=uri.getPort();if(host==null||port<1||port>65535)throw new Exception("Invalid server endpoint");String authority=uri.getEncodedAuthority();String auth=decode(authority.substring(0,authority.lastIndexOf('@')));
   Map<String,String> q=new HashMap<>();String query=uri.getEncodedQuery();if(query!=null)for(String part:query.split("&")){String[] p=part.split("=",2);q.put(decode(p[0]),p.length==2?decode(p[1]):"");}
   if(uri.getScheme().equals("vless")){UUID.fromString(auth);JSONObject user=new JSONObject().put("id",auth).put("encryption",q.getOrDefault("encryption","none"));if(q.containsKey("flow"))user.put("flow",q.get("flow"));outbound=new JSONObject().put("protocol","vless").put("settings",new JSONObject().put("vnext",new JSONArray().put(new JSONObject().put("address",host).put("port",port).put("users",new JSONArray().put(user)))));}
   else outbound=new JSONObject().put("protocol","trojan").put("settings",new JSONObject().put("servers",new JSONArray().put(new JSONObject().put("address",host).put("port",port).put("password",auth))));
   String network=q.getOrDefault("type","tcp"),security=q.getOrDefault("security","none");stream.put("network",network).put("security",security);
   if(network.equals("ws"))stream.put("wsSettings",new JSONObject().put("path",q.getOrDefault("path","/")).put("headers",new JSONObject().put("Host",q.getOrDefault("host",""))));
   if(network.equals("grpc"))stream.put("grpcSettings",new JSONObject().put("serviceName",q.getOrDefault("serviceName",q.getOrDefault("path",""))).put("authority",q.getOrDefault("authority","")));
   if(network.equals("httpupgrade"))stream.put("httpupgradeSettings",new JSONObject().put("path",q.getOrDefault("path","/")).put("host",q.getOrDefault("host","")));
   if(network.equals("tcp")&&q.containsKey("headerType"))stream.put("tcpSettings",new JSONObject().put("header",new JSONObject().put("type",q.get("headerType"))));
   if(security.equals("tls")||security.equals("reality")){JSONObject tls=new JSONObject().put("serverName",q.getOrDefault("sni",host));String[][] mapping={{"fp","fingerprint"},{"pbk","publicKey"},{"sid","shortId"},{"spx","spiderX"}};for(String[] pair:mapping)if(q.containsKey(pair[0]))tls.put(pair[1],q.get(pair[0]));if(q.containsKey("alpn"))tls.put("alpn",new JSONArray(Arrays.asList(q.get("alpn").split(","))));if(q.containsKey("allowInsecure"))tls.put("allowInsecure",q.get("allowInsecure").equals("1")||q.get("allowInsecure").equals("true"));stream.put(security+"Settings",tls);}
   remarks=decode(uri.getEncodedFragment());if(remarks.isEmpty())remarks=host+":"+port;
  }
  outbound.put("tag","proxy").put("streamSettings",stream);return new JSONObject().put("remarks",remarks).put("outbounds",new JSONArray().put(outbound)).put("_shareLink",text);
 }
 static JSONObject outbound(JSONObject config)throws Exception{JSONArray a=config.getJSONArray("outbounds");for(int i=0;i<a.length();i++){JSONObject o=a.getJSONObject(i);if(!Arrays.asList("freedom","blackhole","dns").contains(o.optString("protocol")))return o;}throw new Exception("Proxy outbound missing");}
 static String share(JSONObject config)throws Exception{if(config.has("_shareLink"))return config.getString("_shareLink");JSONObject clean=new JSONObject(config.toString());clean.remove("_shareLink");return clean.toString(2);}
 static String host(JSONObject config)throws Exception{JSONObject o=outbound(config),settings=o.getJSONObject("settings");JSONArray endpoints=settings.optJSONArray("vnext");if(endpoints==null)endpoints=settings.optJSONArray("servers");if(endpoints==null)throw new Exception("Server address missing");return endpoints.getJSONObject(0).getString("address");}
 static JSONObject prepare(JSONObject config)throws Exception{
  JSONObject c=new JSONObject(config.toString());c.remove("_shareLink");JSONObject proxy=outbound(c);String tag=proxy.optString("tag","proxy");proxy.put("tag",tag);
  c.put("inbounds",new JSONArray().put(new JSONObject().put("tag","melavo-tun").put("protocol","tun").put("settings",new JSONObject().put("name","MelavoTUN").put("mtu",1500).put("userLevel",0))).put(new JSONObject().put("tag","melavo-socks").put("listen","127.0.0.1").put("port",17890).put("protocol","socks").put("settings",new JSONObject().put("auth","noauth").put("udp",true))));
  c.put("log",new JSONObject().put("loglevel","warning"));c.put("stats",new JSONObject());JSONObject policy=c.optJSONObject("policy");if(policy==null)policy=new JSONObject();policy.put("system",new JSONObject().put("statsOutboundUplink",true).put("statsOutboundDownlink",true));c.put("policy",policy);
  JSONObject routing=c.optJSONObject("routing");if(routing==null)routing=new JSONObject();JSONArray rules=routing.optJSONArray("rules");if(rules==null)rules=new JSONArray();JSONArray combined=new JSONArray().put(new JSONObject().put("type","field").put("inboundTag",new JSONArray().put("melavo-socks")).put("outboundTag",tag));for(int i=0;i<rules.length();i++)combined.put(rules.get(i));combined.put(new JSONObject().put("type","field").put("inboundTag",new JSONArray().put("melavo-tun")).put("outboundTag",tag));routing.put("rules",combined);c.put("routing",routing);return c;
 }
}
