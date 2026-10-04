namespace MelavoClient;
sealed partial class Client {
 static void CheckPremiumRendering(Action<bool,string> assert){
  void Pump(int milliseconds){var watch=System.Diagnostics.Stopwatch.StartNew();while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();Thread.Sleep(5);}}
  Design.Glass=true;Design.Dark=true;GlassAnimation.ReducedMotionOverride=false;GlassAnimation.Capture=false;
  using var host=new Form{ShowInTaskbar=false,Size=new(600,360)};using var card=new Card{Bounds=new(20,20,500,260)};var label=Design.Label("125 KB/s",12);label.AutoSize=false;label.Bounds=new(90,90,180,40);card.Controls.Add(label);host.Controls.Add(card);host.Show();Pump(1100);
  int paints=0;label.Paint+=(_,_)=>paints++;var phase=card.ReflectionPhase;int blurBuilds=GlassMaterial.BlurBuilds;int frameBuilds=card.ReflectionFrameBuilds;using var process=System.Diagnostics.Process.GetCurrentProcess();var before=process.TotalProcessorTime;Pump(550);var consumed=process.TotalProcessorTime-before;
  assert(card.ReflectionPhase!=phase,"specular reflection travels continuously");assert(paints==0,"border animation does not repaint traffic text");assert(card.ReflectionFrameBuilds==frameBuilds,"animation ticks only blit cached frames");assert(GlassMaterial.BlurBuilds==blurBuilds,"animation does not rebuild backdrop blur");
  var overlay=card.Controls.OfType<GlassBorderOverlay>().Single();assert(overlay.Region!=null&&!overlay.Region.IsVisible(card.Width/2,card.Height/2),"animation surface excludes card content");
  GlassAnimation.ReducedMotionOverride=true;phase=card.ReflectionPhase;Pump(120);assert(card.ReflectionPhase==phase,"reduced motion suspends reflection");GlassAnimation.ReducedMotionOverride=false;host.Hide();phase=card.ReflectionPhase;Pump(120);assert(card.ReflectionPhase==phase,"hidden windows suspend glass animation");host.Close();GlassAnimation.Capture=true;
  using var geometry=new Card{Size=new(320,180)};var edge=new GlassEdge();var start=edge.Position(geometry,0);var end=edge.Position(geometry,1);assert(Math.Abs(start.X-end.X)<.01&&Math.Abs(start.Y-end.Y)<.01,"border path loops without a discontinuity");
    var lengths=new List<double>();for(int i=0;i<500;i++){var a=edge.Position(geometry,i/500d);var b=edge.Position(geometry,(i+1)/500d);lengths.Add(Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)));}assert(lengths.Min()/lengths.Max()>.85,"reflection maintains speed through rounded corners");var initial=edge.Phase;edge.Advance(5,false);assert(Math.Abs((edge.Phase-initial+1)%1-.5)<.0001,"reflection completes one circuit in ten seconds");GlassAnimation.ReducedMotionOverride=null;File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"premium-checks.txt"),$"PASS: single moving specular reflection; reduced motion respected, no traffic text repaints, cached blur, idle suspension, seamless closed path. Animation CPU time over 550ms: {consumed.TotalMilliseconds:0}ms (one test card).");
 }
}




