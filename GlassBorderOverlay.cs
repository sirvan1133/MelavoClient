using System.Drawing.Drawing2D;
namespace MelavoClient;
// A separate, clipped border surface keeps animation invalidations away from text.
sealed class GlassBorderOverlay:Control {
 readonly Card card;readonly GlassEdge edge;readonly Func<double> hover;
 Bitmap? material;bool materialDark;Size windowSize;Point position;
 public GlassBorderOverlay(Card owner,GlassEdge reflection,Func<double> emphasis){card=owner;edge=reflection;hover=emphasis;TabStop=false;Enabled=false;Visible=false;AccessibleRole=AccessibleRole.None;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 public void Fit(){Bounds=card.ClientRectangle;if(Width<8||Height<8)return;int band=Design.Scale(this,6);using var outside=Design.Round(new RectangleF(0,0,Width,Height),Design.RadiusCard);var ring=new Region(outside);if(Width>band*2&&Height>band*2){using var inside=Design.Round(new RectangleF(band,band,Width-band*2,Height-band*2),Math.Max(1,Design.RadiusCard-band));ring.Exclude(inside);}var previous=Region;Region=ring;previous?.Dispose();BringToFront();}
 protected override void OnPaintBackground(PaintEventArgs e){
  var form=FindForm();var currentWindow=form?.Size??Size;var currentPosition=IsHandleCreated?PointToScreen(Point.Empty):Location;
  if(material==null||material.Size!=Size||materialDark!=Design.Dark||windowSize!=currentWindow||position!=currentPosition){material?.Dispose();material=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));using var graphics=Graphics.FromImage(material);GlassMaterial.PaintCard(graphics,this);materialDark=Design.Dark;windowSize=currentWindow;position=currentPosition;}
  e.Graphics.DrawImageUnscaled(material,0,0);
 }
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var shape=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusCard);using var border=new Pen(Design.Blend(Design.Border,Design.BorderHover,.18+hover()*.3));e.Graphics.DrawPath(border,shape);edge.Paint(e.Graphics,this,hover());}
 protected override void Dispose(bool disposing){if(disposing)material?.Dispose();base.Dispose(disposing);}
}
