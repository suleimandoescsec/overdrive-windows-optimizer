using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
namespace Overdrive {
 public class SimpleForm:Form {
  readonly Button optimize; readonly Label status; readonly TextBox log; bool busy; readonly bool runAfterElevation;
  public SimpleForm(bool run){runAfterElevation=run;Text="OVERDRIVE";ClientSize=new Size(1050,720);MinimumSize=new Size(1050,720);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(7,10,16);ForeColor=Color.White;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.None;
   var close=new Label{Text="×",Location=new Point(1000,18),Size=new Size(32,32),Font=new Font("Segoe UI",17),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.LightGray,Cursor=Cursors.Hand,AccessibleName="Close"};close.Click+=(s,e)=>Close();Controls.Add(close);close.BringToFront();
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){NativeWindowDrag.ReleaseCapture();NativeWindowDrag.SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};
   var portraitSettings=GazeSettings.Default();portraitSettings.bgMode="photo";portraitSettings.bgOpacity=35;
   var portrait=new ElectricPortrait{Location=new Point(525,60),Size=new Size(480,555),Settings=portraitSettings,HeroLayout=false,CanAnimate=()=>!busy&&WindowState!=FormWindowState.Minimized&&Visible};Controls.Add(portrait);
   var brand=new Label{Text="OVERDRIVE / FREE WINDOWS PROJECT",Location=new Point(52,38),Size=new Size(500,28),ForeColor=Color.FromArgb(168,216,255),Font=new Font("Consolas",11,FontStyle.Bold)};brand.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){NativeWindowDrag.ReleaseCapture();NativeWindowDrag.SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};Controls.Add(brand);
   var title=new Label{Text="KEEP YOUR PC.\nCUT THE BLOAT.",Location=new Point(48,118),Size=new Size(600,150),Font=new Font("Segoe UI",31,FontStyle.Bold),ForeColor=Color.White};Controls.Add(title);title.BringToFront();
   var description=new Label{Text="Press the button to turn off Windows effects and alerts, cut some startup apps, switch the power plan, and delete old temp files.",Location=new Point(52,295),Size=new Size(475,128),ForeColor=Color.FromArgb(190,203,219),Font=new Font("Segoe UI",11)};Controls.Add(description);description.BringToFront();
   optimize=new Button{Text="OPTIMIZE MY PC  →",Location=new Point(52,440),Size=new Size(340,62),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(240,243,242),ForeColor=Color.FromArgb(7,10,16),Font=new Font("Segoe UI",13,FontStyle.Bold),Cursor=Cursors.Hand};optimize.Click+=(s,e)=>Run();Controls.Add(optimize);optimize.BringToFront();close.BringToFront();
   status=new Label{Text="Windows may ask for admin access. Old temp files are deleted for good.",Location=new Point(52,525),Size=new Size(475,76),ForeColor=Color.FromArgb(150,166,185)};Controls.Add(status);status.BringToFront();
   log=new TextBox{Visible=false,Multiline=true};Controls.Add(log);Shown+=(s,e)=>{if(runAfterElevation)BeginInvoke((Action)Run);};FormClosing+=(s,e)=>{if(busy){e.Cancel=true;status.Text="Finishing the current changes. Please wait.";}};
  }
  void Write(string message){if(IsDisposed)return;if(InvokeRequired){BeginInvoke((Action)(()=>Write(message)));return;}log.AppendText(message+Environment.NewLine);}
  void Run(){if(busy)return;if(!Store.Admin){try{Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--optimize"){Verb="runas",UseShellExecute=true});Close();}catch(Exception e){status.Text="Administrator access was not granted: "+e.Message;}return;}
   busy=true;optimize.Enabled=false;status.Text="Optimizing your PC. This can take a few minutes…";
   Task.Run(()=>{var messages=new List<string>();var errors=new List<string>();int changes=0;long cleared=0;Action<string> record=m=>{messages.Add(m);Write(m);};
    try{var profile=Profiles.Apply("Aggressive",record);changes=profile.Data.Changes.Count(x=>x.Status=="Applied");int failures=profile.Data.Changes.Count(x=>x.Status=="Failed");if(failures>0)errors.Add(failures+" Windows settings failed");else record("Windows settings complete.");}catch(Exception e){errors.Add("Windows settings: "+e.Message);}
    try{var tx=new Transaction("One-click startup",record);foreach(var item in Profiles.Startup().Where(x=>new[]{"onedrive","docker desktop","teams"}.Any(s=>(x.Key+" "+x.Value).IndexOf(s,StringComparison.OrdinalIgnoreCase)>=0)).ToArray())try{tx.RemoveStartup(item.Key);}catch(Exception e){errors.Add("Startup "+item.Key+": "+e.Message);}tx.Finish();}catch(Exception e){errors.Add("Startup: "+e.Message);}
    try{string old=Store.Power(),target="8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";string receipt=Path.Combine(Store.History,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-power.receipt.json");Store.Save(receipt,new{Machine=Environment.MachineName,Sid=Store.Sid,Previous=old,Applied=target});Store.SetPower(target);if(!Store.Power().Equals(target,StringComparison.OrdinalIgnoreCase))throw new Exception("Power plan readback did not match");record("High performance power plan enabled; previous plan saved.");}catch(Exception e){errors.Add("Power plan: "+e.Message);}
    try{cleared=Cleaner.Delete(Cleaner.Scan(Cleaner.Roots()),record);record("Temporary files removed: "+(cleared/1048576.0).ToString("N1")+" MB.");}catch(Exception e){errors.Add("Temporary files: "+e.Message);}
    string result=errors.Count==0?"YOU'RE DONE. TRY YOUR GAME.\n"+changes+" settings applied · "+(cleared/1048576.0).ToString("N1")+" MB cleared. Some changes need a restart.":"Finished with "+errors.Count+" issue(s). See the saved report for details.";try{Store.Save(Path.Combine(Store.Root,"last-one-click-result.json"),new{Time=DateTime.UtcNow.ToString("o"),Result=result,Details=messages,Errors=errors});}catch(Exception e){result="Finished, but could not save report: "+e.Message;}
    if(!IsDisposed)BeginInvoke((Action)(()=>{status.Text=result;busy=false;optimize.Text="OPTIMIZE AGAIN  →";optimize.Enabled=true;}));
   });
  }
  public void Render(string path){Show();Application.DoEvents();using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(Point.Empty,Size));b.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Close();}
  public void Smoke(string path){Show();Application.DoEvents();if(Controls.OfType<Button>().Count()!=1||optimize.Text!="OPTIMIZE MY PC  →")throw new Exception("Expected one optimize button");Store.Save(path,new{success=true,checks=1,passed=new[]{"Single screen has one optimize button"},note="Read-only; did not apply changes."});Close();}
 }
}
