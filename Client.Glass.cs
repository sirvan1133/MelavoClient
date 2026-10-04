using System.Drawing.Drawing2D;
namespace MelavoClient;
sealed partial class Client {
 static void RefreshThemeGeometry(Control root){
  if(root is Card&&root.Width>0&&root.Height>0){using var path=Design.Round(new Rectangle(0,0,root.Width,root.Height),Math.Min(Design.RadiusCard,Math.Min(root.Width,root.Height)/2f));var previous=root.Region;root.Region=new Region(path);previous?.Dispose();}
  foreach(Control child in root.Controls)RefreshThemeGeometry(child);
 }
}
sealed partial class PowerControl {
 void PaintGlass(Graphics g){
  Design.PaintBackdrop(g,this);g.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Min(Width,Height),cx=Width/2f,cy=Height/2f;
  var color=State==ConnectionState.Error?Design.Error:Design.Accent;
  for(int i=4;i>0;i--){float radius=size*(.38f+i*.017f);using var glow=new SolidBrush(Color.FromArgb(State==ConnectionState.Connected?9:4,color));g.FillEllipse(glow,cx-radius,cy-radius,radius*2,radius*2);}
  var face=new RectangleF(cx-size*.37f,cy-size*.37f,size*.74f,size*.74f);
  using var fill=new LinearGradientBrush(face,Design.Blend(Design.Surface,Color.White,Design.Dark?.14:.6),Design.Blend(Design.Surface,color,State==ConnectionState.Connected?.35:.10),65f);g.FillEllipse(fill,face);
  using var rim=new Pen(Design.Blend(Design.Border,Color.White,.35),Math.Max(1,size*.012f));g.DrawEllipse(rim,face);
  using var reflection=new Pen(Color.FromArgb(Design.Dark?80:210,Color.White),Math.Max(1,size*.018f));g.DrawArc(reflection,face.X+2,face.Y+2,face.Width-4,face.Height-4,205,100);
  var glyphColor=State==ConnectionState.Disconnected?Design.Text:color;using var glyph=new Pen(glyphColor,size*.028f){StartCap=LineCap.Round,EndCap=LineCap.Round};float diameter=size*.31f;g.DrawArc(glyph,cx-diameter/2,cy-diameter*.35f,diameter,diameter,-43,266);g.DrawLine(glyph,cx,cy-diameter*.56f,cx,cy-diameter*.06f);
  if(State is ConnectionState.Connecting or ConnectionState.Disconnecting){using var progress=new Pen(color,size*.02f){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawArc(progress,cx-size*.44f,cy-size*.44f,size*.88f,size*.88f,Phase*45,92);}
  if(Focused){using var focus=new Pen(color,2);g.DrawEllipse(focus,cx-size*.46f,cy-size*.46f,size*.92f,size*.92f);}
 }
}
