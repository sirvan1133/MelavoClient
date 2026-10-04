using System.Drawing.Drawing2D;
using System.Text.Json;
namespace MelavoClient;
sealed record TextStyle(float Size,bool Muted,FontStyle Weight=FontStyle.Regular);
enum ButtonKind { Primary,Secondary,Ghost,Danger,Icon }
static class Design {
 public static bool Glass;
 public static void PaintBackdrop(Graphics graphics,Control control){
  if(!Glass){graphics.Clear(control.Parent?.BackColor??Canvas);return;}
  var offset=Point.Empty;Control? parent=control;while(parent!=null&&parent is not Card){offset.Offset(parent.Left,parent.Top);parent=parent.Parent;}
  if(parent is Card card&&card.Width>0&&card.Height>0){using var gradient=new LinearGradientBrush(new Rectangle(-offset.X,-offset.Y,card.Width,card.Height),Blend(Surface,Color.White,Dark?.075:.25),Blend(Surface,Accent,Dark?.035:.018),90f);graphics.FillRectangle(gradient,control.ClientRectangle);}else graphics.Clear(Canvas);
 }
 public static int RadiusCard=>Glass?22:12;public static int RadiusInput=>Glass?12:8;
 public static readonly int[] Spacing={4,8,12,16,20,24,32};
 public static int Scale(Control c,int value)=>(int)Math.Round(value*c.DeviceDpi/96d);
 public static Color Blend(Color a,Color b,double t){t=Math.Clamp(t,0,1);return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
 public static bool Persian(string text)=>text.Any(c=>c>=0x600&&c<=0x6ff);
 public static TextFormatFlags TextFlags(string text)=>TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|(Persian(text)?TextFormatFlags.Right|TextFormatFlags.RightToLeft:TextFormatFlags.Left);
 public static Color SurfaceSecondary=>Glass?(Dark?Color.FromArgb(38,48,72):Color.FromArgb(238,242,252)):Dark?Color.FromArgb(31,39,34):Color.FromArgb(244,248,245);
 public static Color BorderHover=>Glass?(Dark?Color.FromArgb(105,128,180):Color.FromArgb(158,180,225)):Dark?Color.FromArgb(76,96,81):Color.FromArgb(165,188,171);
 public static Color Selection=>Blend(Surface,Accent,Dark?.22:.12);
 public static Color OnAccent=>Glass?(Dark?Color.FromArgb(255,255,255):Color.FromArgb(255,255,255)):Dark?Color.FromArgb(9,30,16):Color.White;
 public static bool Dark=true;
 public static Color Canvas=>Glass?(Dark?Color.FromArgb(13,18,32):Color.FromArgb(230,237,250)):Dark?Color.FromArgb(15,19,17):Color.FromArgb(243,247,244);
 public static Color Surface=>Glass?(Dark?Color.FromArgb(27,35,54):Color.FromArgb(250,252,255)):Dark?Color.FromArgb(23,29,25):Color.White;
 public static Color Border=>Glass?(Dark?Color.FromArgb(62,76,104):Color.FromArgb(205,218,238)):Dark?Color.FromArgb(42,52,45):Color.FromArgb(218,228,220);
 public static Color Text=>Glass?(Dark?Color.FromArgb(242,246,255):Color.FromArgb(28,39,63)):Dark?Color.FromArgb(235,241,236):Color.FromArgb(25,37,29);
 public static Color Muted=>Glass?(Dark?Color.FromArgb(166,181,208):Color.FromArgb(98,115,142)):Dark?Color.FromArgb(157,173,161):Color.FromArgb(91,109,96);
 public static Color Accent=>Glass?(Dark?Color.FromArgb(106,161,255):Color.FromArgb(39,111,239)):Dark?Color.FromArgb(72,211,119):Color.FromArgb(20,128,63);
 public static Color Warning=>Dark?Color.FromArgb(235,181,95):Color.FromArgb(167,100,15);
 public static Color Error=>Dark?Color.FromArgb(239,119,125):Color.FromArgb(195,47,61);
 public static Font Font(float size=10,FontStyle style=FontStyle.Regular)=>new("Segoe UI",size,style);
 public static GraphicsPath Round(RectangleF r,float radius=14){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static Label Label(string text,float size=10,bool muted=false)=>new StableLabel(){Text=text,Tag=new TextStyle(size,muted,size>=12&&!muted?FontStyle.Bold:FontStyle.Regular),AutoSize=true,Font=Font(size,size>=12&&!muted?FontStyle.Bold:FontStyle.Regular),ForeColor=muted?Muted:Text,BackColor=Color.Transparent,Margin=new(0,0,0,8),RightToLeft=Persian(text)?RightToLeft.Yes:RightToLeft.No,TextAlign=ContentAlignment.MiddleLeft};
}
class Card:Panel {
 public Card(){DoubleBuffered=true;Padding=new(20);BackColor=Design.Surface;Margin=new(0,0,0,16);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor is Color background&&background.A==255?background:Design.Canvas);if(Width<2||Height<2)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusCard);if(Design.Glass){using var fill=new LinearGradientBrush(ClientRectangle,Design.Blend(Design.Surface,Color.White,Design.Dark?.075:.25),Design.Blend(Design.Surface,Design.Accent,Design.Dark?.035:.018),90f);e.Graphics.FillPath(fill,path);}else{using var fill=new SolidBrush(Design.Surface);e.Graphics.FillPath(fill,path);}}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using var path=Design.Round(new RectangleF(0,0,Width,Height),Math.Min(Design.RadiusCard,Math.Min(Width,Height)/2f));var old=Region;Region=new Region(path);old?.Dispose();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var p=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusCard);using var pen=new Pen(Design.Glass?Design.Blend(Design.Border,Color.White,Design.Dark?.13:.5):Design.Border);e.Graphics.DrawPath(pen,p);}
}
class ModernButton:Button {
 bool pressed;readonly Motion interaction;double emphasis;public ButtonKind Kind{get;set;}=ButtonKind.Secondary;public bool Primary{get=>Kind==ButtonKind.Primary;set{if(value)Kind=ButtonKind.Primary;else if(Kind==ButtonKind.Primary)Kind=ButtonKind.Secondary;}}public bool Selected{get;set;}public string? IconGlyph{get;set;}
 public ModernButton(){interaction=new Motion(this,v=>{emphasis=v;Invalidate();});FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Height=42;AutoSize=false;Cursor=Cursors.Hand;Font=Design.Font();Margin=new(0,0,10,0);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnMouseEnter(EventArgs e){interaction.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){interaction.To(0);base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){Design.PaintBackdrop(e.Graphics,this);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var fill=Kind==ButtonKind.Danger?Design.Error:Primary?Design.Accent:Selected?Design.Selection:Design.Blend(Kind==ButtonKind.Ghost&&!Design.Glass?Parent?.BackColor??Design.Canvas:Design.Surface,Design.SurfaceSecondary,emphasis);if(pressed)fill=Design.Blend(fill,Design.Text,.08);if(!Enabled)fill=Design.SurfaceSecondary;using var path=Design.Round(new RectangleF(1,1,Width-2,Height-2),Design.Glass?Math.Min(Height/2f-1,22):10);using var b=new SolidBrush(fill);e.Graphics.FillPath(b,path);if(Design.Glass){using var shine=new LinearGradientBrush(ClientRectangle,Color.FromArgb(Primary?38:18,Color.White),Color.FromArgb(0,Color.White),90f);e.Graphics.FillPath(shine,path);using var rim=new Pen(Color.FromArgb(Design.Dark?36:160,Color.White));e.Graphics.DrawPath(rim,path);}if(Kind is ButtonKind.Secondary or ButtonKind.Icon){using var outline=new Pen(Design.Blend(Design.Border,Design.BorderHover,emphasis));e.Graphics.DrawPath(outline,path);}if(Selected&&!Design.Glass){using var activePen=new Pen(Design.Accent,3);int marker=RightToLeft==RightToLeft.Yes?Width-4:4;e.Graphics.DrawLine(activePen,marker,12,marker,Height-12);}if(Focused){using var pen=new Pen(Design.Accent,2);e.Graphics.DrawPath(pen,path);}var textRect=ClientRectangle;if(IconGlyph!=null){using var font=new Font("Segoe MDL2 Assets",12);TextRenderer.DrawText(e.Graphics,IconGlyph,font,new Rectangle(RightToLeft==RightToLeft.Yes?Width-40:12,0,28,Height),Selected?Design.Accent:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);textRect.X=RightToLeft==RightToLeft.Yes?8:44;textRect.Width-=48;}TextRenderer.DrawText(e.Graphics,Text,Font,textRect,!Enabled?Design.Muted:Primary||Kind==ButtonKind.Danger?Design.OnAccent:Design.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|(Design.Persian(Text)?TextFormatFlags.RightToLeft:0));}
}
enum ConnectionState{Disconnected,Connecting,Connected,Disconnecting,Error}
sealed class QuietProgress:Control {
 int value;public int Maximum{get;set;}=1000;
 public int Value{get=>value;set{this.value=Math.Clamp(value,0,Maximum);AccessibleDescription=$"{this.value*100d/Maximum:0}%";Invalidate();}}
 public QuietProgress(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);AccessibleRole=AccessibleRole.ProgressBar;}
 public Color FillColor{get{double ratio=value/(double)Math.Max(1,Maximum);return ratio<=.75?Design.Blend(Design.Accent,Design.Warning,ratio/.75):Design.Blend(Design.Warning,Design.Error,(ratio-.75)/.25);}}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(Width<2||Height<2)return;using var track=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),(Height-1)/2f);using var background=new SolidBrush(Design.Border);e.Graphics.FillPath(background,track);if(value>0){var state=e.Graphics.Save();e.Graphics.SetClip(track);using var fill=new SolidBrush(FillColor);float usedWidth=Width*value/(float)Math.Max(1,Maximum);e.Graphics.FillRectangle(fill,RightToLeft==RightToLeft.Yes?Width-usedWidth:0,0,usedWidth,Height);e.Graphics.Restore(state);}}
}
sealed partial class PowerControl:Button {
 bool hover,pressed;
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 public ConnectionState State{get;set;}public float Phase{get;set;}
 public PowerControl(){Size=new(170,170);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;AccessibleName="اتصال VPN";SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnPaint(PaintEventArgs e){if(Design.Glass){PaintGlass(e.Graphics);return;}var g=e.Graphics;Design.PaintBackdrop(g,this);g.SmoothingMode=SmoothingMode.AntiAlias;float unit=Math.Min(Width,Height),scale=unit/170f,cx=Width/2f,cy=Height/2f;var color=State==ConnectionState.Error?Design.Error:State==ConnectionState.Disconnected?Design.Muted:Design.Accent;float pulse=(State==ConnectionState.Connecting?(float)(Math.Sin(Phase)*2+3):2)*scale;float outerInset=10*scale,innerInset=17*scale;using var outer=new Pen(Color.FromArgb(45,color),7*scale);g.DrawEllipse(outer,cx-unit*.5f+outerInset-pulse,cy-unit*.5f+outerInset-pulse,unit-2*outerInset+2*pulse,unit-2*outerInset+2*pulse);using var fill=new SolidBrush(pressed?Design.Selection:hover?Design.SurfaceSecondary:Design.Surface);g.FillEllipse(fill,cx-unit*.5f+innerInset,cy-unit*.5f+innerInset,unit-2*innerInset,unit-2*innerInset);using var outline=new Pen(Design.Border,2*scale);g.DrawEllipse(outline,cx-unit*.5f+innerInset,cy-unit*.5f+innerInset,unit-2*innerInset,unit-2*innerInset);using var pen=new Pen(color,Math.Max(2.6f,4*scale)){StartCap=LineCap.Round,EndCap=LineCap.Round};float glyph=58*scale;g.DrawArc(pen,cx-glyph/2,cy-glyph*.40f,glyph,glyph,-45,270);g.DrawLine(pen,cx,cy-glyph*.60f,cx,cy-glyph*.06f);if(State is ConnectionState.Connecting or ConnectionState.Disconnecting){using var progress=new Pen(color,3*scale);g.DrawArc(progress,cx-unit*.5f+8*scale,cy-unit*.5f+8*scale,unit-16*scale,unit-16*scale,Phase*45,85);}if(Focused){using var focus=new Pen(color,2*scale);g.DrawEllipse(focus,cx-unit*.5f+3*scale,cy-unit*.5f+3*scale,unit-6*scale,unit-6*scale);}}
}
sealed class UiPreferences {
 public bool AutoAppUpdate{get;set;}=true;
 public string VisualStyle{get;set;}="classic";
 public string Language{get;set;}="en";public bool Dark{get;set;}=true;public bool Tray{get;set;}=true;public bool Notifications{get;set;}=false;
 public HashSet<string> Favorites{get;set;}=new();public List<string> Recent{get;set;}=new();
 static string PathName=>Path.Combine(SubscriptionStore.DirectoryPath,"appearance.json");
 public static UiPreferences Read(){try{return File.Exists(PathName)?JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(PathName))??new():new();}catch{return new();}}
 public void Save(){Directory.CreateDirectory(SubscriptionStore.DirectoryPath);var tmp=PathName+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(this));File.Move(tmp,PathName,true);}
}

