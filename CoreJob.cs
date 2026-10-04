using System.Diagnostics;
using System.Runtime.InteropServices;
namespace MelavoClient;
sealed class CoreJob:IDisposable {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes,string? name);
 [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job,int type,IntPtr data,uint length);
 [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [StructLayout(LayoutKind.Sequential)] struct Basic {public long UserTime,JobTime;public uint Flags;public UIntPtr Minimum,Maximum;public uint Active;public UIntPtr Affinity;public uint Priority,Scheduling;}
 [StructLayout(LayoutKind.Sequential)] struct Io {public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes;}
 [StructLayout(LayoutKind.Sequential)] struct Extended {public Basic Basic;public Io Io;public UIntPtr ProcessMemory,JobMemory,PeakProcess,PeakJob;}
 IntPtr handle;
 public CoreJob(){handle=CreateJobObject(IntPtr.Zero,null);var info=new Extended{Basic=new Basic{Flags=0x2000}};var p=Marshal.AllocHGlobal(Marshal.SizeOf<Extended>());try{Marshal.StructureToPtr(info,p,false);if(handle==IntPtr.Zero||!SetInformationJobObject(handle,9,p,(uint)Marshal.SizeOf<Extended>()))throw new InvalidOperationException("Cannot create core job");}finally{Marshal.FreeHGlobal(p);}}
 public void Add(Process process){if(!AssignProcessToJobObject(handle,process.Handle)){try{process.Kill(true);}catch{}throw new InvalidOperationException("Cannot protect child process");}}
 public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}}
}
