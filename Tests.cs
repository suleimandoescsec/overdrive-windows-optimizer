using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Overdrive {
 public static class Tests {
  static List<string> passed=new List<string>();
  static void Check(string name,bool value){if(!value)throw new Exception("FAIL: "+name);passed.Add(name);}
  public static string Run(){string root=Path.Combine(Path.GetTempPath(),"Overdrive-selftest-"+Guid.NewGuid().ToString("N"));Store.TestRoot=root;try{return RunChecks();}finally{Store.TestRoot=null;string full=Path.GetFullPath(root);if(Cleaner.Under(full,Path.GetTempPath())&&Path.GetFileName(full).StartsWith("Overdrive-selftest-",StringComparison.Ordinal)&&Directory.Exists(full))Directory.Delete(full,true);}}
  static string RunChecks(){
   Check("Path boundary accepts child",Cleaner.Under(@"C:\Temp\a.bin",@"C:\Temp"));
   Check("Path boundary rejects sibling prefix",!Cleaner.Under(@"C:\Temp-other\a.bin",@"C:\Temp"));
   Check("Path boundary rejects root itself",!Cleaner.Under(@"C:\Temp",@"C:\Temp"));
   Check("Path normalization rejects traversal",!Cleaner.Under(@"C:\Temp\..\Secrets\a.bin",@"C:\Temp"));
   Check("Cleanup rejects a whole drive as TEMP",!Cleaner.AllowedTempRoot(@"C:\"));
   Check("Cleanup rejects Documents as TEMP",!Cleaner.AllowedTempRoot(@"C:\Users\Example\Documents"));
   Check("Cleanup accepts a named temp folder",Cleaner.AllowedTempRoot(@"D:\Example\Temp"));
   Check("Critical system process protected",Focus.ProtectedName("lsass"));
   Check("Anti-cheat protected",Focus.ProtectedName("EasyAntiCheat_EOS"));
   Check("Development servers protected",Focus.ProtectedName("node"));
   Check("Remote access startup protected",Profiles.ProtectedStartup("Tailscale updater"));
   var p=new AppProcess{Pid=12345,Session=1,Sid="same",Name="Spotify",Path=@"C:\Program Files\Spotify\Spotify.exe"};
   Check("Optional candidate can be selected",Focus.CanClose(p,new HashSet<int>(),1,"same"));
   Check("Target process cannot be closed",!Focus.CanClose(p,new HashSet<int>{12345},1,"same"));
   Check("Other user excluded",!Focus.CanClose(p,new HashSet<int>(),1,"other"));
   Check("Other session excluded",!Focus.CanClose(p,new HashSet<int>(),2,"same"));
   var tree=Focus.ProtectedTree(new List<AppProcess>{new AppProcess{Pid=40001,Parent=40000,Name="Game",Path=@"C:\Games\g.exe"},new AppProcess{Pid=40002,Parent=40001,Name="chrome",Path=@"C:\Browser\chrome.exe"},new AppProcess{Pid=40003,Parent=40000,Name="Spotify",Path=@"C:\Spotify\Spotify.exe"}},40001);
   Check("Game descendants protected",tree.Contains(40002));
   Check("Game ancestors protected",tree.Contains(40000));
   Check("Sibling apps remain candidates",!tree.Contains(40003));
   Check("Unlisted processes excluded",!Focus.CanClose(new AppProcess{Pid=12345,Session=1,Sid="same",Name="UnknownGame",Path=@"C:\Games\game.exe"},new HashSet<int>(),1,"same"));
   var known=Benchmarks.Calculate(Enumerable.Repeat(10.0,99).Concat(new[]{20.0}),"synthetic");
   Check("FPS uses frame interval mean",Math.Abs(known.AverageFps-99.00990099)<0.0001);
   Check("One-percent low uses slowest frames",Math.Abs(known.LowOnePercent-50)<0.001);
   Check("CSV quoted fields parsed",Benchmarks.Csv("\"game, title\",10,\"a\"\"b\"")[2]=="a\"b");
   string sandbox=Path.Combine(Path.GetTempPath(),"Overdrive-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(sandbox);
   try{string old=Path.Combine(sandbox,"old.tmp"),fresh=Path.Combine(sandbox,"fresh.tmp");File.WriteAllText(old,"old");File.SetLastWriteTimeUtc(old,DateTime.UtcNow.AddDays(-10));File.WriteAllText(fresh,"fresh");var scan=Cleaner.Scan(new[]{sandbox});Check("Cleanup scan selects only old files",scan.Count==1&&scan[0].Path==old);Check("Cleanup ancestry accepts real child",Cleaner.SafeChain(old,sandbox));
    string actualRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\');var fi=new FileInfo(old);var item=new TempFile{Path=old,Root=actualRoot,Bytes=fi.Length,Modified=fi.LastWriteTimeUtc.Ticks};Cleaner.Delete(new List<TempFile>{item},s=>{});Check("Cleanup deletes only the previewed old test file",!File.Exists(old)&&File.Exists(fresh));File.WriteAllText(old,"old");File.SetLastWriteTimeUtc(old,DateTime.UtcNow.AddDays(-10));fi=new FileInfo(old);item=new TempFile{Path=old,Root=actualRoot,Bytes=fi.Length,Modified=fi.LastWriteTimeUtc.Ticks};File.AppendAllText(old,"changed");Cleaner.Delete(new List<TempFile>{item},s=>{});Check("Cleanup skips files changed after preview",File.Exists(old));var freshInfo=new FileInfo(fresh);Cleaner.Delete(new List<TempFile>{new TempFile{Path=fresh,Root=actualRoot,Bytes=freshInfo.Length,Modified=freshInfo.LastWriteTimeUtc.Ticks}},s=>{});Check("Cleanup refuses fresh files even when previewed",File.Exists(fresh));
   }
   finally{foreach(string f in Directory.GetFiles(sandbox))File.Delete(f);Directory.Delete(sandbox);}
   string key=@"HKCU\Software\Overdrive\SelfTest\"+Guid.NewGuid().ToString("N");string regSub=key.Substring(5);string journal=null;
   try{using(var reg=Transaction.Open(key,true))reg.SetValue("Existing",7,RegistryValueKind.DWord);var tx=new Transaction("Self-test isolated registry",s=>{});journal=tx.FilePath;tx.Registry(key,"Existing",9,RegistryValueKind.DWord);tx.Registry(key,"New",1,RegistryValueKind.DWord);tx.Finish();Check("Journal created before changes",File.Exists(journal)&&Store.Load<Journal>(journal).Changes.Count==2);Transaction.Restore(journal,s=>{});using(var reg=Transaction.Open(key,false)){Check("Undo restores original typed value",(int)reg.GetValue("Existing")==7);Check("Undo removes newly-created value",!reg.GetValueNames().Contains("New"));}
    var tx2=new Transaction("Self-test conflict",s=>{});tx2.Registry(key,"Existing",9,RegistryValueKind.DWord);tx2.Finish();using(var reg=Transaction.Open(key,true))reg.SetValue("Existing",11,RegistryValueKind.DWord);Transaction.Restore(tx2.FilePath,s=>{});using(var reg=Transaction.Open(key,false))Check("Undo preserves a newer user value",(int)reg.GetValue("Existing")==11);File.Delete(tx2.FilePath);if(File.Exists(tx2.FilePath+".previous"))File.Delete(tx2.FilePath+".previous");
   }finally{if(regSub.StartsWith(@"Software\Overdrive\SelfTest\",StringComparison.Ordinal)&&regSub.Length>40)Registry.CurrentUser.DeleteSubKeyTree(regSub,false);if(journal!=null){File.Delete(journal);if(File.Exists(journal+".previous"))File.Delete(journal+".previous");}}
   Check("Chrome policy hive is correct",Profiles.All.First(t=>t.Name=="No Chrome background apps").Path==@"HKCU\Software\Policies\Google\Chrome");
   Check("Recovery rejects arbitrary machine keys",!Profiles.AllowedRecovery(new Change{Kind="Registry",Path=@"HKLM\Software\Unreviewed",Name="Value",OldType="DWord"}));
   Check("Motion controls include font smoothing",Native.Actions.Contains(0x4bu));
   Check("Adaptive plan detects low RAM",Adaptive.Plan(.08,.5,100,20)[2]=="Focus session");
   Check("Adaptive plan detects CPU load",Adaptive.Plan(.5,.5,100,95)[0]=="YOUR CPU IS BUSY");
   Check("Adaptive plan detects low disk space",Adaptive.Plan(.5,.03,12,20)[2]=="Cleanup");
   Check("Adaptive plan avoids unnecessary tweaks",Adaptive.Plan(.5,.5,100,20)[2]=="FPS comparison");
   using(var source=new Bitmap(48,64)){
    using(var g=Graphics.FromImage(source))using(var gradient=new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,48,64),Color.FromArgb(30,80,140),Color.FromArgb(245,190,80),45f))g.FillRectangle(gradient,0,0,48,64);
    foreach(string mode in ElectricGaze.Modes){var settings=GazeSettings.Default();settings.renderMode=mode;using(var frame=ElectricGaze.Render(source,96,128,settings,1.2))Check("Raster mode: "+mode,frame.Width==96&&frame.Height==128);}
    foreach(string blur in new[]{"gaussian","directional","tilt-shift","lens","progressive"}){var settings=GazeSettings.Default();settings.blurType=blur;using(var frame=ElectricGaze.Render(source,48,64,settings,1))Check("Blur mode: "+blur,frame.Width==48);}
    foreach(string fx in GazeSettings.Default().pfx.Keys){var settings=GazeSettings.Default();settings.pfx[fx].enabled=true;settings.pfx[fx].intensity=85;using(var frame=ElectricGaze.Render(source,48,64,settings,1))Check("Post effect: "+fx,frame.Height==64);}
    var defaults=GazeSettings.Default();using(var a=ElectricGaze.Render(source,96,128,defaults,1))using(var b=ElectricGaze.Render(source,96,128,defaults,1))using(var c=ElectricGaze.Render(source,96,128,defaults,2)){
     Check("Raster rendering is deterministic at the same time",ImageHash(a)==ImageHash(b));Check("Shimmer changes the rendered image over time",ImageHash(a)!=ImageHash(c));
    }
    using(var mask=new Bitmap(48,64))using(var stream=new MemoryStream()){
     using(var g=Graphics.FromImage(mask))g.Clear(Color.White);mask.Save(stream,ImageFormat.Png);var settings=GazeSettings.Default();settings.mask.enabled=true;settings.mask.dataUrl="data:image/png;base64,"+Convert.ToBase64String(stream.ToArray());using(var frame=ElectricGaze.Render(source,48,64,settings,0))Check("White reveal mask restores the plain photo",frame.GetPixel(24,32).ToArgb()==source.GetPixel(24,32).ToArgb());
    }
    var glows=GazeSettings.Default();glows.lights.enabled=true;glows.lights.points.Add(new GlowPoint{x=.5,y=.5,radius=.4,intensity=80});using(var image=ElectricGaze.Render(source,48,64,glows,0))Check("Normalized glow point renders",image.Width==48);
   }
   string session=Path.Combine(Store.Root,"watchdog-test.json");string power=Store.Power();
   using(var worker=Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--test-worker"){UseShellExecute=false,CreateNoWindow=true})){
    try{string original=worker.PriorityClass.ToString();worker.PriorityClass=ProcessPriorityClass.AboveNormal;Store.Save(session,new FocusSession{Id=Guid.NewGuid().ToString("N"),OwnerSid=Store.Sid,Machine=Environment.MachineName,Created=DateTime.UtcNow.ToString("o"),HostPid=int.MaxValue,HostStarted=0,TargetPid=worker.Id,TargetStarted=worker.StartTime.ToUniversalTime().Ticks,TargetPath=worker.MainModule.FileName,OldPower=power,NewPower=power,OldPriority=original,NewPriority="AboveNormal",State="Active",Closed=new List<string>()});
     using(var watcher=Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--watch-session \""+session+"\""){UseShellExecute=false,CreateNoWindow=true})){
      bool done=watcher.WaitForExit(20000);if(!done){watcher.Kill();throw new Exception("Watchdog self-test timeout");}Check("Watchdog exits after host disappears",watcher.ExitCode==0);
     }
     Check("Watchdog marks session restored",Store.Load<FocusSession>(session).State=="Restored");worker.Refresh();Check("Watchdog restores test-process priority",worker.PriorityClass.ToString()==original);Check("Watchdog preserves original power plan",Store.Power()==power);
    }finally{if(!worker.HasExited)worker.Kill();}
   }
   return Store.Json().Serialize(new {success=true,checks=passed.Count,passed=passed});
  }
  static long ImageHash(Bitmap image){unchecked{long hash=17;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)hash=hash*31+image.GetPixel(x,y).ToArgb();return hash;}}
 }
}
