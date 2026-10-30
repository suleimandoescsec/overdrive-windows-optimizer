using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
namespace Overdrive {
 public class ThankYou:Form {
  public const string SupportUrl="https://donate.stripe.com/dRm8wP5ChgHy0EZ3GDaIM04";
  public ThankYou(int count,long bytes,bool openBrowser){Text="OVERDRIVE — Thank you";ClientSize=new Size(530,465);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Color.FromArgb(10,13,19);ForeColor=Color.White;Font=new Font("Segoe UI",10);
   var chip=new Chip{Location=new Point(197,23),Size=new Size(136,120)};Controls.Add(chip);
   Controls.Add(new Label{Text="YOU'RE ALL SET.",TextAlign=ContentAlignment.MiddleCenter,Location=new Point(20,155),Size=new Size(490,47),Font=new Font("Segoe UI",22,FontStyle.Bold)});
   Controls.Add(new Label{Text=count+" settings applied  ·  "+(bytes/1048576.0).ToString("N1")+" MB cleared",TextAlign=ContentAlignment.MiddleCenter,Location=new Point(28,211),Size=new Size(474,30),ForeColor=Color.FromArgb(168,216,255)});
   Controls.Add(new Label{Text="Thanks for giving OVERDRIVE a shot.\nIf it helped, you can support the project.\nAny amount helps. No pressure. ♡",TextAlign=ContentAlignment.MiddleCenter,Location=new Point(35,252),Size=new Size(460,78),ForeColor=Color.FromArgb(190,203,219)});
   var support=new Button{Text="SUPPORT THE PROJECT  ↗",Location=new Point(95,350),Size=new Size(340,48),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(168,216,255),ForeColor=Color.FromArgb(7,10,16),Font=new Font("Segoe UI",10,FontStyle.Bold)};support.Click+=(s,e)=>OpenSupport();Controls.Add(support);
   var later=new LinkLabel{Text="Maybe later",TextAlign=ContentAlignment.MiddleCenter,Location=new Point(185,414),Size=new Size(160,27),LinkColor=Color.FromArgb(155,171,187)};later.Click+=(s,e)=>Close();Controls.Add(later);
   if(openBrowser)Shown+=(s,e)=>BeginInvoke((Action)OpenSupport);
  }
  void OpenSupport(){try{Process.Start(new ProcessStartInfo(SupportUrl){UseShellExecute=true});}catch{MessageBox.Show("Could not open the support page. You can still close this window.","OVERDRIVE");}}
 }
 public class Chip:Control {
  public Chip(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);BackColor=Color.FromArgb(10,13,19);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var pin=new Pen(Color.FromArgb(96,151,190),4)){for(int i=0;i<4;i++){int x=40+i*18,y=35+i*16;g.DrawLine(pin,x,9,x,25);g.DrawLine(pin,x,94,x,110);g.DrawLine(pin,16,y,31,y);g.DrawLine(pin,105,y,120,y);}}using(var body=new SolidBrush(Color.FromArgb(168,216,255)))g.FillRectangle(body,31,23,74,74);using(var ink=new SolidBrush(Color.FromArgb(13,22,31))){g.FillEllipse(ink,47,48,9,12);g.FillEllipse(ink,80,48,9,12);}using(var smile=new Pen(Color.FromArgb(13,22,31),3))g.DrawArc(smile,51,55,34,22,15,150);using(var blush=new SolidBrush(Color.FromArgb(235,154,163))){g.FillEllipse(blush,39,65,10,6);g.FillEllipse(blush,87,65,10,6);}}
 }
}
