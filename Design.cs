using System.Drawing.Drawing2D;
using System.Text.Json;
namespace MelavoClient;
sealed record TextStyle(float Size,bool Muted,FontStyle Weight=FontStyle.Regular);
enum ButtonKind { Primary,Secondary,Ghost,Danger,Icon }
static class Design {
 public static bool Glass;
 public static void PaintBackdrop(Graphics graphics,Control control){if(Glass)GlassMaterial.Backdrop(graphics,control);else graphics.Clear(control.Parent?.BackColor is Color parent&&parent.A==255?parent:Canvas);}
 public static int RadiusCard=>Glass?22:12;public static int RadiusInput=>Glass?16:8;
 public static readonly int[] Spacing={4,8,12,16,20,24,32};
 public static int Scale(Control c,int value)=>(int)Math.Round(value*c.DeviceDpi/96d);
 public static Color Blend(Color a,Color b,double t){t=Math.Clamp(t,0,1);return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
 public static bool Persian(string text)=>text.Any(c=>c>=0x600&&c<=0x6ff);
 public static TextFormatFlags TextFlags(string text)=>TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|(Persian(text)?TextFormatFlags.Right|TextFormatFlags.RightToLeft:TextFormatFlags.Left);
 public static Color SurfaceSecondary=>Glass?(Dark?Color.FromArgb(24,43,70):Color.FromArgb(238,242,252)):Dark?Color.FromArgb(31,39,34):Color.FromArgb(244,248,245);
 public static Color BorderHover=>Glass?(Dark?Color.FromArgb(105,128,180):Color.FromArgb(158,180,225)):Dark?Color.FromArgb(76,96,81):Color.FromArgb(165,188,171);
 public static Color Selection=>Blend(Surface,Accent,Dark?.22:.12);
 public static Color OnAccent=>Glass?(Dark?Color.FromArgb(255,255,255):Color.FromArgb(255,255,255)):Dark?Color.FromArgb(9,30,16):Color.White;
 public static bool Dark=true;
 public static Color Canvas=>Glass?(Dark?Color.FromArgb(8,14,29):Color.FromArgb(230,237,250)):Dark?Color.FromArgb(15,19,17):Color.FromArgb(243,247,244);
 public static Color Surface=>Glass?(Dark?Color.FromArgb(17,29,49):Color.FromArgb(250,252,255)):Dark?Color.FromArgb(23,29,25):Color.White;
 public static Color Border=>Glass?(Dark?Color.FromArgb(74,99,153):Color.FromArgb(205,218,238)):Dark?Color.FromArgb(42,52,45):Color.FromArgb(218,228,220);
 public static Color Text=>Glass?(Dark?Color.FromArgb(242,246,255):Color.FromArgb(28,39,63)):Dark?Color.FromArgb(235,241,236):Color.FromArgb(25,37,29);
 public static Color Muted=>Glass?(Dark?Color.FromArgb(151,177,207):Color.FromArgb(98,115,142)):Dark?Color.FromArgb(157,173,161):Color.FromArgb(91,109,96);
 public static Color Accent=>Glass?(Dark?Color.FromArgb(76,148,248):Color.FromArgb(39,111,239)):Dark?Color.FromArgb(72,211,119):Color.FromArgb(20,128,63);
 public static Color Warning=>Dark?Color.FromArgb(235,181,95):Color.FromArgb(167,100,15);
 public static Color Error=>Dark?Color.FromArgb(239,119,125):Color.FromArgb(195,47,61);
 static readonly string FontFamilyName=new System.Drawing.Text.InstalledFontCollection().Families.Any(f=>f.Name=="Segoe UI Variable Text")?"Segoe UI Variable Text":"Segoe UI";
 public static Font Font(float size=10,FontStyle style=FontStyle.Regular)=>new(FontFamilyName,size,style);
 public static Font FitFont(string text,Font font,Size available){float size=font.Size;while(size>5.5f){using var candidate=new Font(font.FontFamily,size,font.Style);var measure=TextRenderer.MeasureText(text,candidate,Size.Empty,TextFormatFlags.NoPadding);if(measure.Width<=available.Width-12&&measure.Height<=available.Height-8)break;size-=.25f;}return new Font(font.FontFamily,size,font.Style);}
 public static GraphicsPath Round(RectangleF r,float radius=14){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static Label Label(string text,float size=10,bool muted=false)=>new StableLabel(){Text=text,Tag=new TextStyle(size,muted,size>=12&&!muted?FontStyle.Bold:FontStyle.Regular),AutoSize=true,Font=Font(size,size>=12&&!muted?FontStyle.Bold:FontStyle.Regular),ForeColor=muted?Muted:Text,BackColor=Color.Transparent,Margin=new(0,0,0,8),RightToLeft=Persian(text)?RightToLeft.Yes:RightToLeft.No,TextAlign=ContentAlignment.MiddleLeft};
}
class Card:Panel {
 readonly GlassEdge edge=new();GlassBorderOverlay? edgeOverlay;readonly Motion hoverMotion;double hover;bool hovering;
 public double ReflectionPhase=>edge.Phase;public int ReflectionFrameBuilds=>edge.FrameBuilds;protected override void Dispose(bool disposing){if(disposing){edge.Dispose();edgeOverlay?.Dispose();}base.Dispose(disposing);}
 public Card(){DoubleBuffered=true;Padding=new(20);BackColor=Design.Surface;Margin=new(0,0,0,16);hoverMotion=new Motion(this,value=>{hover=value;InvalidateEdges();},200);edgeOverlay=new GlassBorderOverlay(this,edge,()=>hover);if(!GlassAnimation.Capture)Controls.Add(edgeOverlay);ControlAdded+=(_,e)=>{Observe(e.Control!);edgeOverlay.BringToFront();};GlassAnimation.Register(this);}
 void Observe(Control control){control.MouseEnter+=(_,_)=>SetHover(true);control.MouseLeave+=(_,_)=>{if(!ClientRectangle.Contains(PointToClient(Cursor.Position)))SetHover(false);};control.ControlAdded+=(_,e)=>Observe(e.Control!);foreach(Control child in control.Controls)Observe(child);}
 void SetHover(bool value){if(hovering==value)return;hovering=value;hoverMotion.To(value?1:0);}
 protected override void OnMouseEnter(EventArgs e){SetHover(true);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){if(!ClientRectangle.Contains(PointToClient(Cursor.Position)))SetHover(false);base.OnMouseLeave(e);}
 internal void SyncMaterial(){edgeOverlay?.ResetMaterial();InvalidateEdges();Invalidate(true);}
 public double ReflectionDelay{set=>edge.SetDelay(value);}internal void AnimateEdge(double delta){edge.Advance(delta,false);InvalidateEdges();}
 void InvalidateEdges(){if(edgeOverlay==null||!IsHandleCreated||!Visible)return;if(GlassAnimation.Capture){if(edgeOverlay.Parent==this)Controls.Remove(edgeOverlay);return;}if(edgeOverlay.Parent==null&&Design.Glass){Controls.Add(edgeOverlay);edgeOverlay.Fit();}edgeOverlay.Visible=Design.Glass;if(edgeOverlay.Visible)edgeOverlay.Invalidate();}
 protected override void OnPaintBackground(PaintEventArgs e){if(Design.Glass){if(!GlassAnimation.Capture&&GlassAnimation.MotionAllowed)edge.Warm(this);GlassMaterial.PaintCard(e.Graphics,this);return;}e.Graphics.Clear(Parent?.BackColor is Color background&&background.A==255?background:Design.Canvas);if(Width<2||Height<2)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusCard);using var fill=new SolidBrush(Design.Surface);e.Graphics.FillPath(fill,path);}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using var path=Design.Round(new RectangleF(0,0,Width,Height),Math.Min(Design.RadiusCard,Math.Min(Width,Height)/2f));var old=Region;Region=new Region(path);old?.Dispose();edgeOverlay?.Fit();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<4||Height<4)return;if(Design.Glass){var state=e.Graphics.Save();using var clip=Design.Round(new RectangleF(0,0,Width,Height),Design.RadiusCard);e.Graphics.SetClip(clip,CombineMode.Intersect);if(GlassAnimation.Capture||edgeOverlay?.Visible!=true){GlassBorderOverlay.BaseBorder(e.Graphics,this,hover);edge.Paint(e.Graphics,this,hover);}e.Graphics.Restore(state);}else{using var path=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusCard);using var pen=new Pen(Design.Border);e.Graphics.DrawPath(pen,path);}}
}
static class GlassAnimation {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SystemParametersInfo(uint action,uint parameter,out int value,uint flags);public static bool? ReducedMotionOverride;static long policyChecked=-2000;static bool motionAllowed=true;public static bool MotionAllowed{get{if(ReducedMotionOverride.HasValue)return !ReducedMotionOverride.Value;long now=Environment.TickCount64;if(now-policyChecked>=1000){policyChecked=now;motionAllowed=!SystemParametersInfo(0x1042,0,out var enabled,0)||enabled!=0;}return motionAllowed;}}
 public static bool Capture;
 static readonly List<WeakReference<Card>> cards=new();static readonly System.Windows.Forms.Timer timer=new(){Interval=33};static readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();static double previous;
 static GlassAnimation(){timer.Tick+=(_,_)=>{double now=clock.Elapsed.TotalSeconds,delta=Math.Min(.1,now-previous);previous=now;if(!Design.Glass||Capture||!MotionAllowed)return;for(int i=cards.Count-1;i>=0;i--){if(!cards[i].TryGetTarget(out var card)||card.IsDisposed){cards.RemoveAt(i);continue;}var form=card.FindForm();if(card.IsHandleCreated&&card.Visible&&(form==null||form.Visible&&form.WindowState!=FormWindowState.Minimized))card.AnimateEdge(delta);}};timer.Start();}
 public static void Register(Card card)=>cards.Add(new(card));
}
class ModernButton:Button {
 bool pressed;readonly Motion interaction;double emphasis;public ButtonKind Kind{get;set;}=ButtonKind.Secondary;public bool Primary{get=>Kind==ButtonKind.Primary;set{if(value)Kind=ButtonKind.Primary;else if(Kind==ButtonKind.Primary)Kind=ButtonKind.Secondary;}}public bool Selected{get;set;}public string? IconGlyph{get;set;}
 public ModernButton(){interaction=new Motion(this,v=>{emphasis=v;Invalidate();});FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Height=44;AutoSize=false;Cursor=Cursors.Hand;Font=Design.Font();Margin=new(0,0,10,0);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnMouseEnter(EventArgs e){interaction.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){interaction.To(0);base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){Design.PaintBackdrop(e.Graphics,this);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var fill=Kind==ButtonKind.Danger?Design.Error:Primary?Design.Accent:Selected?Design.Selection:Design.Blend(Kind==ButtonKind.Ghost&&!Design.Glass?Parent?.BackColor??Design.Canvas:Design.Surface,Design.SurfaceSecondary,emphasis);if(Design.Glass&&!Primary&&!Selected&&Kind!=ButtonKind.Danger)fill=Color.FromArgb(85,Design.Blend(Design.Surface,Design.Accent,.12+emphasis*.14));if(pressed)fill=Design.Blend(fill,Design.Text,.08);if(!Enabled)fill=Design.SurfaceSecondary;using var path=Design.Round(new RectangleF(1,1,Width-2,Height-2),Design.Glass?Kind==ButtonKind.Ghost?16:Math.Min(Height/2f-1,16):10);using var b=new SolidBrush(fill);e.Graphics.FillPath(b,path);if(Design.Glass&&Enabled&&(Primary||Selected)){using var primaryGradient=new LinearGradientBrush(ClientRectangle,Design.Accent,Design.Blend(Design.Accent,Color.FromArgb(83,74,212),.75),25f);e.Graphics.FillPath(primaryGradient,path);}if(Design.Glass){using var shine=new LinearGradientBrush(ClientRectangle,Color.FromArgb(Primary||Selected?48:20,Color.White),Color.FromArgb(0,Color.White),90f);e.Graphics.FillPath(shine,path);}if(!Design.Glass&&Kind is ButtonKind.Secondary or ButtonKind.Icon){using var outline=new Pen(Design.Blend(Design.Border,Design.BorderHover,emphasis));e.Graphics.DrawPath(outline,path);}if(Selected&&!Design.Glass){using var activePen=new Pen(Design.Accent,3);int marker=RightToLeft==RightToLeft.Yes?Width-4:4;e.Graphics.DrawLine(activePen,marker,12,marker,Height-12);}var textRect=ClientRectangle;if(IconGlyph!=null){using var font=new Font("Segoe MDL2 Assets",10);TextRenderer.DrawText(e.Graphics,IconGlyph,font,new Rectangle(RightToLeft==RightToLeft.Yes?Width-32:8,0,24,Height),Selected||Primary?Color.White:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);textRect.X=RightToLeft==RightToLeft.Yes?4:34;textRect.Width-=38;}using var fitted=Design.FitFont(Text,Font,textRect.Size);TextRenderer.DrawText(e.Graphics,Text,fitted,textRect,!Enabled?Design.Muted:Primary||Kind==ButtonKind.Danger?Design.OnAccent:Design.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|(Design.Persian(Text)?TextFormatFlags.RightToLeft:0));}
}
enum ConnectionState{Disconnected,Connecting,Connected,Disconnecting,Error}
sealed class QuietProgress:Control {
 int value;public int Maximum{get;set;}=1000;
 public int Value{get=>value;set{this.value=Math.Clamp(value,0,Maximum);AccessibleDescription=$"{this.value*100d/Maximum:0}%";Invalidate();}}
 public QuietProgress(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);AccessibleRole=AccessibleRole.ProgressBar;}
 public Color FillColor{get{double ratio=value/(double)Math.Max(1,Maximum);return ratio<=.75?Design.Blend(Design.Accent,Design.Warning,ratio/.75):Design.Blend(Design.Warning,Design.Error,(ratio-.75)/.25);}}
 protected override void OnPaintBackground(PaintEventArgs e){Design.PaintBackdrop(e.Graphics,this);}protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(Width<2||Height<2)return;using var track=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),(Height-1)/2f);using var background=new SolidBrush(Design.Border);e.Graphics.FillPath(background,track);if(value>0){using var fill=new SolidBrush(FillColor);float usedWidth=Math.Max(1,(Width-1)*value/(float)Math.Max(1,Maximum));using var segment=Design.Round(new RectangleF(RightToLeft==RightToLeft.Yes?Width-usedWidth-.5f:.5f,.5f,usedWidth,Height-1),Math.Min((Height-1)/2f,usedWidth/2));e.Graphics.FillPath(fill,segment);}}
}
sealed partial class PowerControl:Button {
 bool hover,pressed;readonly Motion glassMotion;double glassEmphasis;
 protected override void OnMouseEnter(EventArgs e){hover=true;glassMotion.To(1);Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;glassMotion.To(0);Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 public ConnectionState State{get;set;}public float Phase{get;set;}
 public PowerControl(){glassMotion=new Motion(this,v=>{glassEmphasis=v;Invalidate();});Size=new(170,170);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;AccessibleName="اتصال VPN";SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnPaint(PaintEventArgs e){if(Design.Glass){PaintGlass(e.Graphics);return;}var g=e.Graphics;Design.PaintBackdrop(g,this);g.SmoothingMode=SmoothingMode.AntiAlias;float unit=Math.Min(Width,Height),scale=unit/170f,cx=Width/2f,cy=Height/2f;var color=State==ConnectionState.Error?Design.Error:State==ConnectionState.Disconnected?Design.Muted:Design.Accent;float pulse=(State==ConnectionState.Connecting?(float)(Math.Sin(Phase)*2+3):2)*scale;float outerInset=10*scale,innerInset=17*scale;using var outer=new Pen(Color.FromArgb(45,color),7*scale);g.DrawEllipse(outer,cx-unit*.5f+outerInset-pulse,cy-unit*.5f+outerInset-pulse,unit-2*outerInset+2*pulse,unit-2*outerInset+2*pulse);using var fill=new SolidBrush(pressed?Design.Selection:hover?Design.SurfaceSecondary:Design.Surface);g.FillEllipse(fill,cx-unit*.5f+innerInset,cy-unit*.5f+innerInset,unit-2*innerInset,unit-2*innerInset);using var outline=new Pen(Design.Border,2*scale);g.DrawEllipse(outline,cx-unit*.5f+innerInset,cy-unit*.5f+innerInset,unit-2*innerInset,unit-2*innerInset);using var pen=new Pen(color,Math.Max(2.6f,4*scale)){StartCap=LineCap.Round,EndCap=LineCap.Round};float glyph=58*scale;g.DrawArc(pen,cx-glyph/2,cy-glyph*.40f,glyph,glyph,-45,270);g.DrawLine(pen,cx,cy-glyph*.60f,cx,cy-glyph*.06f);if(State is ConnectionState.Connecting or ConnectionState.Disconnecting){using var progress=new Pen(color,3*scale);g.DrawArc(progress,cx-unit*.5f+8*scale,cy-unit*.5f+8*scale,unit-16*scale,unit-16*scale,Phase*45,85);}if(Focused){using var focus=new Pen(color,2*scale);g.DrawEllipse(focus,cx-unit*.5f+3*scale,cy-unit*.5f+3*scale,unit-6*scale,unit-6*scale);}}
}
sealed class UiPreferences {
 public bool AutoAppUpdate{get;set;}=true;
 public string VisualStyle{get;set;}="glass";
 public string Language{get;set;}="en";public bool Dark{get;set;}=true;public bool Tray{get;set;}=true;public bool Notifications{get;set;}=false;
 public HashSet<string> Favorites{get;set;}=new();public List<string> Recent{get;set;}=new();
 static string PathName=>Path.Combine(SubscriptionStore.DirectoryPath,"appearance.json");
 public static UiPreferences Read(){try{return File.Exists(PathName)?JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(PathName))??new():new();}catch{return new();}}
 public void Save(){Directory.CreateDirectory(SubscriptionStore.DirectoryPath);var tmp=PathName+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(this));File.Move(tmp,PathName,true);}
}












