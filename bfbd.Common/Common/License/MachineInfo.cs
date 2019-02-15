using System;
using System.Collections.Generic;
using System.Text;
using System.Management;

namespace bfbd.Common.License
{
	sealed class MachineInfo
	{
		public string GetMachineId(HardwareType hardware)
		{
			List<string> array = new List<string>();
			if ((hardware & HardwareType.Process) > 0)
				array.Add(ProcessId);
			if ((hardware & HardwareType.Board) > 0)
				array.Add(BoardSerial);
			if ((hardware & HardwareType.Bios) > 0)
				array.Add(BiosSerial);
			if ((hardware & HardwareType.Driver) > 0)
				array.Add(DriverModel);
			if ((hardware & HardwareType.Mac) > 0)
				array.Add(MacAddress);
			string machine = string.Join(",", array.ToArray());
			return Encryption.MD5(machine, "bfbd");
		}

		internal string ProcessId { get { return string.Join("|", SelectProperties("Win32_Processor", "ProcessorId")); } }

		internal string BoardSerial { get { return string.Join("|", SelectProperties("Win32_BaseBoard", "SerialNumber")); } }

		internal string BiosSerial { get { return string.Join("|", SelectProperties("Win32_BIOS", "SerialNumber")); } }

		public string DriverModel { get { return GetMainDriverModel(); } }

		internal string MacAddress
		{
			get
			{
				List<string> macs = new List<string>();
				using (var mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
				{
					foreach (ManagementObject mo in mc.GetInstances())
					{
						if ((bool)mo["IPEnabled"])
							macs.Add(mo["MacAddress"].ToString());
					}
				}
				return string.Join(",", macs.ToArray());
			}
		}

		//internal string MacAddress
		//{
		//    get
		//    {
		//        var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
		//        foreach (var adapter in nics)
		//        {
		//            var mac = adapter.GetPhysicalAddress().ToString();
		//            if (!string.IsNullOrEmpty(mac) && !mac.StartsWith("00"))
		//                return mac;
		//        }
		//        return null;
		//    }
		//}

		private string[] SelectProperties(string clsName, string propName)
		{
			List<string> props = new List<string>();
			try
			{
				string wql = string.Format("SELECT * FROM {0}", clsName);
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", wql))
				{
					foreach (var mo in searcher.Get())
						props.Add((mo[propName] ?? "").ToString());
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return props.ToArray();
		}

		private string GetMainDriverModel()
		{
			string model = null;
			try
			{
				string wql = string.Format("SELECT * FROM {0}", @"Win32_DiskDrive");
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", wql))
				{
					foreach (var mo in searcher.Get())
					{
						string deviceId = (mo["DeviceID"]).ToString();
						if (deviceId != null && deviceId.EndsWith("PHYSICALDRIVE0"))
						{
							model = (mo["Model"] ?? "").ToString();
							break;
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return model;
		}
	}

	[Flags]
	public enum HardwareType
	{
		Process = 0x0001,
		Board = 0x0002,
		Driver = 0x0004,
		Mac = 0x0008,
		Bios = 0x0010,
	}
}
