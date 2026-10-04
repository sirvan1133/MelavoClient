namespace MelavoClient;
sealed class StableLabel:Label {
 public string? IconGlyph{get;set;}
 public StableLabel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnPaint(PaintEventArgs e){
  if(!Design.Glass||IconGlyph==null){if(!AutoSize&&!Text.Contains("\n")){var area=new Rectangle(Padding.Left,Padding.Top,Math.Max(1,Width-Padding.Horizontal),Math.Max(1,Height-Padding.Vertical));using var fitted=Design.FitFont(Text,Font,area.Size);var align=TextAlign is ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter?TextFormatFlags.HorizontalCenter:TextAlign is ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight?TextFormatFlags.Right:TextFormatFlags.Left;TextRenderer.DrawText(e.Graphics,Text,fitted,area,ForeColor,align|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|(RightToLeft==RightToLeft.Yes?TextFormatFlags.RightToLeft:0));}else base.OnPaint(e);return;}
  int iconWidth=22;bool rtl=RightToLeft==RightToLeft.Yes;using var iconFont=new Font("Segoe MDL2 Assets",9);
  TextRenderer.DrawText(e.Graphics,IconGlyph,iconFont,new Rectangle(rtl?Width-iconWidth:0,0,iconWidth,Height),Design.Accent,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter);
  TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(rtl?0:iconWidth+3,0,Math.Max(1,Width-iconWidth-3),Height),ForeColor,Design.TextFlags(Text)|TextFormatFlags.EndEllipsis);
 }
}
sealed class BufferedPanel:Panel {public BufferedPanel(){DoubleBuffered=true;}protected override void OnPaintBackground(PaintEventArgs e){if(Design.Glass)Design.PaintBackdrop(e.Graphics,this);else base.OnPaintBackground(e);}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Design.Glass&&Parent is BufferedTable){using var divider=new Pen(Color.FromArgb(65,129,168,246));e.Graphics.DrawLine(divider,Width-1,8,Width-1,Height-8);}}}
sealed class BufferedTable:TableLayoutPanel {public BufferedTable(){DoubleBuffered=true;}protected override void OnPaintBackground(PaintEventArgs e){if(Design.Glass)Design.PaintBackdrop(e.Graphics,this);else base.OnPaintBackground(e);}}
sealed class BufferedFlow:FlowLayoutPanel {public BufferedFlow(){DoubleBuffered=true;}protected override void OnPaintBackground(PaintEventArgs e){if(Design.Glass)Design.PaintBackdrop(e.Graphics,this);else base.OnPaintBackground(e);}}

