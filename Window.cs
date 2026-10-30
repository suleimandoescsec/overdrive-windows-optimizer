using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Overdrive {
 static class NativeWindowDrag {
  [DllImport("user32.dll")]public static extern bool ReleaseCapture();
  [DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
 }
 public partial class MainForm {
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg!=0x84||WindowState==FormWindowState.Maximized||m.Result!=new IntPtr(1))return;int raw=unchecked((int)m.LParam.ToInt64());Point p=PointToClient(new Point((short)(raw&0xffff),(short)(raw>>16)));bool l=p.X<6,r=p.X>=ClientSize.Width-6,t=p.Y<6,b=p.Y>=ClientSize.Height-6;int code=t&&l?13:t&&r?14:b&&l?16:b&&r?17:l?10:r?11:t?12:b?15:1;m.Result=new IntPtr(code);}
 }
}
