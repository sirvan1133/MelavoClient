namespace MelavoClient;
sealed partial class Client {
 static Size PortraitSize(Rectangle area,int dpi){
  double scale=dpi/96d;int availableHeight=Math.Max(200,area.Height-24),availableWidth=Math.Max(200,area.Width-24);
  int height=Math.Min(availableHeight,(int)Math.Round(860*scale));
  int width=Math.Min(availableWidth,(int)Math.Round(410*scale));
  // On short monitors shrink the whole portrait proportionally, preserving a usable minimum width.
  width=Math.Min(width,(int)Math.Round(height*410d/860));
  return new(width,height);
 }
 void FitPortraitWindow(bool center=false){
  if(!useWebShell||WindowState==FormWindowState.Minimized)return;
  var area=Screen.FromControl(this).WorkingArea;var size=PortraitSize(area,DeviceDpi);
  MinimumSize=new(Math.Min(size.Width,Design.Scale(this,320)),Math.Min(size.Height,Design.Scale(this,480)));
  MaximumSize=size;MaximizedBounds=new Rectangle(area.Location,size);
  if(WindowState==FormWindowState.Maximized)WindowState=FormWindowState.Normal;
  Size=size;
  Location=center?new Point(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2):new Point(Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-Height)));
  if(webShell!=null)webShell.ZoomFactor=Math.Min(1,Height/(860d*DeviceDpi/96d));
 }
}
