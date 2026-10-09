package org.melavo.vpn;
import android.content.Context;
import android.security.keystore.*;
import android.util.Base64;
import org.json.*;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.*;
import javax.crypto.spec.GCMParameterSpec;

final class SecureStore {
 final Context context;JSONObject data;
 SecureStore(Context c)throws Exception{context=c.getApplicationContext();String saved=context.getSharedPreferences("vault",0).getString("encrypted",null);data=saved==null?new JSONObject().put("groups",new JSONArray()).put("preferences",new JSONObject()):new JSONObject(decrypt(saved));}
 private javax.crypto.SecretKey key()throws Exception{KeyStore store=KeyStore.getInstance("AndroidKeyStore");store.load(null);if(!store.containsAlias("MelavoVault")){KeyGenerator gen=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");gen.init(new KeyGenParameterSpec.Builder("MelavoVault",KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());gen.generateKey();}return(javax.crypto.SecretKey)store.getKey("MelavoVault",null);}
 private String decrypt(String text)throws Exception{JSONObject o=new JSONObject(text);Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.DECRYPT_MODE,key(),new GCMParameterSpec(128,Base64.decode(o.getString("iv"),Base64.NO_WRAP)));return new String(cipher.doFinal(Base64.decode(o.getString("data"),Base64.NO_WRAP)),StandardCharsets.UTF_8);}
 synchronized void save()throws Exception{Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.ENCRYPT_MODE,key());JSONObject box=new JSONObject().put("iv",Base64.encodeToString(cipher.getIV(),Base64.NO_WRAP)).put("data",Base64.encodeToString(cipher.doFinal(data.toString().getBytes(StandardCharsets.UTF_8)),Base64.NO_WRAP));if(!context.getSharedPreferences("vault",0).edit().putString("encrypted",box.toString()).commit())throw new Exception("Could not save settings");}
 JSONArray groups()throws Exception{return data.getJSONArray("groups");}
 JSONObject preferences()throws Exception{return data.getJSONObject("preferences");}
 JSONObject node(String id)throws Exception{JSONArray g=groups();for(int i=0;i<g.length();i++){JSONArray nodes=g.getJSONObject(i).getJSONArray("nodes");for(int j=0;j<nodes.length();j++)if(nodes.getJSONObject(j).getString("id").equals(id))return nodes.getJSONObject(j);}throw new Exception("Configuration not found");}
 static JSONObject nodeFor(JSONObject config)throws Exception{JSONObject outbound=ConfigCodec.outbound(config),stream=outbound.optJSONObject("streamSettings");return new JSONObject().put("id",java.util.UUID.randomUUID().toString()).put("config",config).put("name",config.optString("remarks","Server")).put("type",outbound.getString("protocol").toUpperCase(java.util.Locale.ROOT)).put("proto",stream==null?"":stream.optString("network","tcp")+" · "+stream.optString("security","none")).put("flag","🌍").put("ping",JSONObject.NULL).put("failed",false);}
}
