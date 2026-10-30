using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Overdrive {
 public class TempFile {public string Path,Root;public long Bytes,Modified;}
 public static class Cleaner {
  public static bool Under(string path,string root){string full=System.IO.Path.GetFullPath(path);string normalized=System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar)+System.IO.Path.DirectorySeparatorChar;return full.StartsWith(normalized,StringComparison.OrdinalIgnoreCase);}
  public static bool Reparse(string path){return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;}
  public static bool SafeChain(string path,string root){if(!Under(path,root))return false;string full=System.IO.Path.GetFullPath(path);string limit=System.IO.Path.GetFullPath(root).TrimEnd('\\');try{for(string p=full;!string.IsNullOrEmpty(p);p=System.IO.Path.GetDirectoryName(p)){if(Reparse(p))return false;if(p.Equals(limit,StringComparison.OrdinalIgnoreCase))return true;}}catch{}return false;}
  public static List<TempFile> Scan(IEnumerable<string> roots){var result=new List<TempFile>();DateTime cutoff=DateTime.UtcNow.AddDays(-7);foreach(string raw in roots.Distinct(StringComparer.OrdinalIgnoreCase)){string root=Path.GetFullPath(raw).TrimEnd('\\');if(!Directory.Exists(root)||Reparse(root))continue;var stack=new Stack<string>();stack.Push(root);while(stack.Count>0){string dir=stack.Pop();try{if(!SafeChain(dir+Path.DirectorySeparatorChar+"_",root)){ // A synthetic child cannot be inspected; inspect existing ancestry below.
       string cursor=dir;bool blocked=false;while(true){if(Reparse(cursor)){blocked=true;break;}if(cursor.Equals(root,StringComparison.OrdinalIgnoreCase))break;cursor=Path.GetDirectoryName(cursor);if(string.IsNullOrEmpty(cursor)){blocked=true;break;}}if(blocked)continue;
      }
      foreach(string file in Directory.GetFiles(dir)){try{var f=new FileInfo(file);if((f.Attributes&FileAttributes.ReparsePoint)==0&&f.LastWriteTimeUtc<cutoff&&Under(f.FullName,root))result.Add(new TempFile{Path=f.FullName,Root=root,Bytes=f.Length,Modified=f.LastWriteTimeUtc.Ticks});}catch{}}
      foreach(string child in Directory.GetDirectories(dir))if(!Reparse(child))stack.Push(child);
     }catch{}}}return result;}
  public static bool AllowedTempRoot(string path){string full=Path.GetFullPath(path).TrimEnd('\\');if(full.Equals(Path.GetPathRoot(full).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))return false;string name=Path.GetFileName(full);return name.Equals("Temp",StringComparison.OrdinalIgnoreCase)||name.Equals("Tmp",StringComparison.OrdinalIgnoreCase);}
  public static string[] Roots(){var roots=new List<string>{Path.GetTempPath(),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp")};if(Store.Admin)roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Temp"));return roots.Where(AllowedTempRoot).ToArray();}
  public static long Delete(List<TempFile> scan,Action<string> log){long removed=0;int count=0,skipped=0;var allowed=Roots().Select(Path.GetFullPath).ToArray();foreach(var item in scan){try{if(!allowed.Any(r=>r.TrimEnd('\\').Equals(item.Root.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))||!SafeChain(item.Path,item.Root))throw new Exception("Path no longer safe");var f=new FileInfo(item.Path);if(f.Length!=item.Bytes||f.LastWriteTimeUtc.Ticks!=item.Modified||f.LastWriteTimeUtc>=DateTime.UtcNow.AddDays(-7))throw new Exception("File changed since scan");f.Delete();removed+=item.Bytes;count++;}catch{skipped++;}}log("Deleted "+count+" old temporary files ("+(removed/1048576.0).ToString("N1")+" MB). Skipped "+skipped+" locked, changed or unsafe files.");return removed;}
 }
 public class OptionalApp {public string Name,PackageFullName,InstallLocation;public override string ToString(){return Name;}}
 public static class Debloat {
  public static readonly HashSet<string> Allowed=new HashSet<string>(new[]{"Microsoft.BingNews","Microsoft.BingWeather","Microsoft.MicrosoftSolitaireCollection","Clipchamp.Clipchamp","Microsoft.GetHelp","Microsoft.Getstarted","Microsoft.WindowsFeedbackHub","Microsoft.YourPhone","Microsoft.People","Microsoft.WindowsMaps","Microsoft.ZuneMusic","Microsoft.ZuneVideo","Microsoft.Todos","MicrosoftCorporationII.MicrosoftFamily"},StringComparer.OrdinalIgnoreCase);
  public static List<OptionalApp> Installed(){string json=Store.PowerShell("ConvertTo-Json -Compress -InputObject @((Get-AppxPackage | Where-Object { $_.Name -in @("+string.Join(",",Allowed.Select(x=>"'"+x+"'"))+ ") }) | Select-Object Name,PackageFullName,InstallLocation)");if(string.IsNullOrWhiteSpace(json))return new List<OptionalApp>();return Store.Json().Deserialize<List<OptionalApp>>(json.Trim())??new List<OptionalApp>();}
  public static void Remove(List<OptionalApp> apps,Action<string> log){if(apps.Any(a=>!Allowed.Contains(a.Name)))throw new Exception("Package is outside the optional-app allowlist");string receipt=Path.Combine(Store.History,"removed-apps-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".receipt.json");Store.Save(receipt,apps);foreach(var app in apps){try{Store.PowerShell("Get-AppxPackage -Name '"+app.Name+"' | Remove-AppxPackage -ErrorAction Stop");log("Removed for current user: "+app.Name);}catch(Exception e){log("Removal failed: "+app.Name+" — "+e.Message);}}log("Removal receipt saved. Reinstall through Microsoft Store; app data recovery is not guaranteed.");}
 }
}
