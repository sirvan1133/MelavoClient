using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace MelavoClient;
// Blur the shared decorative backdrop once, then composite that cached material.
// No window capture, text blur, or convolution runs on the animation timer.
static class GlassMaterial {
 static Bitmap? scene,blurred;static bool sceneDark;
 public static int BlurBuilds{get;private set;}
 sealed class WindowSurface:IDisposable {
  public Bitmap? Ambient,Material;public Size Size;public bool Dark;
  public void Dispose(){Ambient?.Dispose();Material?.Dispose();Ambient=Material=null;}
 }
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control,WindowSurface> windows=new();
 static WindowSurface ForWindow(Control root){
  EnsureScene();var cached=windows.GetValue(root,owner=>{var value=new WindowSurface();owner.Disposed+=(_,_)=>value.Dispose();return value;});
  if(cached.Ambient!=null&&cached.Size==root.Size&&cached.Dark==Design.Dark)return cached;
  cached.Dispose();cached.Size=root.Size;cached.Dark=Design.Dark;int width=Math.Max(1,root.Width)+36,height=Math.Max(1,root.Height)+36;
  cached.Ambient=new Bitmap(width,height);using(var g=Graphics.FromImage(cached.Ambient))DrawCover(g,scene!,new Rectangle(0,0,width,height));
  cached.Material=new Bitmap(width,height);using(var g=Graphics.FromImage(cached.Material)){DrawCover(g,blurred!,new Rectangle(0,0,width,height));using var tint=new SolidBrush(Design.Dark?Color.FromArgb(70,18,27,52):Color.FromArgb(155,250,252,255));g.FillRectangle(tint,new Rectangle(0,0,width,height));}
  return cached;
 }
 // Preserve the artwork's proportions; keep the globe on the left when cropping.
 static void DrawCover(Graphics graphics,Image image,Rectangle destination){
  float scale=Math.Max(destination.Width/(float)image.Width,destination.Height/(float)image.Height);
  var source=new RectangleF(0,Math.Max(0,(image.Height-destination.Height/scale)/2),destination.Width/scale,destination.Height/scale);
  graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
  graphics.DrawImage(image,destination,source,GraphicsUnit.Pixel);
 }
 // Percentage controls how much softened detail is mixed into the original artwork.
 static Bitmap Softened(Bitmap original,int radius,float amount){
  using var filtered=BackdropBlur.Create(original,radius);var result=new Bitmap(original.Width,original.Height);
  using var graphics=Graphics.FromImage(result);graphics.DrawImageUnscaled(original,0,0);
  using var attributes=new ImageAttributes();attributes.SetColorMatrix(new ColorMatrix{Matrix33=amount});
  graphics.DrawImage(filtered,new Rectangle(0,0,result.Width,result.Height),0,0,filtered.Width,filtered.Height,GraphicsUnit.Pixel,attributes);
  return result;
 }
 static void EnsureScene(){
  if(scene!=null&&sceneDark==Design.Dark)return;scene?.Dispose();blurred?.Dispose();sceneDark=Design.Dark;
  const int width=1536,height=960;
  using var artworkScene=new Bitmap(width,height);
  using(var graphics=Graphics.FromImage(artworkScene)){
   graphics.SmoothingMode=SmoothingMode.AntiAlias;
   using var ground=new LinearGradientBrush(new Rectangle(0,0,width,height),Design.Dark?Color.FromArgb(7,16,43):Color.FromArgb(226,237,255),Design.Dark?Color.FromArgb(5,10,30):Color.FromArgb(245,243,255),160f);
   graphics.FillRectangle(ground,0,0,width,height);
   void Wash(RectangleF bounds,Color color){using var path=new GraphicsPath();path.AddEllipse(bounds);using var brush=new PathGradientBrush(path){CenterColor=color,SurroundColors=new[]{Color.FromArgb(0,color)}};graphics.FillPath(brush,path);}
   Wash(new RectangleF(-500,-460,1700,1250),Color.FromArgb(Design.Dark?90:40,40,100,220));
   Wash(new RectangleF(700,380,1450,1250),Color.FromArgb(Design.Dark?70:32,110,70,230));
   Wash(new RectangleF(-150,430,1500,220),Color.FromArgb(Design.Dark?24:14,80,170,255));
  }
  scene=Softened(artworkScene,5,.05f);blurred=BackdropBlur.Create(artworkScene,15);BlurBuilds++;
 }
 static (Control Root,Point Offset) Coordinates(Control control){var offset=Point.Empty;Control root=control;while(root.Parent!=null){offset.Offset(root.Left,root.Top);root=root.Parent;}return(root,offset);}
 public static void Ambient(Graphics g,Control control){
  var (root,offset)=Coordinates(control);var cached=ForWindow(root);g.DrawImageUnscaled(cached.Ambient!,-offset.X-18,-offset.Y-18);
 }
 public static void Surface(Graphics g,Control control){
  var (root,offset)=Coordinates(control);var cached=ForWindow(root);g.DrawImageUnscaled(cached.Material!,-offset.X-18,-offset.Y-18);
 }
 public static void Backdrop(Graphics g,Control control){
  if(control.FindForm() is Form dialog && dialog is not Client){Control? parent=control.Parent;while(parent!=null&&parent.BackColor.A!=255)parent=parent.Parent;g.Clear(parent?.BackColor??dialog.BackColor);return;}
  var offset=Point.Empty;Control? ancestor=control;while(ancestor!=null&&ancestor is not Card){offset.Offset(ancestor.Left,ancestor.Top);ancestor=ancestor.Parent;}
  if(ancestor is Card card){Surface(g,control);Finish(g,control,new Rectangle(-offset.X,-offset.Y,Math.Max(1,card.Width),Math.Max(1,card.Height)));}else{Ambient(g,control);if(control is ModernButton){var saved=g.Save();using var shape=Design.Round(new RectangleF(1,1,control.Width-2,control.Height-2),Design.RadiusInput);g.SetClip(shape,CombineMode.Intersect);Surface(g,control);g.Restore(saved);}}
 }
 static void Finish(Graphics g,Control control,Rectangle bounds){
  using var highlight=new LinearGradientBrush(bounds,Color.FromArgb(Design.Dark?20:35,255,255,255),Color.FromArgb(0,255,255,255),110f);g.FillRectangle(highlight,control.ClientRectangle);
  using var shadow=new LinearGradientBrush(bounds,Color.Transparent,Color.FromArgb(Design.Dark?13:5,0,3,14),90f);g.FillRectangle(shadow,control.ClientRectangle);
 }
 public static void PaintCard(Graphics g,Control control){
  Ambient(g,control);if(control.Width<4||control.Height<4)return;
  using var shape=Design.Round(new RectangleF(.5f,.5f,control.Width-1,control.Height-1),Design.RadiusCard);
  var state=g.Save();g.SetClip(shape,CombineMode.Intersect);Surface(g,control);
  Finish(g,control,control.ClientRectangle);g.Restore(state);
 }
 public static void Title(Graphics g,Control control,bool separator=true){Surface(g,control);using var veil=new SolidBrush(Design.Dark?Color.FromArgb(105,5,12,27):Color.FromArgb(90,245,250,255));g.FillRectangle(veil,control.ClientRectangle);using var line=new Pen(Color.FromArgb(65,140,174,231));if(separator)g.DrawLine(line,0,control.Height-1,control.Width,control.Height-1);}
 public static void Input(Graphics g,Rectangle bounds,GraphicsPath shape,double focus){
  using var fill=new SolidBrush(Design.Surface);g.FillPath(fill,shape);
  if(focus>0){using var accent=new Pen(Color.FromArgb((int)(Math.Clamp(focus,0,1)*90),Design.Accent),1.25f);g.DrawPath(accent,shape);}
 }
}
// Cached, cropped reflection frames: timer paints only an image, never paths or blur.
sealed class GlassEdge:IDisposable {
 const double Span=.09;
 PointF[] points=Array.Empty<PointF>();float[] distance=Array.Empty<float>();float perimeter;Size size;int radius;

