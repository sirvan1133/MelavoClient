using System.Drawing.Drawing2D;
namespace MelavoClient;
// A separate, clipped border surface keeps animation invalidations away from text.
sealed class GlassBorderOverlay:Control {
 readonly Card card;readonly GlassEdge edge;readonly Func<double> hover;
 Bitmap? material;bool materialDark;Size windowSize;Point position;
 public GlassBorderOverlay(Card owner,GlassEdge reflection,Func<double> emphasis){card=owner;edge=reflection;hover=emphasis;TabStop=false;Enabled=false;Visible=false;AccessibleRole=AccessibleRole.None;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 public void ResetMaterial(){material?.Dispose();material=null;Visible=Design.Glass;Fit();Invalidate();}
 public void Fit(){Bounds=card.ClientRectangle;if(Width<8||Height<8)return;int band=6;using var outside=Design.Round(new RectangleF(0,0,Width,Height),Design.RadiusCard);var ring=new Region(outside);if(Width>band*2&&Height>band*2){using var inside=Design.Round(new RectangleF(band,band,Width-band*2,Height-band*2),Math.Max(1,Design.RadiusCard-band));ring.Exclude(inside);}var previous=Region;Region=ring;previous?.Dispose();BringToFront();}
 protected override void OnPaintBackground(PaintEventArgs e){
  var form=FindForm();var currentWindow=form?.Size??Size;var currentPosition=IsHandleCreated?PointToScreen(Point.Empty):Location;
  if(material==null||material.Size!=Size||materialDark!=Design.Dark||windowSize!=currentWindow||position!=currentPosition){material?.Dispose();material=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));using var graphics=Graphics.FromImage(material);GlassMaterial.PaintCard(graphics,this);materialDark=Design.Dark;windowSize=currentWindow;position=currentPosition;}
  e.Graphics.DrawImageUnscaled(material,0,0);
 }
 public static void BaseBorder(Graphics g,Control owner,double hover){
  using var shape=Design.Round(new RectangleF(.5f,.5f,owner.Width-1,owner.Height-1),Design.RadiusCard);using var border=new Pen(Color.FromArgb((int)(41*(1+hover*.12)),170,205,255),1);g.DrawPath(border,shape);
  using var inner=Design.Round(new RectangleF(1.5f,1.5f,owner.Width-3,owner.Height-3),Design.RadiusCard-1);using var light=new Pen(Color.FromArgb(15,255,255,255),1);g.DrawPath(light,inner);
 }
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;BaseBorder(e.Graphics,this,hover());edge.Paint(e.Graphics,this,hover());}
 protected override void Dispose(bool disposing){if(disposing)material?.Dispose();base.Dispose(disposing);}
}



