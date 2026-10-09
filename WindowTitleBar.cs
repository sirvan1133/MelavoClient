using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace MelavoClient;
sealed class WindowTitleBar:Control {
 readonly Form window;readonly CaptionButton minimize=new("\uE921"),maximize=new("\uE922"),close=new("\uE8BB",true);
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
 public WindowTitleBar(Form form){window=form;DoubleBuffered=true;Dock=DockStyle.Top;Height=48;TabStop=false;Controls.AddRange(new Control[]{minimize,maximize,close});minimize.Click+=(_,_)=>window.WindowState=FormWindowState.Minimized;maximize.Click+=(_,_)=>ToggleMaximize();close.Click+=(_,_)=>window.Close();window.Resize+=(_,_)=>{maximize.Glyph=window.WindowState==FormWindowState.Maximized?"\uE923":"\uE922";maximize.Invalidate();};Configure(false);}
 public void Configure(bool persian){minimize.AccessibleName=persian?"کوچک‌کردن":"Minimize";maximize.AccessibleName=persian?"بزرگ‌کردن":"Maximize";close.AccessibleName=persian?"بستن":"Close";Invalidate();}
 void ToggleMaximize(){if(window is Client client){client.ToggleMaximizeWindow();return;}window.WindowState=window.WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<4||Height<4)return;using var shape=Design.Round(new RectangleF(0,0,Width,Height),16);var old=Region;Region=new Region(shape);old?.Dispose();}
 protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);int width=44,height=30,top=Math.Max(3,(Height-height)/2);close.Bounds=new(Width-width-12,top,width,height);maximize.Bounds=new(close.Left-width-4,top,width,height);minimize.Bounds=new(maximize.Left-width-4,top,width,height);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){if(e.Clicks==2){ToggleMaximize();return;}ReleaseCapture();SendMessage(window.Handle,0xA1,(IntPtr)2,IntPtr.Zero);}}
 protected override void OnPaint(PaintEventArgs e){if(Design.Glass)GlassMaterial.Title(e.Graphics,this);else e.Graphics.Clear(Design.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;MelavoBrand.Mark(e.Graphics,new RectangleF(16,(Height-24)/2f,24,24));using var font=Design.Font(10,FontStyle.Bold);TextRenderer.DrawText(e.Graphics,"Melavo VPN",font,new Rectangle(52,0,130,Height),Design.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);using var small=Design.Font(8);using var pill=Design.Round(new RectangleF(182,11,64,22),11);using var wash=new SolidBrush(Color.FromArgb(20,Color.White));e.Graphics.FillPath(wash,pill);TextRenderer.DrawText(e.Graphics,"v"+UpdateService.AppVersion,small,new Rectangle(182,11,64,22),Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
 sealed class CaptionButton:Button {
  public string Glyph;readonly bool destructive;readonly Motion motion;double emphasis;
  public CaptionButton(string glyph,bool danger=false){Glyph=glyph;destructive=danger;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);motion=new Motion(this,value=>{emphasis=value;Invalidate();});}
  protected override void OnMouseEnter(EventArgs e){motion.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){motion.To(0);base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){if(Design.Glass)GlassMaterial.Title(e.Graphics,this,false);else e.Graphics.Clear(Design.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(emphasis>.005){using var shape=Design.Round(new RectangleF(0,0,Width-1,Height-1),10);using var fill=new SolidBrush(Color.FromArgb((int)(emphasis*32),destructive?Design.Error:Design.Accent));e.Graphics.FillPath(fill,shape);}using var font=new Font("Segoe MDL2 Assets",10);TextRenderer.DrawText(e.Graphics,Glyph,font,ClientRectangle,destructive&&emphasis>.1?Design.Error:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(Focused){using var shape=Design.Round(new RectangleF(1,1,Width-3,Height-3),9);using var outline=new Pen(Design.Accent);e.Graphics.DrawPath(outline,shape);}}
 }
}
sealed partial class Client {
 WindowTitleBar? titleBar;
 internal void ToggleMaximizeWindow(){MaximizedBounds=Screen.FromControl(this).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
 protected override void OnPaintBackground(PaintEventArgs e){if(Design.Glass)GlassMaterial.Title(e.Graphics,this,false);else base.OnPaintBackground(e);}
 protected override void WndProc(ref Message message){
  if(message.Msg==0x84&&FormBorderStyle==FormBorderStyle.None&&WindowState!=FormWindowState.Maximized){var packed=message.LParam.ToInt64();var point=PointToClient(new Point((short)(packed&0xffff),(short)((packed>>16)&0xffff)));int edge=Design.Scale(this,6);bool left=point.X<edge,right=point.X>=Width-edge,top=point.Y<edge,bottom=point.Y>=Height-edge;int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;if(hit!=0){message.Result=(IntPtr)hit;return;}}
  base.WndProc(ref message);
 }
}


