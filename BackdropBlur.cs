using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace MelavoClient;
static class BackdropBlur {
 // Three full-resolution box convolutions approximate a Gaussian without resampling text.
 public static Bitmap Create(Bitmap source,int radius){
  int width=source.Width,height=source.Height;var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
  using(var graphics=Graphics.FromImage(bitmap))graphics.DrawImageUnscaled(source,0,0);
  var locked=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
  try{var pixels=new int[width*height];var scratch=new int[pixels.Length];Marshal.Copy(locked.Scan0,pixels,0,pixels.Length);
   for(int pass=0;pass<3;pass++){Filter(pixels,scratch,width,height,radius,true);Filter(scratch,pixels,width,height,radius,false);}
   Marshal.Copy(pixels,0,locked.Scan0,pixels.Length);
  }finally{bitmap.UnlockBits(locked);}return bitmap;
 }
 static void Filter(int[] source,int[] destination,int width,int height,int radius,bool horizontal){
  int length=horizontal?width:height,lines=horizontal?height:width,divisor=radius*2+1;
  for(int line=0;line<lines;line++){
   int Offset(int position)=>horizontal?line*width+Math.Clamp(position,0,length-1):Math.Clamp(position,0,length-1)*width+line;
   long red=0,green=0,blue=0;
   void Add(int pixel,int sign){red+=((pixel>>16)&255)*sign;green+=((pixel>>8)&255)*sign;blue+=(pixel&255)*sign;}
   for(int sample=-radius;sample<=radius;sample++)Add(source[Offset(sample)],1);
   for(int position=0;position<length;position++){destination[Offset(position)]=unchecked((int)0xff000000)|((int)(red/divisor)<<16)|((int)(green/divisor)<<8)|(int)(blue/divisor);Add(source[Offset(position-radius)],-1);Add(source[Offset(position+radius+1)],1);}
  }
 }
}
