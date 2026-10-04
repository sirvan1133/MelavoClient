using System.Drawing.Drawing2D;
namespace MelavoClient;
static class MelavoBrand {
 public static void Mark(Graphics g,RectangleF bounds){
  g.SmoothingMode=SmoothingMode.AntiAlias;
  using var frame=Design.Round(bounds,bounds.Width*.27f);
  using var fill=new LinearGradientBrush(bounds,Color.FromArgb(43,76,136),Color.FromArgb(14,24,51),65);
  g.FillPath(fill,frame);using var border=new Pen(Color.FromArgb(145,153,203,255),1);g.DrawPath(border,frame);
  float x=bounds.X,y=bounds.Y,s=bounds.Width;
  using var mark=new GraphicsPath();mark.AddLines(new[]{new PointF(x+s*.24f,y+s*.70f),new PointF(x+s*.24f,y+s*.30f),new PointF(x+s*.50f,y+s*.53f),new PointF(x+s*.76f,y+s*.30f),new PointF(x+s*.76f,y+s*.70f)});
  using var glow=new Pen(Color.FromArgb(32,97,220,255),s*.13f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawPath(glow,mark);
  using var ink=new Pen(Color.FromArgb(155,235,255),s*.073f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawPath(ink,mark);
 }
}
sealed class BrandLockup:Control {
 public BrandLockup(){DoubleBuffered=true;AccessibleName="Melavo VPN";Size=new(176,116);TabStop=false;}
 protected override void OnPaint(PaintEventArgs e){
  Design.PaintBackdrop(e.Graphics,this);MelavoBrand.Mark(e.Graphics,new RectangleF((Width-42)/2f,3,42,42));
  using var name=Design.Font(16,FontStyle.Bold);using var caption=Design.Font(7);
  TextRenderer.DrawText(e.Graphics,"Melavo",name,new Rectangle(0,51,Width,34),Design.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  TextRenderer.DrawText(e.Graphics,"WINDOWS VPN",caption,new Rectangle(0,88,Width,20),Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
 }
}
