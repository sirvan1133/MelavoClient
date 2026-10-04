using System.Diagnostics;
namespace MelavoClient;
sealed class Motion:IDisposable {
 readonly System.Windows.Forms.Timer timer=new(){Interval=16};readonly Stopwatch clock=new();readonly Action<double> paint;double from,to;public double Value{get;private set;}
 public Motion(Control owner,Action<double> update){paint=update;timer.Tick+=(_,_)=>{double t=Math.Min(1,clock.Elapsed.TotalMilliseconds/160);Value=from+(to-from)*(1-Math.Pow(1-t,3));paint(Value);if(t>=1)timer.Stop();};owner.Disposed+=(_,_)=>Dispose();}
 public void To(double target){from=Value;to=target;clock.Restart();timer.Start();}
 public void Dispose()=>timer.Dispose();
 public static void Reveal(Control target){foreach(var cover in target.Controls.OfType<TransitionCover>().ToArray())cover.Dispose();if(!target.IsDisposed){target.PerformLayout();target.Invalidate(true);}}
 public static void Dialog(Form form){if(form.Tag is string tag&&tag=="motion")return;form.Tag="motion";bool closing=false,finished=false;var result=DialogResult.None;ModalBackdrop? backdrop=null;form.HandleCreated+=(_,_)=>NativeTheme.Window(form.Handle);var motion=new Motion(form,v=>{form.Opacity=Math.Clamp(v,0,1);if(backdrop!=null&&!backdrop.IsDisposed){backdrop.Strength=v;backdrop.Invalidate();}if(closing&&v<.001){finished=true;form.DialogResult=result;form.Close();}});form.Opacity=0;form.Shown+=(_,_)=>{NativeTheme.Window(form.Handle);if(form.Owner is Form owner&&owner.ClientSize.Width>0){backdrop=new ModalBackdrop(owner){Dock=DockStyle.Fill};owner.Controls.Add(backdrop);backdrop.BringToFront();}motion.To(1);};form.FormClosed+=(_,_)=>{backdrop?.Dispose();form.Owner?.Invalidate(true);};form.Disposed+=(_,_)=>backdrop?.Dispose();form.FormClosing+=(_,e)=>{if(e.Cancel||finished)return;e.Cancel=true;if(closing)return;closing=true;result=form.DialogResult;form.DialogResult=DialogResult.None;motion.To(0);};}
}
sealed class ModalBackdrop:Control {
 readonly Bitmap original,blurred;public double Strength;readonly Color canvas;
 public ModalBackdrop(Form owner){DoubleBuffered=true;TabStop=false;canvas=Design.Canvas;original=new Bitmap(owner.ClientSize.Width,owner.ClientSize.Height);using(var window=new Bitmap(owner.Width,owner.Height)){owner.DrawToBitmap(window,new Rectangle(Point.Empty,owner.Size));var origin=owner.PointToScreen(Point.Empty);using var graphics=Graphics.FromImage(original);graphics.DrawImageUnscaled(window,owner.Left-origin.X,owner.Top-origin.Y);}blurred=BackdropBlur.Create(original,Math.Max(2,Design.Scale(owner,3)));AccessibleName="Dialog backdrop";}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.DrawImage(original,ClientRectangle);using var attributes=new System.Drawing.Imaging.ImageAttributes();attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix{Matrix33=(float)Strength});e.Graphics.DrawImage(blurred,ClientRectangle,0,0,blurred.Width,blurred.Height,GraphicsUnit.Pixel,attributes);using var tint=new SolidBrush(Color.FromArgb((int)((Design.Dark?38:24)*Strength),canvas));e.Graphics.FillRectangle(tint,ClientRectangle);}
 protected override void Dispose(bool disposing){if(disposing){original.Dispose();blurred.Dispose();}base.Dispose(disposing);}
}
sealed class TransitionCover:Control {
 readonly Bitmap image;public double Progress=1;
 public TransitionCover(Bitmap bitmap){image=bitmap;DoubleBuffered=true;Disposed+=(_,_)=>image.Dispose();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Design.Canvas);using var attributes=new System.Drawing.Imaging.ImageAttributes();var matrix=new System.Drawing.Imaging.ColorMatrix{Matrix33=(float)Progress};attributes.SetColorMatrix(matrix);e.Graphics.DrawImage(image,new Rectangle(0,(int)(8*(1-Progress)),Width,Height),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}
}
sealed class AnimatedDropDown:ToolStripDropDown {
 readonly Motion motion;bool closing,finished;ToolStripDropDownCloseReason reason;
 public AnimatedDropDown(){motion=new Motion(this,v=>{Opacity=Math.Clamp(v,0,1);if(closing&&v<.001){finished=true;Close(reason);}});Opened+=(_,_)=>{closing=finished=false;Opacity=0;motion.To(1);};Closing+=(_,e)=>{if(finished)return;e.Cancel=true;if(closing)return;closing=true;reason=e.CloseReason;motion.To(0);};}
}


