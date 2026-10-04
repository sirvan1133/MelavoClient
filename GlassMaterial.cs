using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace MelavoClient;
// Blur the shared decorative backdrop once, then composite that cached material.
// No window capture, text blur, or convolution runs on the animation timer.
static class GlassMaterial {
 static Bitmap? scene,blurred;static bool sceneDark;static readonly Stopwatch time=Stopwatch.StartNew();
 public static double Seconds=>time.Elapsed.TotalSeconds;
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
  cached.Ambient=new Bitmap(width,height);using(var g=Graphics.FromImage(cached.Ambient))g.DrawImage(scene!,new Rectangle(0,0,width,height));
  cached.Material=new Bitmap(width,height);using(var g=Graphics.FromImage(cached.Material)){g.DrawImage(blurred!,new Rectangle(0,0,width,height));using var tint=new SolidBrush(Design.Dark?Color.FromArgb(140,18,38,74):Color.FromArgb(192,250,252,255));g.FillRectangle(tint,new Rectangle(0,0,width,height));}
  return cached;
 }
 static void EnsureScene(){
  if(scene!=null&&sceneDark==Design.Dark)return;scene?.Dispose();blurred?.Dispose();sceneDark=Design.Dark;
  scene=new Bitmap(1024,768);using(var g=Graphics.FromImage(scene)){
   g.SmoothingMode=SmoothingMode.AntiAlias;using var fill=new LinearGradientBrush(new Rectangle(0,0,1024,768),Design.Dark?Color.FromArgb(7,13,27):Color.FromArgb(223,235,253),Design.Dark?Color.FromArgb(12,19,39):Color.FromArgb(239,240,253),55f);g.FillRectangle(fill,new Rectangle(0,0,1024,768));
   void Glow(RectangleF bounds,Color color,int strength){using var path=new GraphicsPath();path.AddEllipse(bounds);using var brush=new PathGradientBrush(path){CenterColor=Color.FromArgb(strength,color),SurroundColors=new[]{Color.FromArgb(0,color)}};g.FillPath(brush,path);}
   Glow(new RectangleF(-220,-310,1000,900),Color.FromArgb(34,126,240),Design.Dark?84:68);
   Glow(new RectangleF(430,-80,900,860),Color.FromArgb(77,73,186),Design.Dark?65:46);
   Glow(new RectangleF(-180,420,1100,780),Color.FromArgb(23,141,183),Design.Dark?41:35);
   using var wave=new GraphicsPath();wave.AddBezier(-80,550,250,180,570,850,1100,230);using var light=new Pen(Color.FromArgb(Design.Dark?17:21,110,163,226),26){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawPath(light,wave);
  }
  using(var stream=typeof(GlassMaterial).Assembly.GetManifestResourceStream("MelavoClient.Assets.GlassWallpaper.png"))if(stream!=null){using var artwork=Image.FromStream(stream);using var graphics=Graphics.FromImage(scene);graphics.DrawImage(artwork,new Rectangle(0,0,scene.Width,scene.Height));if(Design.Dark){using var calm=new SolidBrush(Color.FromArgb(100,7,15,38));graphics.FillRectangle(calm,new Rectangle(0,0,scene.Width,scene.Height));}if(!Design.Dark){using var veil=new SolidBrush(Color.FromArgb(178,239,246,255));graphics.FillRectangle(veil,new Rectangle(0,0,scene.Width,scene.Height));}}
  blurred=BackdropBlur.Create(scene,7);BlurBuilds++;
 }
 static (Control Root,Point Offset) Coordinates(Control control){var offset=Point.Empty;Control root=control;while(root.Parent!=null){offset.Offset(root.Left,root.Top);root=root.Parent;}return(root,offset);}
 public static void Ambient(Graphics g,Control control){
  var (root,offset)=Coordinates(control);var cached=ForWindow(root);int drift=(int)(Math.Sin(Seconds*Math.PI/12)*9);g.DrawImageUnscaled(cached.Ambient!,-offset.X-18+drift,-offset.Y-18);
 }
 public static void Surface(Graphics g,Control control){
  var (root,offset)=Coordinates(control);var cached=ForWindow(root);g.DrawImageUnscaled(cached.Material!,-offset.X-18,-offset.Y-18);
 }
 public static void Backdrop(Graphics g,Control control){
  var offset=Point.Empty;Control? ancestor=control;while(ancestor!=null&&ancestor is not Card){offset.Offset(ancestor.Left,ancestor.Top);ancestor=ancestor.Parent;}
  if(ancestor is Card card){Surface(g,control);Finish(g,control,new Rectangle(-offset.X,-offset.Y,Math.Max(1,card.Width),Math.Max(1,card.Height)));}else Ambient(g,control);
 }
 static void Finish(Graphics g,Control control,Rectangle bounds){
  using var highlight=new LinearGradientBrush(bounds,Color.FromArgb(Design.Dark?12:35,164,192,255),Color.FromArgb(0,255,255,255),110f);g.FillRectangle(highlight,control.ClientRectangle);
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
  using var inner=new Pen(Color.FromArgb(Design.Dark?18:90,Color.White));
  
 }
}
// Cached, cropped reflection frames: timer paints only an image, never paths or blur.
sealed class GlassEdge:IDisposable {
 const int FrameCount=300;const double Span=.09;
 PointF[] points=Array.Empty<PointF>();float[] distance=Array.Empty<float>();float perimeter;Size size;int radius;
 readonly Dictionary<int,(Bitmap Image,Rectangle Bounds)> frames=new();
 static int sequence;
 public double Phase{get;private set;}=(System.Threading.Interlocked.Increment(ref sequence)*.173)%1;
 public int FrameBuilds{get;private set;}
 public void SetDelay(double seconds){Phase=((1-seconds/10)%1+1)%1;}
 void Build(Control owner){
  if(size==owner.Size&&radius==Design.RadiusCard)return;DisposeFrames();size=owner.Size;radius=Design.RadiusCard;
  using var path=Design.Round(new RectangleF(1,1,Math.Max(2,owner.Width-2),Math.Max(2,owner.Height-2)),Math.Min(radius,Math.Min(owner.Width,owner.Height)/2f-1));path.Flatten(null,.15f);points=path.PathPoints.Concat(new[]{path.PathPoints[0]}).ToArray();distance=new float[points.Length];
  for(int i=1;i<points.Length;i++){float dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;distance[i]=distance[i-1]+MathF.Sqrt(dx*dx+dy*dy);}perimeter=distance[^1];
 }
 public void Advance(double delta,bool hovered){Phase=(Phase+delta/10)%1;}
 public PointF Position(Control owner,double phase){Build(owner);return At(phase);}
 PointF At(double phase){float at=(float)((phase%1+1)%1)*perimeter;int index=Array.FindIndex(distance,value=>value>=at);index=Math.Clamp(index,1,points.Length-1);float t=(at-distance[index-1])/Math.Max(.01f,distance[index]-distance[index-1]);return new(points[index-1].X+(points[index].X-points[index-1].X)*t,points[index-1].Y+(points[index].Y-points[index-1].Y)*t);}
 (Bitmap Image,Rectangle Bounds) Frame(int index){
  if(frames.TryGetValue(index,out var cached))return cached;
  const int samples=56;double phase=index/(double)FrameCount;
  var line=Enumerable.Range(0,samples+1).Select(i=>At(phase-Span/2+i*Span/samples)).ToArray();
  var bounds=Rectangle.FromLTRB((int)Math.Floor(line.Min(p=>p.X))-6,(int)Math.Floor(line.Min(p=>p.Y))-6,(int)Math.Ceiling(line.Max(p=>p.X))+6,(int)Math.Ceiling(line.Max(p=>p.Y))+6);
  var bitmap=new Bitmap(Math.Max(1,bounds.Width),Math.Max(1,bounds.Height));using(var g=Graphics.FromImage(bitmap)){
   g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(-bounds.X,-bounds.Y);
   for(int i=0;i<samples;i++){
    double t=(i+.5)/samples,feather=Math.Pow(Math.Sin(t*Math.PI),3);var color=t<.55?Design.Blend(Color.FromArgb(150,205,255),Color.FromArgb(105,185,255),t/.55):Design.Blend(Color.FromArgb(105,185,255),Color.FromArgb(180,150,255),(t-.55)/.45);
    using var halo=new Pen(Color.FromArgb((int)(6*feather),color),9){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(halo,line[i],line[i+1]);
    using var soft=new Pen(Color.FromArgb((int)(13*feather),color),4){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(soft,line[i],line[i+1]);
    using var light=new Pen(Color.FromArgb((int)(166*feather),color),1.1f){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(light,line[i],line[i+1]);
   }
  }
  FrameBuilds++;return frames[index]=(bitmap,bounds);
 }
 public void Paint(Graphics g,Control owner,double hover){
  if(owner.Width<12||owner.Height<12||!GlassAnimation.MotionAllowed)return;Build(owner);
  var frame=Frame((int)(Phase*FrameCount)%FrameCount);
  if(hover<.001)g.DrawImageUnscaled(frame.Image,frame.Bounds.Location);
  else{using var attributes=new System.Drawing.Imaging.ImageAttributes();attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix{Matrix33=(float)(1+hover*.15)});g.DrawImage(frame.Image,frame.Bounds,0,0,frame.Image.Width,frame.Image.Height,GraphicsUnit.Pixel,attributes);}
 }
 public void Warm(Control owner){Build(owner);if(frames.Count==FrameCount)return;for(int i=0;i<FrameCount;i++)Frame(i);}
 void DisposeFrames(){foreach(var frame in frames.Values)frame.Image.Dispose();frames.Clear();}
 public void Dispose()=>DisposeFrames();
}

