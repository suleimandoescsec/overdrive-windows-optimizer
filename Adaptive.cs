using System;
using System.Runtime.InteropServices;
namespace Overdrive {
 public static class Adaptive {
  [StructLayout(LayoutKind.Sequential)]struct Clock {public uint low,high;public ulong Value {get{return ((ulong)high<<32)|low;}}}
  [DllImport("kernel32.dll")]static extern bool GetSystemTimes(out Clock idle,out Clock kernel,out Clock user);
  static ulong previousIdle,previousTotal;
  public static double Cpu(){Clock i,k,u;if(!GetSystemTimes(out i,out k,out u))return 0;ulong total=k.Value+u.Value;if(previousTotal==0){previousIdle=i.Value;previousTotal=total;return double.NaN;}ulong elapsed=total-previousTotal,idle=i.Value-previousIdle;previousTotal=total;previousIdle=i.Value;return elapsed==0?0:Math.Max(0,Math.Min(100,100*(1-idle/(double)elapsed)));}
  public static string[] Plan(double availableRatio,double freeDriveRatio,double freeGiB,double cpu){if(availableRatio<.15)return new[]{"RAM IS RUNNING LOW","Close a few background apps before you play.","Focus session"};if(cpu>85)return new[]{"YOUR CPU IS BUSY","Check which apps are running alongside your game.","Focus session"};if(freeGiB<20||freeDriveRatio<.08)return new[]{"YOUR DRIVE NEEDS SPACE","Scan old temp files. Review the list before you delete.","Cleanup"};return new[]{"YOU HAVE ROOM TO PLAY","Test a game first. Keep changes that help your results.","FPS comparison"};}
 }
}
