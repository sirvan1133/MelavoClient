namespace MelavoClient;
sealed class StableLabel:Label {
 public StableLabel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
}
sealed class BufferedPanel:Panel {public BufferedPanel(){DoubleBuffered=true;}}
sealed class BufferedTable:TableLayoutPanel {public BufferedTable(){DoubleBuffered=true;}}
