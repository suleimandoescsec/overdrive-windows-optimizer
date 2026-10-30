using System;
using System.Collections.Generic;
using System.Management;
namespace Overdrive {
 public class GpuInfo {public string Name,Vendor,Driver;}
 public static class Gpu {
  public static string Vendor(string pnp){string id=(pnp??"").ToUpperInvariant();if(id.Contains("VEN_10DE"))return "NVIDIA";if(id.Contains("VEN_1002")||id.Contains("VEN_1022"))return "AMD";if(id.Contains("VEN_8086"))return "Intel";return "Other";}
  public static List<GpuInfo> Read(){var cards=new List<GpuInfo>();using(var q=new ManagementObjectSearcher("SELECT Name,PNPDeviceID,DriverVersion FROM Win32_VideoController"))using(var rows=q.Get())foreach(ManagementObject row in rows){using(row){string name=Convert.ToString(row["Name"]),id=Convert.ToString(row["PNPDeviceID"]);if(string.IsNullOrWhiteSpace(name))continue;cards.Add(new GpuInfo{Name=name,Vendor=Vendor(id),Driver=Convert.ToString(row["DriverVersion"])});}}return cards;}
 }
}
