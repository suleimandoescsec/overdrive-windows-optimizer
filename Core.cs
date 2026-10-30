using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Overdrive {
 public static class Store {
  internal static string TestRoot;
  public static string Root {get {return TestRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Overdrive");}}
  public static string History {get {string p=Path.Combine(Root,"History");Directory.CreateDirectory(p);return p;}}
  public static JavaScriptSerializer Json(){return new JavaScriptSerializer {MaxJsonLength=16777216};}
  public static void Save<T>(string path,T value){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,Json().Serialize(value),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,path+".previous");else File.Move(temp,path);}
  public static T Load<T>(string path){return Json().Deserialize<T>(File.ReadAllText(path));}
  public static bool Admin {get {return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);}}
  public static string Sid {get {return WindowsIdentity.GetCurrent().User.Value;}}
  public static string Run(string exe,string args,int timeout){var si=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};using(var p=Process.Start(si)){var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(timeout)){try{p.Kill();}catch{}throw new TimeoutException(Path.GetFileName(exe)+" timed out");}string text=output.Result;if(p.ExitCode!=0)throw new Exception(error.Result+text);return text;}}
  public static string PowerShell(string script){string code=Convert.ToBase64String(Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; "+script));return Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),"-NoProfile -NonInteractive -EncodedCommand "+code,120000);}
  public static string Power(){var m=System.Text.RegularExpressions.Regex.Match(Run("powercfg.exe","/getactivescheme",10000),@"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}");if(!m.Success)throw new Exception("Cannot read the current power plan");return m.Value;}
  public static void SetPower(string id){Guid parsed;if(!Guid.TryParse(id,out parsed))throw new Exception("Invalid power-plan ID");Run("powercfg.exe","/setactive "+parsed.ToString(),10000);}
 }
 public class MotionValue {public uint GetAction {get;set;}public uint SetAction {get;set;}public int Value {get;set;}public string Name {get;set;}}
 public static class Native {
  [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]static extern bool GetSPI(uint action,uint param,out int val,uint flags);
  [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]static extern bool SetSPI(uint action,uint param,IntPtr val,uint flags);
  [StructLayout(LayoutKind.Sequential)]struct Animation {public uint cb;public int enabled;}
  [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)]static extern bool AnimSPI(uint action,uint param,ref Animation val,uint flags);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessageTimeout(IntPtr h,uint m,UIntPtr w,string s,uint f,uint t,out UIntPtr r);
  public static readonly uint[] Actions={0x1043,0x103f,0x1003,0x1005,0x1007,0x1013,0x1015,0x1017,0x1019,0x49,0x101b,0x1025,0x4b,0x25};
  public static readonly string[] MotionNames={"Client-area animations","UI effects","Menu animations","Combo animations","Smooth list scrolling","Menu fading","Selection fading","Tooltip animations","Tooltip fading","Minimize / maximize","Mouse-pointer shadows","Window shadows","Font smoothing","Full-window dragging"};
  public static int GetMotion(uint set){if(!Actions.Contains(set))throw new Exception("Unsupported motion control");if(set==0x49){var a=new Animation{cb=8};if(!AnimSPI(0x48,8,ref a,0))throw new Exception("Cannot read window animation preference");return a.enabled;}int value;if(!GetSPI(set==0x25?0x26:set-1,0,out value,0))throw new Exception("Cannot read animation preference");return value;}
  public static void SetMotion(uint set,int value){if(!Actions.Contains(set))throw new Exception("Unsupported motion control");bool ok;if(set==0x49){var a=new Animation{cb=8,enabled=value};ok=AnimSPI(set,8,ref a,3);}else if(set==0x4b||set==0x25)ok=SetSPI(set,(uint)value,IntPtr.Zero,3);else ok=SetSPI(set,0,new IntPtr(value),3);if(!ok)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  public static void RefreshTheme(){UIntPtr r;SendMessageTimeout(new IntPtr(0xffff),0x1a,UIntPtr.Zero,"ImmersiveColorSet",2,100,out r);}
  [StructLayout(LayoutKind.Sequential)]public struct Memory {public uint length,load;public ulong total,available,totalPage,availablePage,totalVirtual,availableVirtual,extended;}
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GlobalMemoryStatusEx(ref Memory m);
  public static Memory ReadMemory(){var m=new Memory{length=(uint)Marshal.SizeOf(typeof(Memory))};if(!GlobalMemoryStatusEx(ref m))throw new System.ComponentModel.Win32Exception();return m;}
 }
 public class Change {
  public string Kind {get;set;}public string Path {get;set;}public string Name {get;set;}public bool Existed {get;set;}public string OldType {get;set;}public string OldValue {get;set;}public string NewType {get;set;}public string NewValue {get;set;}public string Status {get;set;}public string Error {get;set;}public uint Action {get;set;}
 }
 public class Journal {
  public string Id {get;set;}public string Label {get;set;}public string Machine {get;set;}public string Sid {get;set;}public string Created {get;set;}public string State {get;set;}public List<Change> Changes {get;set;}
 }
 public class Transaction {
  public Journal Data;public string FilePath;public Action<string> Log;
  public Transaction(string label,Action<string> log){Log=log;Data=new Journal{Id=Guid.NewGuid().ToString("N"),Label=label,Machine=Environment.MachineName,Sid=Store.Sid,Created=DateTime.UtcNow.ToString("o"),State="In progress",Changes=new List<Change>()};FilePath=Path.Combine(Store.History,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Data.Id+".json");Save();}
  void Save(){Store.Save(FilePath,Data);}
  public static RegistryKey Open(string path,bool write){if(path.StartsWith("HKCU\\",StringComparison.OrdinalIgnoreCase))return write?Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path.Substring(5)):Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path.Substring(5));if(path.StartsWith("HKLM\\",StringComparison.OrdinalIgnoreCase))return write?Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path.Substring(5)):Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path.Substring(5));throw new Exception("Unsupported registry hive");}
  public static string Encode(object value,RegistryValueKind kind){if(kind==RegistryValueKind.Binary)return Convert.ToBase64String((byte[])value);if(kind==RegistryValueKind.MultiString)return Store.Json().Serialize((string[])value);return Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);}
  public static object Decode(string value,string type){switch(type){case "DWord":return int.Parse(value,System.Globalization.CultureInfo.InvariantCulture);case "QWord":return long.Parse(value,System.Globalization.CultureInfo.InvariantCulture);case "Binary":return Convert.FromBase64String(value);case "MultiString":return Store.Json().Deserialize<string[]>(value);default:return value;}}
  public void Registry(string path,string name,object value,RegistryValueKind kind){Change c=new Change{Kind="Registry",Path=path,Name=name,NewType=kind.ToString(),NewValue=Encode(value,kind),Status="Planned"};using(var k=Open(path,false)){c.Existed=k!=null&&k.GetValueNames().Contains(name);if(c.Existed){var oldkind=k.GetValueKind(name);c.OldType=oldkind.ToString();c.OldValue=Encode(k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),oldkind);}}Data.Changes.Add(c);Save();try{using(var k=Open(path,true))k.SetValue(name,value,kind);using(var k=Open(path,false)){if(Encode(k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),k.GetValueKind(name))!=c.NewValue)throw new Exception("Readback did not match");}c.Status="Applied";Log("Applied: "+name);}catch(Exception ex){c.Status="Failed";c.Error=ex.Message;Log("Failed: "+name+" — "+ex.Message);}Save();}
  public void RemoveStartup(string name){const string path=@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run";using(var k=Open(path,false)){if(k==null||!k.GetValueNames().Contains(name))return;string command=Convert.ToString(k.GetValue(name));if(Profiles.ProtectedStartup(name+" "+command))throw new Exception("Protected startup item");var kind=k.GetValueKind(name);var c=new Change{Kind="Registry",Path=path,Name=name,Existed=true,OldType=kind.ToString(),OldValue=Encode(k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),kind),NewType="Delete",Status="Planned"};Data.Changes.Add(c);Save();using(var w=Open(path,true))w.DeleteValue(name,false);c.Status="Applied";Save();Log("Startup disabled: "+name);}}
  public void Motion(uint action){var c=new Change{Kind="Motion",Name=Native.MotionNames[Array.IndexOf(Native.Actions,action)],Action=action,OldValue=Native.GetMotion(action).ToString(),NewValue="0",Status="Planned"};Data.Changes.Add(c);Save();try{Native.SetMotion(action,0);if(Native.GetMotion(action)!=0)throw new Exception("Animation readback did not match");c.Status="Applied";Log("Disabled: "+c.Name);}catch(Exception e){c.Status="Failed";c.Error=e.Message;Log("Failed: "+c.Name);}Save();}
  public void Finish(){Data.State=Data.Changes.Any(x=>x.Status=="Failed")?"Completed with failures":"Completed";Save();}
  public static void Restore(string file,Action<string> log){var data=Store.Load<Journal>(file);if(data.Machine!=Environment.MachineName||data.Sid!=Store.Sid)throw new Exception("This backup belongs to a different PC or user");foreach(var c in data.Changes.AsEnumerable().Reverse()){if(c.Status=="Restored")continue;try{if(!Profiles.AllowedRecovery(c))throw new Exception("Backup contains an unsupported registry target");if(c.Kind=="Registry"){using(var k=Open(c.Path,false)){bool exists=k!=null&&k.GetValueNames().Contains(c.Name);bool matches=c.NewType=="Delete"?!exists:exists&&Encode(k.GetValue(c.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),k.GetValueKind(c.Name))==c.NewValue;bool alreadyOld=c.Existed?exists&&k.GetValueKind(c.Name).ToString()==c.OldType&&Encode(k.GetValue(c.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),k.GetValueKind(c.Name))==c.OldValue:!exists;if(!matches){if(alreadyOld){c.Status="Restored";Store.Save(file,data);continue;}throw new Exception("Changed since this backup; skipped to preserve your newer setting");}}using(var k=Open(c.Path,true)){if(c.Existed)k.SetValue(c.Name,Decode(c.OldValue,c.OldType),(RegistryValueKind)Enum.Parse(typeof(RegistryValueKind),c.OldType));else k.DeleteValue(c.Name,false);}}else if(c.Kind=="Motion"){int current=Native.GetMotion(c.Action);if(current!=int.Parse(c.NewValue)&&current!=int.Parse(c.OldValue))throw new Exception("Motion preference changed since backup");Native.SetMotion(c.Action,int.Parse(c.OldValue));}else throw new Exception("Unknown change type");c.Status="Restored";c.Error=null;log("Restored: "+c.Name);}catch(Exception e){c.Status="Restore skipped";c.Error=e.Message;log("Skipped "+c.Name+": "+e.Message);}Store.Save(file,data);}data.State=data.Changes.All(c=>c.Status=="Restored")?"Restored":"Partially restored";Store.Save(file,data);Native.RefreshTheme();}
 }
}