 static int sequence;
 public double Phase{get;private set;}=(System.Threading.Interlocked.Increment(ref sequence)*.173)%1;
 public int FrameBuilds{get;private set;}
 public void SetDelay(double seconds){Phase=((1-seconds/10)%1+1)%1;}
 void Build(Control owner){
  if(size==owner.Size&&radius==Design.RadiusCard)return;size=owner.Size;radius=Design.RadiusCard;
  using var path=Design.Round(new RectangleF(1,1,Math.Max(2,owner.Width-2),Math.Max(2,owner.Height-2)),Math.Min(radius,Math.Min(owner.Width,owner.Height)/2f-1));path.Flatten(null,.15f);points=path.PathPoints.Concat(new[]{path.PathPoints[0]}).ToArray();distance=new float[points.Length];
  for(int i=1;i<points.Length;i++){float dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;distance[i]=distance[i-1]+MathF.Sqrt(dx*dx+dy*dy);}perimeter=distance[^1];
 }
 public void Advance(double delta,bool hovered){Phase=(Phase+delta/10)%1;}
 public PointF Position(Control owner,double phase){Build(owner);return At(phase);}
 PointF At(double phase){float at=(float)((phase%1+1)%1)*perimeter;int index=Array.FindIndex(distance,value=>value>=at);index=Math.Clamp(index,1,points.Length-1);float t=(at-distance[index-1])/Math.Max(.01f,distance[index]-distance[index-1]);return new(points[index-1].X+(points[index].X-points[index-1].X)*t,points[index-1].Y+(points[index].Y-points[index-1].Y)*t);}
 public void Paint(Graphics g,Control owner,double hover){
  if(owner.Width<12||owner.Height<12||!GlassAnimation.MotionAllowed)return;Build(owner);
  const int samples=48;var line=Enumerable.Range(0,samples+1).Select(i=>At(Phase-Span/2+i*Span/samples)).ToArray();
  var saved=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
  // Filled stroke strips share flat joins, avoiding overlapping round caps.
  void Strip(float width,int peak){
   for(int i=0;i<samples;i++){
    double t=(i+.5)/samples,feather=Math.Pow(Math.Sin(t*Math.PI),3);
    var color=t<.55?Design.Blend(Color.FromArgb(150,205,255),Color.FromArgb(105,185,255),t/.55):Design.Blend(Color.FromArgb(105,185,255),Color.FromArgb(180,150,255),(t-.55)/.45);
    PointF Offset(int index,float sign){var prev=line[Math.Max(0,index-1)];var next=line[Math.Min(samples,index+1)];float dx=next.X-prev.X,dy=next.Y-prev.Y,length=Math.Max(.001f,MathF.Sqrt(dx*dx+dy*dy));return new(line[index].X-dy/length*width*.5f*sign,line[index].Y+dx/length*width*.5f*sign);}
    using var brush=new SolidBrush(Color.FromArgb(Math.Clamp((int)(peak*feather*(1+hover*.15)),0,255),color));
    g.FillPolygon(brush,new[]{Offset(i,1),Offset(i+1,1),Offset(i+1,-1),Offset(i,-1)});
   }
  }
  Strip(9,6);Strip(4,13);Strip(1.1f,166);g.Restore(saved);
 }
 public void Warm(Control owner)=>Build(owner);
 public void Dispose(){}
}
