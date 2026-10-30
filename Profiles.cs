using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace Overdrive {
 public class Tweak {public string Group,Name,Effect,Tradeoff,Path,ValueName;public object Value;public RegistryValueKind Kind=RegistryValueKind.DWord;public bool Aggressive;public Tweak(string group,string name,string effect,string tradeoff,string path,string key,object value,bool aggressive=false){Group=group;Name=name;Effect=effect;Tradeoff=tradeoff;Path=path;ValueName=key;Value=value;Aggressive=aggressive;if(value is string)Kind=RegistryValueKind.String;}}
 public static class Profiles {
  const string U=@"HKCU\Software\Microsoft\Windows\CurrentVersion\";
  const string P=@"HKCU\Software\Policies\Microsoft\";
  public static List<Tweak> All=new List<Tweak>{
   new Tweak("Appearance","Dark Windows + apps","Dark Windows surfaces","Some apps keep their own theme",U+@"Themes\Personalize","AppsUseLightTheme",0),
   new Tweak("Appearance","Dark shell","Dark taskbar and shell","Appearance only",U+@"Themes\Personalize","SystemUsesLightTheme",0),
   new Tweak("Appearance","No transparency","Less composition work","Opaque surfaces",U+@"Themes\Personalize","EnableTransparency",0),
   new Tweak("Appearance","No taskbar animations","Fewer shell effects","No taskbar transitions",U+@"Explorer\Advanced","TaskbarAnimations",0),
   new Tweak("Appearance","No window transitions","Instant minimize / maximize","Less visual continuity",@"HKCU\Control Panel\Desktop\WindowMetrics","MinAnimate","0"),
   new Tweak("Appearance","No window shadows","Fewer cosmetic effects","Flatter appearance",U+@"Explorer\Advanced","ListviewShadow",0),
   new Tweak("Gaming","Disable Game DVR","Stops background game capture","Xbox capture features unavailable",U+"GameDVR","AppCaptureEnabled",0),
   new Tweak("Gaming","Disable historical capture","Stops historical clips","No retroactive game clips",U+"GameDVR","HistoricalCaptureEnabled",0),
   new Tweak("Gaming","Disable DVR recording","Stops game recording preference","Keep OBS configured separately",@"HKCU\System\GameConfigStore","GameDVR_Enabled",0),
   new Tweak("Gaming","Enable Game Mode","Windows game scheduling preference","Effect depends on workload",@"HKCU\Software\Microsoft\GameBar","AutoGameModeEnabled",1),
   new Tweak("Distractions","No app notifications","No Windows app toast popups","Messages may be missed",U+"PushNotifications","ToastEnabled",0),
   new Tweak("Distractions","No welcome prompts","Less post-update promotion","Welcome prompts disabled",U+"UserProfileEngagement","ScoobeSystemSettingEnabled",0),
   new Tweak("Distractions","No promoted installs","Stops suggested app installs","Recommended app offers disabled",U+"ContentDeliveryManager","SilentInstalledAppsEnabled",0),
   new Tweak("Distractions","No Windows suggestions","Less promotional content","Tips disabled",U+"ContentDeliveryManager","SystemPaneSuggestionsEnabled",0),
   new Tweak("Distractions","No Start web suggestions","Local search emphasis","Search suggestions reduced",P+@"Windows\Explorer","DisableSearchBoxSuggestions",1),
   new Tweak("Background","No Edge startup boost","Less preloaded browser activity","First browser launch may be slower",P+"Edge","StartupBoostEnabled",0),
   new Tweak("Background","No Edge background apps","Browser stops after closing","Browser apps stop in background",P+"Edge","BackgroundModeEnabled",0),
   new Tweak("Background","No Chrome background apps","Browser stops after closing","Browser apps stop in background",@"HKCU\Software\Policies\Google\Chrome","BackgroundModeEnabled",0),
   new Tweak("Background","Edge sleeping tabs","Reclaims inactive-tab resources","Some tabs resume on return; profile support varies",P+"Edge","SleepingTabsEnabled",1),
   new Tweak("Background","Sleep tabs after 5 minutes","Earlier inactive-tab savings","Policy may not apply to personal Microsoft profiles",P+"Edge","SleepingTabsTimeout",300),
   new Tweak("Aggressive","Block packaged background apps","Less background app activity","Mail and other apps refresh when opened",@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy","LetAppsRunInBackground",2,true),
   new Tweak("Aggressive","Disable Widgets","Less optional news/widget activity","Widgets unavailable",@"HKLM\SOFTWARE\Policies\Microsoft\Dsh","AllowNewsAndInterests",0,true),
   new Tweak("Aggressive","No update peer sharing","Stops update upload sharing","Updates download from Microsoft",@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization","DODownloadMode",0,true),
   new Tweak("Aggressive","No automatic app restore","Lighter sign-in workload","Reopen apps manually",@"HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon","RestartApps",0,true),
   new Tweak("Aggressive","No full-window dragging","Less redraw during movement","Only window outline while dragging",@"HKCU\Control Panel\Desktop","DragFullWindows","0",true),
   new Tweak("Aggressive","Icons instead of thumbnails","Less thumbnail generation","Images and videos show file icons",U+@"Explorer\Advanced","IconsOnly",1,true),
   new Tweak("Aggressive","No selection transparency","Plain selection rectangle","Less visual polish",U+@"Explorer\Advanced","ListviewAlphaSelect",0,true),
   new Tweak("Aggressive","No cached taskbar previews","Fewer preview resources","Previews rebuild when needed",@"HKCU\Software\Microsoft\Windows\DWM","AlwaysHibernateThumbnails",0,true),
   new Tweak("Aggressive","Disable Peek","Fewer shell preview effects","No Aero Peek",@"HKCU\Software\Microsoft\Windows\DWM","EnableAeroPeek",0,true),
   new Tweak("Aggressive","No font smoothing","Bare performance appearance","Text can look jagged",@"HKCU\Control Panel\Desktop","FontSmoothing","0",true)
  };
  public static Transaction Apply(string profile,Action<string> log){if(profile!="Responsive"&&profile!="Gaming"&&profile!="Aggressive")throw new Exception("Unknown profile");var tx=new Transaction(profile+" profile",log);foreach(var t in All.Where(t=>(profile=="Aggressive"||!t.Aggressive)&&(profile!="Responsive"||t.Group!="Gaming"))){tx.Registry(t.Path,t.ValueName,t.Value,t.Kind);}tx.Registry(U+@"Explorer\VisualEffects","VisualFXSetting",profile=="Aggressive"?2:3,RegistryValueKind.DWord);foreach(uint action in Native.Actions.Take(profile=="Aggressive"?Native.Actions.Length:10))tx.Motion(action);tx.Finish();Native.RefreshTheme();log("Saved recovery journal: "+tx.FilePath);log("Sign out or restart later for settings that apps cache. Unsupported Windows Home policies may have no effect.");return tx;}
  public static bool AllowedRecovery(Change c){if(c.Kind!="Registry")return true;if(c.Path.StartsWith(@"HKCU\Software\Overdrive\SelfTest\",StringComparison.Ordinal)&&c.Path.Length>55)return true;if(c.Path.Equals(U+@"Explorer\VisualEffects",StringComparison.OrdinalIgnoreCase)&&c.Name=="VisualFXSetting")return c.OldType==null||c.OldType=="DWord";if(c.Path.Equals(U+"Run",StringComparison.OrdinalIgnoreCase))return c.OldType=="String"||c.OldType=="ExpandString";return All.Any(t=>t.Path.Equals(c.Path,StringComparison.OrdinalIgnoreCase)&&t.ValueName==c.Name&&(!c.Existed||t.Kind.ToString()==c.OldType));}
  public static bool ProtectedStartup(string text){string value=text.ToLowerInvariant();return new[]{"rustdesk","tailscale","securityhealth","defender","rtkaud","realtek","anticheat","anti-cheat","easyanticheat","battleye","vgc","synaptics","touchpad"}.Any(value.Contains);}
  public static Dictionary<string,string> Startup(){var items=new Dictionary<string,string>();using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(k!=null)foreach(string n in k.GetValueNames()){string v=Convert.ToString(k.GetValue(n));if(!ProtectedStartup(n+" "+v))items[n]=v;}}return items;}
 }
}
