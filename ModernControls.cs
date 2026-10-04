using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace MelavoClient;
// Rendering primitives are shared by input, menus, lists and page scrollbars.
sealed class SlimScrollBar:Control {
 int value,maximum,viewport=1;bool hover,drag;int origin,initial;
 public event EventHandler? ValueChanged;
 public Action<int>? WheelRequested;
 public int Maximum{get=>maximum;set{maximum=Math.Max(0,value);Value=this.value;Visible=maximum>0;Invalidate();}}
 public int Viewport{get=>viewport;set{viewport=Math.Max(1,value);Invalidate();}}
 public int Value{get=>value;set{var next=Math.Clamp(value,0,maximum);if(next==this.value)return;this.value=next;Invalidate();ValueChanged?.Invoke(this,EventArgs.Empty);}}
 public SlimScrollBar(){Width=12;TabStop=false;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);AccessibleRole=AccessibleRole.ScrollBar;}
 RectangleF Thumb{get{float length=Math.Min(Height,Math.Max(28,Height*viewport/(float)Math.Max(1,maximum+viewport)));return new(hover||drag?2:4,(Height-length)*value/Math.Max(1,maximum),hover||drag?Width-4:Width-8,length);}}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Height<2||Maximum==0)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Design.Round(Thumb,Math.Min(3,Thumb.Width/2));using var fill=new SolidBrush(hover||drag?Design.Muted:Design.BorderHover);e.Graphics.FillPath(fill,path);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(Thumb.Contains(e.Location)){drag=true;Capture=true;origin=e.Y;initial=Value;}else Value+=e.Y<Thumb.Y?-Viewport:Viewport;}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(drag)Value=initial+(int)((e.Y-origin)*Maximum/Math.Max(1,Height-Thumb.Height));}
 protected override void OnMouseUp(MouseEventArgs e){drag=false;Capture=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnMouseWheel(MouseEventArgs e){if(WheelRequested!=null)WheelRequested(e.Delta);else Value-=e.Delta*40/120;base.OnMouseWheel(e);}
}
sealed class ModernInput:UserControl {
 sealed class InputTextBox:TextBox{public Action<int>? Wheel;protected override void WndProc(ref Message m){if(m.Msg==0x20A&&Multiline){Wheel?.Invoke(unchecked((short)((long)m.WParam>>16)));return;}base.WndProc(ref m);}}
 readonly InputTextBox editor=new(){BorderStyle=BorderStyle.None,AutoSize=false};readonly SlimScrollBar scroll=new();bool hover;string? error;readonly Motion interaction;double emphasis;
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
 public ModernInput(){interaction=new Motion(this,v=>{emphasis=v;Invalidate();});AutoScaleMode=AutoScaleMode.None;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Height=40;Cursor=Cursors.IBeam;editor.Cursor=Cursors.IBeam;Margin=new(4);Controls.Add(editor);Controls.Add(scroll);editor.TextChanged+=(_,_)=>{error=null;Invalidate();OnTextChanged(EventArgs.Empty);SyncScroll();};editor.GotFocus+=(_,_)=>interaction.To(1);editor.LostFocus+=(_,_)=>interaction.To(hover?.4:0);editor.MouseEnter+=(_,_)=>{hover=true;interaction.To(editor.Focused?1:.4);};editor.MouseLeave+=(_,_)=>{hover=false;interaction.To(editor.Focused?1:0);};editor.Wheel=delta=>{if(scroll.Maximum>0)scroll.Value-=Math.Sign(delta)*Math.Max(1,SystemInformation.MouseWheelScrollLines);};editor.KeyUp+=(_,_)=>SyncScroll();scroll.WheelRequested=delta=>{if(scroll.Maximum>0)scroll.Value-=Math.Sign(delta)*Math.Max(1,SystemInformation.MouseWheelScrollLines);};scroll.ValueChanged+=(_,_)=>{if(!editor.IsHandleCreated)return;int first=(int)SendMessage(editor.Handle,0xCE,IntPtr.Zero,IntPtr.Zero);SendMessage(editor.Handle,0xB6,IntPtr.Zero,(IntPtr)(scroll.Value-first));};AccessibleRole=AccessibleRole.Text;TabStop=true;}
 [System.Diagnostics.CodeAnalysis.AllowNull] public override string Text{get=>editor.Text;set{editor.Text=value??"";}}
 public string PlaceholderText{get=>editor.PlaceholderText;set{editor.PlaceholderText=value;editor.AccessibleName=value;AccessibleName=value;}}
 public override Size GetPreferredSize(Size proposedSize)=>new(proposedSize.Width>0?proposedSize.Width:Width,Height);
 public bool UseSystemPasswordChar{get=>editor.UseSystemPasswordChar;set=>editor.UseSystemPasswordChar=value;}
 public bool Multiline{get=>editor.Multiline;set{editor.Multiline=value;editor.WordWrap=false;PerformLayout();}}
 public bool ReadOnly{get=>editor.ReadOnly;set=>editor.ReadOnly=value;}
 
