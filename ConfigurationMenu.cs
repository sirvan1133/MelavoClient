using System.Drawing.Drawing2D;
namespace MelavoClient;
sealed class ConfigurationMenu:ToolStripDropDown {
 readonly Card card=new(){Padding=new(10),Margin=Padding.Empty};
 readonly Label heading=Design.Label("",9,true);
 readonly List<MenuAction> actions=new();
 readonly Motion motion;bool closing,finished;ToolStripDropDownCloseReason closeReason;Action? pending;
 public int ActionCount=>actions.Count;
 public ConfigurationMenu(){
  AutoSize=false;Padding=Margin=Padding.Empty;ShowItemToolTips=false;DoubleBuffered=true;
  var host=new ToolStripControlHost(card){AutoSize=false,Padding=Padding.Empty,Margin=Padding.Empty};Items.Add(host);
  motion=new Motion(this,v=>{Opacity=Math.Clamp(v,0,1);if(closing&&v<.001){finished=true;Close(closeReason);}});
  Opened+=(_,_)=>{closing=finished=false;pending=null;Opacity=0;motion.To(1);actions.FirstOrDefault(a=>a.Enabled)?.Focus();};
  Closing+=(_,e)=>{if(finished)return;e.Cancel=true;if(closing)return;closing=true;closeReason=e.CloseReason;motion.To(0);};
  Closed+=(_,_)=>{var action=pending;pending=null;if(action!=null&&!IsDisposed)BeginInvoke(action);};
 }
 public void AddAction(string glyph,Action action,bool danger=false){var row=new MenuAction(glyph,danger);row.Click+=(_,_)=>{if(closing)return;pending=action;Close(ToolStripDropDownCloseReason.ItemClicked);};actions.Add(row);card.Controls.Add(row);}
 public void Prepare(string title,bool rtl,string[] labels,bool[] enabled){
  BackColor=card.BackColor=Design.Surface;RightToLeft=rtl?RightToLeft.Yes:RightToLeft.No;
  int width=Design.Scale(this,286),rowHeight=Design.Scale(this,44),gap=Design.Scale(this,4),inset=Design.Scale(this,10),header=Design.Scale(this,42);
  Size=new(width,inset*2+header+actions.Count*(rowHeight+gap));card.Size=Size;((ToolStripControlHost)Items[0]).Size=Size;
  heading.Text=title;heading.AutoSize=false;heading.AutoEllipsis=true;heading.ForeColor=Design.Muted;heading.BackColor=Design.Surface;heading.Font=Design.Font(9);heading.Bounds=new(inset+8,inset,width-2*inset-16,header-8);heading.RightToLeft=RightToLeft;heading.TextAlign=rtl?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft;if(heading.Parent==null)card.Controls.Add(heading);
  for(int i=0;i<actions.Count;i++){var row=actions[i];row.Text=labels[i];row.Enabled=enabled[i];row.RightToLeft=RightToLeft;row.Bounds=new(inset,inset+header+i*(rowHeight+gap),width-2*inset,rowHeight);row.AccessibleName=labels[i];row.TabIndex=i;}
  using var path=Design.Round(new Rectangle(0,0,Width,Height),14);var old=Region;Region=new Region(path);old?.Dispose();
 }
 protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ClassStyle|=0x20000;return cp;}}
 protected override bool ProcessCmdKey(ref Message m,Keys key){
  if(key is Keys.Down or Keys.Up){int current=actions.FindIndex(a=>a.Focused),direction=key==Keys.Down?1:-1;for(int step=1;step<=actions.Count;step++){int next=(current+direction*step+actions.Count*2)%actions.Count;if(actions[next].Enabled){actions[next].Focus();break;}}return true;}
  if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref m,key);
 }
 sealed class MenuAction:Button {
  readonly string glyph;readonly bool danger;readonly Motion hover;double emphasis;bool pressed;
  public MenuAction(string icon,bool destructive){glyph=icon;danger=destructive;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=Design.Font(10);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);hover=new Motion(this,v=>{emphasis=v;Invalidate();});}
  protected override void OnMouseEnter(EventArgs e){hover.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover.To(Focused?1:0);base.OnMouseLeave(e);}
  protected override void OnGotFocus(EventArgs e){hover.To(1);base.OnGotFocus(e);}protected override void OnLostFocus(EventArgs e){hover.To(0);base.OnLostFocus(e);}
  protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.Clear(Design.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;bool rtl=RightToLeft==RightToLeft.Yes;var accent=danger?Design.Error:Design.Accent;
   using var shape=Design.Round(new RectangleF(0,0,Width-1,Height-1),9);using var fill=new SolidBrush(Design.Blend(Design.Surface,danger?Design.Blend(Design.Surface,Design.Error,.16):Design.Selection,pressed?1:emphasis));g.FillPath(fill,shape);
   var color=!Enabled?Design.Muted:danger?Design.Error:Design.Text;int iconWidth=Design.Scale(this,40);using var iconFont=new Font("Segoe MDL2 Assets",12);TextRenderer.DrawText(g,glyph,iconFont,new Rectangle(rtl?Width-iconWidth:0,0,iconWidth,Height),Enabled?Design.Blend(Design.Muted,accent,emphasis):Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
   var textBounds=new Rectangle(rtl?12:iconWidth,0,Width-iconWidth-12,Height);TextRenderer.DrawText(g,Text,Font,textBounds,color,TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis|(rtl?TextFormatFlags.Right|TextFormatFlags.RightToLeft:TextFormatFlags.Left));
  }
 }
}
