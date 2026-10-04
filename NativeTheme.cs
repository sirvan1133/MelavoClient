using System.Runtime.InteropServices;
namespace MelavoClient;
static class NativeTheme {
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
 [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)] static extern int SetWindowTheme(IntPtr window,string? theme,string? subId);
 public static void Window(IntPtr handle){try{int dark=Design.Dark?1:0;DwmSetWindowAttribute(handle,20,ref dark,4);int rounded=2;DwmSetWindowAttribute(handle,33,ref rounded,4);int caption=Design.Surface.R|(Design.Surface.G<<8)|(Design.Surface.B<<16);int text=Design.Text.R|(Design.Text.G<<8)|(Design.Text.B<<16);DwmSetWindowAttribute(handle,35,ref caption,4);DwmSetWindowAttribute(handle,36,ref text,4);}catch{}}
 public static void Input(Control control){try{if(control.IsHandleCreated)SetWindowTheme(control.Handle,Design.Dark?"DarkMode_Explorer":"Explorer",null);}catch{}}
}
