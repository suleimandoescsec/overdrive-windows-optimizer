using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
namespace Overdrive {
 public partial class MainForm {
  void Pump(int milliseconds){var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();System.Threading.Thread.Sleep(10);}}
  public void Smoke(string output){var checks=new List<string>();Show();ShowPage("Overview");Pump(1200);var portrait=pageHost.Controls[0].Controls.OfType<ElectricPortrait>().First();if(portrait.FramesRendered<2)throw new Exception("Portrait animation did not advance");checks.Add("Portrait animation runs while visible");sessionPath="smoke-only-no-session-file";long before=portrait.FramesRendered;Pump(500);if(portrait.FramesRendered!=before)throw new Exception("Portrait did not pause for focus mode");checks.Add("Portrait pauses during focus session");sessionPath=null;WindowState=FormWindowState.Minimized;before=portrait.FramesRendered;Pump(400);if(portrait.FramesRendered!=before)throw new Exception("Portrait did not pause when minimized");checks.Add("Portrait pauses while minimized");WindowState=FormWindowState.Normal;Pump(100);
   foreach(string page in new[]{"Profiles","Focus session","Optional apps","Startup","Cleanup","FPS comparison","Recovery","Your look","Overview"}){ShowPage(page);var waiting=Stopwatch.StartNew();while(busy&&waiting.ElapsedMilliseconds<25000)Pump(20);if(busy)throw new Exception("UI read-only check timed out: "+page);Pump(80);if(pageHost.Controls.Count!=1)throw new Exception("Missing UI page: "+page);checks.Add("Page opened: "+page);}
   var layout=(TableLayoutPanel)pageHost.Parent;var toggle=log.Parent.Controls.OfType<Button>().First();toggle.PerformClick();Pump(30);if(layout.RowStyles[2].Height<100)throw new Exception("Activity log did not expand");checks.Add("Activity log expands");toggle.PerformClick();Pump(30);if(layout.RowStyles[2].Height>40)throw new Exception("Activity log did not collapse");checks.Add("Activity log collapses");
   var current=Process.GetCurrentProcess();current.Refresh();var cpuBefore=current.TotalProcessorTime;var wall=Stopwatch.StartNew();Pump(1500);current.Refresh();double cpu=(current.TotalProcessorTime-cpuBefore).TotalMilliseconds/wall.Elapsed.TotalMilliseconds/Environment.ProcessorCount*100;
   Store.Save(output,new{success=true,checks=checks.Count,passed=checks,AnimationEnabledCpuPercentOfMachine=cpu,PrivateMemoryMiB=current.PrivateMemorySize64/1048576.0,Note="Short local UI sample, not a gaming benchmark. No optimization profiles, app removals or focus-close actions applied."});Close();
  }
 }
}
