using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace MelavoClient;
sealed class SubscriptionGroup {
 public string Id {get;set;}=Guid.NewGuid().ToString("N");
 public string Name {get;set;}="ساب";
 public string Url {get;set;}="";
 public List<JsonObject> Profiles {get;set;}=new();
 public List<JsonObject> SourceProfiles {get;set;}=new();
 public Dictionary<string,JsonObject> Overrides {get;set;}=new();
 public bool AutoUpdate {get;set;}=true;
 public int AutoUpdateHours {get;set;}=12;
 public DateTime? Updated {get;set;}
 public long? UsedBytes {get;set;}
 public long? TotalBytes {get;set;}
 public long? ExpiresUnix {get;set;}
 public override string ToString()=>Name;
}
static class SubscriptionStore {
#if CUSTOMER_EDITION
 public const string ProfileName="MelavoClient-Customer";
 public const string Edition="customer";
#else
 public const string ProfileName="MelavoClient";
 public const string Edition="standard";
#endif
 internal static string? CheckDirectory;
 public static string DirectoryPath=>CheckDirectory??Path.Combine(AppContext.BaseDirectory,"data",ProfileName);
 static string StorePath=>Path.Combine(DirectoryPath,"subscriptions.dat");
 [StructLayout(LayoutKind.Sequential)] struct Blob {public int Length;public IntPtr Data;}
 [DllImport("crypt32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CryptProtectData(ref Blob input,string? description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("crypt32.dll",SetLastError=true)] static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr pointer);
 static byte[] Transform(byte[] bytes,bool encrypt){var input=new Blob{Length=bytes.Length,Data=Marshal.AllocHGlobal(bytes.Length)};Blob output=default;try{Marshal.Copy(bytes,0,input.Data,bytes.Length);bool ok=encrypt?CryptProtectData(ref input,null,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);if(!ok)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());var result=new byte[output.Length];Marshal.Copy(output.Data,result,0,output.Length);return result;}finally{Marshal.FreeHGlobal(input.Data);if(output.Data!=IntPtr.Zero)LocalFree(output.Data);}}
 public static List<SubscriptionGroup> Read()=>!File.Exists(StorePath)?new():JsonSerializer.Deserialize<List<SubscriptionGroup>>(Transform(File.ReadAllBytes(StorePath),false))??new();
 public static void Save(List<SubscriptionGroup> groups){Directory.CreateDirectory(DirectoryPath);var temp=StorePath+".tmp";File.WriteAllBytes(temp,Transform(JsonSerializer.SerializeToUtf8Bytes(groups),true));File.Move(temp,StorePath,true);}
 public static void SelfTest(){var data=Encoding.UTF8.GetBytes("store-roundtrip");if(!data.SequenceEqual(Transform(Transform(data,true),false)))throw new Exception("DPAPI test failed");var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dat");try{var groups=new List<SubscriptionGroup>{new(){Name="test-group",Url="https://example.invalid/sub/test-secret",Profiles=new(){new JsonObject{["remarks"]="test",["outbounds"]=new JsonArray(new JsonObject{["protocol"]="vless"})}},Updated=DateTime.UtcNow}};File.WriteAllBytes(path,Transform(JsonSerializer.SerializeToUtf8Bytes(groups),true));var recovered=JsonSerializer.Deserialize<List<SubscriptionGroup>>(Transform(File.ReadAllBytes(path),false))!;if(recovered.Count!=1||recovered[0].Name!=groups[0].Name||recovered[0].Url!=groups[0].Url||recovered[0].Profiles[0]["outbounds"]![0]!["protocol"]!.ToString()!="vless")throw new Exception("Persistence test failed");if(Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains("test-secret"))throw new Exception("Store encryption test failed");}finally{File.Delete(path);}}
}