 ScrollBars scrollMode=ScrollBars.None;public ScrollBars ScrollBars{get=>scrollMode;set{scrollMode=value;SyncScroll();}}
 public string? Error{get=>error;set{error=value;AccessibleDescription=value;Invalidate();}}
 public void Clear()=>editor.Clear();
 public void FocusEditor()=>editor.Focus();
 internal Rectangle EditorBounds=>editor.Bounds;
 internal int FirstVisibleLine=>editor.IsHandleCreated?(int)SendMessage(editor.Handle,0xCE,IntPtr.Zero,IntPtr.Zero):0;
 internal int ScrollMaximum=>scroll.Maximum;
 internal void ScrollToLine(int line)=>scroll.Value=line;
 public void SyncTheme(){editor.BackColor=Design.Surface;editor.ForeColor=Enabled?Design.Text:Design.Muted;editor.Font=Font;scroll.BackColor=Design.Surface;PerformLayout();Invalidate();}
 void SyncScroll(){if(!Multiline||ScrollBars==ScrollBars.None||!editor.IsHandleCreated){scroll.Visible=false;return;}int rows=Math.Max(1,editor.Height/Math.Max(1,editor.Font.Height));scroll.Viewport=rows;scroll.Maximum=Math.Max(0,editor.Lines.Length-rows);scroll.Value=(int)SendMessage(editor.Handle,0xCE,IntPtr.Zero,IntPtr.Zero);}
 protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);int pad=Design.Scale(this,12);editor.Bounds=new(pad,Multiline?pad:Math.Max(2,(Height-editor.PreferredHeight)/2),Math.Max(1,Width-pad*2-(Multiline?12:0)),Multiline?Math.Max(1,Height-pad*2):editor.PreferredHeight);scroll.Bounds=new(Width-16,pad,12,Math.Max(1,Height-pad*2));SyncScroll();}
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);if(editor!=null){editor.Font=Font;PerformLayout();SyncScroll();}}
 protected override void OnRightToLeftChanged(EventArgs e){base.OnRightToLeftChanged(e);if(editor!=null){editor.RightToLeft=RightToLeft;editor.TextAlign=HorizontalAlignment.Left;}}
 protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);SyncTheme();}
 protected override void OnEnter(EventArgs e){base.OnEnter(e);editor.Focus();}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){editor.Focus();interaction.To(1);base.OnMouseDown(e);}
 protected override void OnPaint(PaintEventArgs e){Design.PaintBackdrop(e.Graphics,this);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusInput);using var bg=new SolidBrush(Design.Surface);e.Graphics.FillPath(bg,path);using var pen=new Pen(Error!=null?Design.Error:Design.Blend(Design.Border,Design.Accent,emphasis),(float)(1+emphasis*.7));if(!Multiline||!ReadOnly)e.Graphics.DrawPath(pen,path);}
}
sealed class ModernSelect:Control {
 int index=-1;readonly Motion interaction;double emphasis;ToolStripDropDown? popup;
 public List<object> Items{get;}=new();public event EventHandler? SelectedIndexChanged;
 public int SelectedIndex{get=>index;set{int next=value>=0&&value<Items.Count?value:-1;if(index==next)return;index=next;Invalidate();SelectedIndexChanged?.Invoke(this,EventArgs.Empty);}}
 public object? SelectedItem{get=>index>=0&&index<Items.Count?Items[index]:null;set=>SelectedIndex=Items.IndexOf(value!);}
 public ComboBoxStyle DropDownStyle{get;set;}=ComboBoxStyle.DropDownList;
 public ModernSelect(){interaction=new Motion(this,v=>{emphasis=v;Invalidate();});GotFocus+=(_,_)=>interaction.To(1);LostFocus+=(_,_)=>interaction.To(0);Height=40;TabStop=true;Cursor=Cursors.Hand;Margin=new(4);AccessibleRole=AccessibleRole.ComboBox;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 public string GetItemText(object? item)=>item?.ToString()??"";
 protected override void OnMouseEnter(EventArgs e){interaction.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){interaction.To(Focused?1:0);base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left)Open();}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode is Keys.Space or Keys.Enter or Keys.F4){Open();e.Handled=true;}else if(e.KeyCode==Keys.Down){SelectedIndex=Math.Min(Items.Count-1,index+1);e.Handled=true;}else if(e.KeyCode==Keys.Up){SelectedIndex=Math.Max(0,index-1);e.Handled=true;}}
 void Open(){if(Items.Count==0||!Enabled)return;if(popup?.Visible==true){popup.Close();return;}popup?.Dispose();var menu=new OptionList(Items,SelectedIndex,RightToLeft){Size=new(Math.Max(Width,Design.Scale(this,200)),Math.Min(Items.Count,6)*Design.Scale(this,42)+8)};popup=new AnimatedDropDown{Padding=Padding.Empty,Margin=Padding.Empty,AutoSize=true,DropShadowEnabled=false,BackColor=Design.Surface,RightToLeft=RightToLeft.No};var host=new ToolStripControlHost(menu){Padding=Padding.Empty,Margin=Padding.Empty,AutoSize=false,Size=menu.Size};popup.Items.Add(host);menu.Picked+=(_,picked)=>{SelectedIndex=picked;popup.Close();};popup.Closed+=(_,_)=>{Invalidate();Focus();};popup.Show(this,new Point(RightToLeft==RightToLeft.Yes?Width:0,Height+4),RightToLeft==RightToLeft.Yes?ToolStripDropDownDirection.BelowLeft:ToolStripDropDownDirection.BelowRight);using(var rounded=Design.Round(new RectangleF(0,0,popup.Width,popup.Height),Design.RadiusInput)){popup.Region=new Region(rounded);}menu.Focus();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){Design.PaintBackdrop(e.Graphics,this);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusInput);using var bg=new SolidBrush(Design.Surface);e.Graphics.FillPath(bg,path);using var border=new Pen(popup?.Visible==true?Design.Accent:Design.Blend(Design.Border,Design.Accent,emphasis));e.Graphics.DrawPath(border,path);int arrowX=RightToLeft==RightToLeft.Yes?18:Width-18;using var arrow=new Pen(Enabled?Design.Muted:Design.BorderHover,1.5f);e.Graphics.DrawLines(arrow,new[]{new PointF(arrowX-4,Height/2f-2),new PointF(arrowX,Height/2f+2),new PointF(arrowX+4,Height/2f-2)});var r=new Rectangle(RightToLeft==RightToLeft.Yes?34:12,0,Width-46,Height);TextRenderer.DrawText(e.Graphics,GetItemText(SelectedItem),Font,r,Enabled?Design.Text:Design.Muted,Design.TextFlags(GetItemText(SelectedItem))|TextFormatFlags.EndEllipsis);}
 protected override AccessibleObject CreateAccessibilityInstance()=>new SelectAccess(this);
 sealed class SelectAccess:ControlAccessibleObject {readonly ModernSelect select;public SelectAccess(ModernSelect owner):base(owner){select=owner;}public override string? Value{get=>select.GetItemText(select.SelectedItem);set{}}public override string DefaultAction=>"بازکردن فهرست";public override void DoDefaultAction()=>select.Open();}
 protected override void Dispose(bool disposing){if(disposing)popup?.Dispose();base.Dispose(disposing);}
}
sealed class OptionList:Control {
 readonly List<object> items;readonly SlimScrollBar scroll=new();int hover=-1,chosen;public event EventHandler<int>? Picked;
 int Row=>Design.Scale(this,42);
 public OptionList(List<object> options,int selected,RightToLeft rtl){items=options;chosen=selected;RightToLeft=rtl;Font=Design.Font();TabStop=true;Controls.Add(scroll);scroll.ValueChanged+=(_,_)=>Invalidate();SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);AccessibleRole=AccessibleRole.List;}
 protected override void OnResize(EventArgs e){base.OnResize(e);scroll.Bounds=new(Width-14,4,12,Math.Max(1,Height-8));scroll.Viewport=Height-8;scroll.Maximum=Math.Max(0,items.Count*Row-scroll.Viewport);scroll.BackColor=Design.Surface;}
 protected override void OnMouseWheel(MouseEventArgs e){scroll.Value-=e.Delta*Row/120;base.OnMouseWheel(e);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int next=(e.Y-4+scroll.Value)/Row;if(hover!=next){hover=next;Invalidate();}}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){int i=(e.Y-4+scroll.Value)/Row;if(i>=0&&i<items.Count)Picked?.Invoke(this,i);}base.OnMouseDown(e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Down)chosen=Math.Min(items.Count-1,chosen+1);else if(e.KeyCode==Keys.Up)chosen=Math.Max(0,chosen-1);else if(e.KeyCode==Keys.Enter){Picked?.Invoke(this,chosen);e.Handled=true;return;}else{base.OnKeyDown(e);return;}if(chosen*Row<scroll.Value)scroll.Value=chosen*Row;else if((chosen+1)*Row>scroll.Value+scroll.Viewport)scroll.Value=(chosen+1)*Row-scroll.Viewport;Invalidate();e.Handled=true;}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Design.Surface);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var outer=Design.Round(new RectangleF(.5f,.5f,Width-1,Height-1),Design.RadiusInput);using var border=new Pen(Design.Border);e.Graphics.DrawPath(border,outer);for(int i=Math.Max(0,scroll.Value/Row);i<items.Count;i++){int y=4+i*Row-scroll.Value;if(y>=Height)break;var r=new Rectangle(6,y,Width-24,Row-2);if(i==hover||i==chosen){using var path=Design.Round(r,8);using var fill=new SolidBrush(i==chosen?Design.Selection:Design.SurfaceSecondary);e.Graphics.FillPath(fill,path);}r.Inflate(-10,0);var text=items[i]?.ToString()??"";TextRenderer.DrawText(e.Graphics,text,Font,r,Design.Text,Design.TextFlags(text)|TextFormatFlags.EndEllipsis);}}
}
sealed class ModernToggle:CheckBox {
 bool hover;readonly Motion interaction;double progress;
 public ModernToggle(){interaction=new Motion(this,v=>{progress=v;Invalidate();});CheckedChanged+=(_,_)=>interaction.To(Checked?1:0);Appearance=Appearance.Button;AutoSize=false;Height=42;Width=500;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor??Design.Surface);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int x=RightToLeft==RightToLeft.Yes?Width-46:2;var track=new RectangleF(x,(Height-22)/2f,42,22);using var path=Design.Round(track,11);using var fill=new SolidBrush(Design.Blend(hover?Design.BorderHover:Design.Border,Design.Accent,progress));e.Graphics.FillPath(fill,path);using var knob=new SolidBrush(Checked?Design.OnAccent:Design.Muted);e.Graphics.FillEllipse(knob,x+3+(float)(20*progress),(Height-16)/2f,16,16);var textRect=new Rectangle(RightToLeft==RightToLeft.Yes?0:58,0,Width-60,Height);TextRenderer.DrawText(e.Graphics,Text,Font,textRect,Enabled?Design.Text:Design.Muted,Design.TextFlags(Text));if(Focused){using var focus=new Pen(Design.Accent);e.Graphics.DrawPath(focus,path);}}
}
sealed class ScrollPage:Panel {
 readonly SlimScrollBar scroll=new();Panel? content;bool arranging;
 public ScrollPage(){AutoScroll=false;Controls.Add(scroll);scroll.ValueChanged+=(_,_)=>Arrange();}
 public void SetContent(Panel panel){content=panel;Controls.Add(panel);panel.SizeChanged+=(_,_)=>Arrange();Arrange();}
 void Arrange(){if(arranging||content==null)return;arranging=true;try{int available=Math.Max(1,ClientSize.Height-Padding.Bottom);scroll.Bounds=new(ClientSize.Width-12,0,12,available);scroll.Viewport=available;scroll.Maximum=Math.Max(0,content.Height-available);scroll.BackColor=BackColor;content.Location=new Point(0,-scroll.Value);scroll.BringToFront();}finally{arranging=false;}}
 protected override void OnLayout(LayoutEventArgs levent){base.OnLayout(levent);Arrange();}
 protected override void OnMouseWheel(MouseEventArgs e){scroll.Value-=e.Delta*48/120;base.OnMouseWheel(e);}
}
