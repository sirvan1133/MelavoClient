namespace MelavoClient;
sealed class ServerCell { public string Text{get;set;} public ServerCell(string text){Text=text;} }
sealed class ServerCells:List<ServerCell>{public void Add(string text)=>Add(new ServerCell(text));}
sealed class ServerItem {
 bool selected;internal ServerList? Owner;public object? Tag{get;set;}public string Text{get=>SubItems[0].Text;set=>SubItems[0].Text=value;}public ServerCells SubItems{get;}=new();
 public ServerItem(string text){SubItems.Add(text);}
 public bool Selected{get=>selected;set{if(value==selected)return;selected=value;if(value)Owner?.SelectItem(this);else Owner?.NotifySelection();}}
 internal void SetSelected(bool value)=>selected=value;
}
sealed class ServerColumn {public string Text;public int Width{get;set;}=100;public ServerColumn(string text){Text=text;}}
sealed class ServerColumns:List<ServerColumn>{public void Add(string text)=>Add(new ServerColumn(text));}
sealed class ServerItems:List<ServerItem>{readonly ServerList owner;public ServerItems(ServerList list){owner=list;}public new void Add(ServerItem item){item.Owner=owner;base.Add(item);}public new void AddRange(IEnumerable<ServerItem> items){foreach(var item in items)Add(item);}public new void Clear(){foreach(var item in this)item.Owner=null;base.Clear();}}
sealed class ServerList:Control {
 readonly SlimScrollBar scroll=new();readonly ToolTip tips=new();int hover=-1;bool updating;double target;bool animating;readonly System.Windows.Forms.Timer smooth=new(){Interval=16};
 public string EmptyText{get;set;}="No servers found";
 public string EmptyDescription{get;set;}="Add a subscription or import a configuration\nto see available servers.";
 public int ScrollOffset=>scroll.Value;
 public ServerItems Items{get;}public ServerColumns Columns{get;}=new();public event EventHandler? SelectedIndexChanged;
 public List<ServerItem> SelectedItems=>Items.Where(x=>x.Selected).ToList();public List<int> SelectedIndices=>Items.Select((x,i)=>(x,i)).Where(p=>p.x.Selected).Select(p=>p.i).ToList();
 public ServerItem? TopItem{get=>Items.Count>0?Items[Math.Clamp(scroll.Value/Row,0,Items.Count-1)]:null;set{int index=value==null?-1:Items.IndexOf(value);if(index>=0)scroll.Value=index*Row;}}
 int Row=>Design.Scale(this,44);int Header=>Design.Scale(this,40);
 public bool SelectAt(Point point){if(point.Y<Header)return false;int index=(point.Y-Header+scroll.Value)/Row;if(index<0||index>=Items.Count)return false;Items[index].Selected=true;return true;}
 public ServerList(){Items=new(this);TabStop=true;AccessibleRole=AccessibleRole.List;AccessibleName="فهرست سرورها";SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.Selectable,true);Controls.Add(scroll);scroll.ValueChanged+=(_,_)=>{if(!animating)target=scroll.Value;hover=-1;Invalidate();};scroll.WheelRequested=delta=>QueueScroll(-delta*Row*2/120);smooth.Tick+=(_,_)=>AdvanceScroll();smooth.Start();}
 public void BeginUpdate()=>updating=true;
 public void EndUpdate(){updating=false;UpdateScroll();Invalidate();NotifySelection();}
 internal void SelectItem(ServerItem item){foreach(var other in Items)if(other!=item)other.SetSelected(false);NotifySelection();}
 internal void NotifySelection(){if(updating)return;Invalidate();SelectedIndexChanged?.Invoke(this,EventArgs.Empty);}
 public void RestoreTop(int profileIndex,int previousOffset=-1){var item=Items.FirstOrDefault(x=>x.Tag is int index&&index==profileIndex);if(item!=null){TopItem=item;if(previousOffset>=0)scroll.Value+=previousOffset%Row;}}
 void UpdateScroll(){scroll.Bounds=new(RightToLeft==RightToLeft.Yes?0:Width-12,Header,12,Math.Max(1,Height-Header));scroll.Viewport=Math.Max(1,Height-Header);scroll.Maximum=Math.Max(0,Items.Count*Row-scroll.Viewport);scroll.BackColor=Design.Surface;}
 protected override void OnResize(EventArgs e){base.OnResize(e);UpdateScroll();Invalidate();}
 protected override void OnMouseWheel(MouseEventArgs e){QueueScroll(-e.Delta*Row*2/120);base.OnMouseWheel(e);}
 internal void QueueScroll(int pixels)=>target=Math.Clamp(target+pixels,0,scroll.Maximum);
 internal void AdvanceScroll(){double gap=target-scroll.Value;if(Math.Abs(gap)<1)return;int step=(int)Math.Round(gap*.24);if(step==0)step=Math.Sign(gap);animating=true;scroll.Value+=step;animating=false;}
 protected override void OnRightToLeftChanged(EventArgs e){base.OnRightToLeftChanged(e);if(scroll!=null)UpdateScroll();Invalidate();}
 public void ScrollRows(int count)=>scroll.Value+=count*Row;
 protected override AccessibleObject CreateAccessibilityInstance()=>new ListAccess(this);
 sealed class ListAccess:ControlAccessibleObject {
  readonly ServerList list;public ListAccess(ServerList owner):base(owner){list=owner;}
  public override int GetChildCount()=>list.Items.Count;
  public override AccessibleObject? GetChild(int index)=>index>=0&&index<list.Items.Count?new RowAccess(list,index):null;
  public override AccessibleObject? GetSelected(){int index=list.SelectedIndices.FirstOrDefault(-1);return GetChild(index);}
 }
 sealed class RowAccess:AccessibleObject {
  readonly ServerList list;readonly int index;public RowAccess(ServerList owner,int row){list=owner;index=row;}
  public override string? Name{get=>StripFlags(list.Items[index].Text);set{}}
  public override string? Value{get=>string.Join(" · ",list.Items[index].SubItems.Skip(1).Select(c=>c.Text));set{}}
  public override AccessibleRole Role=>AccessibleRole.ListItem;
  public override AccessibleStates State=>AccessibleStates.Selectable|(list.Items[index].Selected?AccessibleStates.Selected:AccessibleStates.None);
  public override Rectangle Bounds=>list.RectangleToScreen(new Rectangle(18,list.Header+index*list.Row-list.scroll.Value,Math.Max(1,list.Width-18),list.Row));
  public override string DefaultAction=>"انتخاب سرور";
  public override void DoDefaultAction(){list.Items[index].Selected=true;}
  public override void Select(AccessibleSelection flags){list.Items[index].Selected=true;}
 }
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int next=e.Y<Header?-1:(e.Y-Header+scroll.Value)/Row;if(next>=Items.Count)next=-1;if(hover==next)return;hover=next;tips.SetToolTip(this,next>=0?Items[next].Text:null);Invalidate();}
 protected override void OnMouseLeave(EventArgs e){hover=-1;tips.SetToolTip(this,null);Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button is not (MouseButtons.Left or MouseButtons.Right)||e.Y<Header)return;int index=(e.Y-Header+scroll.Value)/Row;if(index>=0&&index<Items.Count)Items[index].Selected=true;}
 protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Up or Keys.Down or Keys.PageDown or Keys.PageUp or Keys.Home or Keys.End||base.IsInputKey(keyData);
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(Items.Count==0)return;int current=SelectedIndices.FirstOrDefault();int next=e.KeyCode switch{Keys.Up=>current-1,Keys.Down=>current+1,Keys.PageUp=>current-Math.Max(1,scroll.Viewport/Row),Keys.PageDown=>current+Math.Max(1,scroll.Viewport/Row),Keys.Home=>0,Keys.End=>Items.Count-1,_=>-1};if(next<0&&e.KeyCode!=Keys.Up)return;next=Math.Clamp(next,0,Items.Count-1);Items[next].Selected=true;if(next*Row<scroll.Value)scroll.Value=next*Row;else if((next+1)*Row>scroll.Value+scroll.Viewport)scroll.Value=(next+1)*Row-scroll.Viewport;e.Handled=true;}
 void PaintEmpty(Graphics graphics){
  graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  int available=Math.Max(0,Height-Header);if(available<36)return;
  float scale=Math.Min(DeviceDpi/96f,Math.Max(.45f,(available-24)/190f));
  float cx=Width/2f,top=Header+(available-170*scale)/2f;
  if(available>=145){
   for(int i=0;i<2;i++){
    var face=new RectangleF(cx-32*scale,top+i*26*scale,64*scale,22*scale);
    using var shape=Design.Round(face,7*scale);using var fill=new System.Drawing.Drawing2D.LinearGradientBrush(face,Color.FromArgb(45,89,144,220),Color.FromArgb(25,139,123,255),135f);graphics.FillPath(fill,shape);
    using var outline=new Pen(Color.FromArgb(125,145,187,237),1.1f);graphics.DrawPath(outline,shape);
    using var dot=new SolidBrush(i==0?Color.FromArgb(111,208,255):Color.FromArgb(155,134,255));graphics.FillEllipse(dot,face.Right-12*scale,face.Y+9*scale,4*scale,4*scale);
   }
  }
  using var title=Design.Font(11);using var subtitle=Design.Font(9);
  int titleY=available>=145?(int)(top+62*scale):Header+8;
  var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix;
  TextRenderer.DrawText(graphics,EmptyText,title,new Rectangle(16,titleY,Width-32,30),Design.Text,flags|(Design.Persian(EmptyText)?TextFormatFlags.RightToLeft:0));
  int descriptionY=titleY+34,descriptionHeight=Math.Min(58,Height-descriptionY-10);
  if(descriptionHeight>=30)TextRenderer.DrawText(graphics,EmptyDescription,subtitle,new Rectangle(20,descriptionY,Width-40,descriptionHeight),Design.Muted,flags|TextFormatFlags.WordBreak|(Design.Persian(EmptyDescription)?TextFormatFlags.RightToLeft:0));
 }
 protected override void OnPaint(PaintEventArgs e){if(Design.Glass)Design.PaintBackdrop(e.Graphics,this);else e.Graphics.Clear(Design.Surface);if(Columns.Count==0)return;using var caption=Design.Font(9);using var body=Design.Font(9);using var divider=new Pen(Design.Border);if(Design.Glass){using var wash=new SolidBrush(Color.FromArgb(9,Color.White));e.Graphics.FillRectangle(wash,new Rectangle(0,0,Width,Header));}int usable=Width-18;float total=Columns.Sum(c=>Math.Max(1,c.Width));Rectangle Cell(int col,int y,int height){if(RightToLeft!=RightToLeft.Yes){float left=0;for(int j=0;j<col;j++)left+=usable*Columns[j].Width/total;return new Rectangle((int)left,y,(int)Math.Round(usable*Columns[col].Width/total),height);}float right=usable;for(int j=0;j<col;j++)right-=usable*Columns[j].Width/total;int width=(int)Math.Round(usable*Columns[col].Width/total);return new Rectangle((int)right-width+18,y,width,height);}for(int col=0;col<Columns.Count;col++){var r=Cell(col,0,Header);r.Inflate(-6,0);if(Design.Glass&&(r.Width>=100||col==6)){string[] glyphs={"\uE774","\uE774","\uE71B","\uE701","\uE930","\uE9D9","\uE734"};using var iconFont=new Font("Segoe MDL2 Assets",9);int iconX=RightToLeft==RightToLeft.Yes?r.Right-22:r.Left;TextRenderer.DrawText(e.Graphics,glyphs[col],iconFont,new Rectangle(iconX,r.Y,20,r.Height),col==4?Color.FromArgb(77,218,172):Design.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter);if(col<6){if(RightToLeft!=RightToLeft.Yes)r.X+=25;r.Width-=25;}}if(col!=6||!Design.Glass)TextRenderer.DrawText(e.Graphics,Columns[col].Text,caption,r,Design.Muted,Design.TextFlags(Columns[col].Text)|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);}e.Graphics.DrawLine(divider,18,Header-1,Width,Header-1);var state=e.Graphics.Save();e.Graphics.SetClip(new Rectangle(RightToLeft==RightToLeft.Yes?14:0,Header,Math.Max(1,Width-14),Math.Max(1,Height-Header)));for(int i=Math.Max(0,scroll.Value/Row);i<Items.Count;i++){int y=Header+i*Row-scroll.Value;if(y>=Height)break;var item=Items[i];var row=new Rectangle(RightToLeft==RightToLeft.Yes?18:2,y,Width-20,Row-1);if(item.Selected||hover==i){using var path=Design.Round(row,14);using var fill=new SolidBrush(item.Selected?Design.Selection:Design.SurfaceSecondary);e.Graphics.FillPath(fill,path);}if(item.Selected){using var pen=new Pen(Design.Accent,2);e.Graphics.DrawLine(pen,RightToLeft==RightToLeft.Yes?Width-2:2,y+9,RightToLeft==RightToLeft.Yes?Width-2:2,y+Row-9);}for(int col=0;col<Columns.Count&&col<item.SubItems.Count;col++){var r=Cell(col,y,Row);r.Inflate(-6,0);var text=item.SubItems[col].Text;if(col==1){var code=text.Split(' ').Last();if(FlagPainter.Draw(e.Graphics,r,code)){r.X+=28;r.Width-=28;text=code;}}if(col==0)text=StripFlags(text);var color=col==4?Design.Muted:col==3&&text!="—"?(text is "پاسخ نداد" or "No reply"?Design.Error:Design.Accent):Design.Text;TextRenderer.DrawText(e.Graphics,text,body,r,color,Design.TextFlags(text)|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding|TextFormatFlags.PreserveGraphicsClipping);}e.Graphics.DrawLine(divider,24,y+Row-1,Width-8,y+Row-1);}e.Graphics.Restore(state);if(Items.Count==0)PaintEmpty(e.Graphics);}
 internal static string StripFlags(string text)=>string.Concat(text.EnumerateRunes().Where(r=>r.Value is <0x1F1E6 or >0x1F1FF).Select(r=>r.ToString())).Trim();
 protected override void Dispose(bool disposing){if(disposing){tips.Dispose();smooth.Dispose();}base.Dispose(disposing);}
}




