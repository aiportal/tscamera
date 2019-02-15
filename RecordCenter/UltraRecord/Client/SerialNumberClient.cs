using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common.License;

	class SerialNumberClient
	{
		private static readonly string _licensePath = Path.Combine(Path.GetDirectoryName(Application.StartupPath), "serial.lic");

		public static bool UpdateLicense(string lic)
		{
			if (IsLicenseVerify(lic))
			{
				File.WriteAllText(_licensePath, lic);
				bfbd.Common.Task.PeriodTask.PostMessage("License");
				bfbd.Common.Task.PeriodTask.PostMessage("Configuration");
				return true;
			}
			else
				return false;
		}

		public static SerialNumber ParseLicense(string lic)
		{
			SerialNumber sn = new SerialNumber();
			var mask = lic.Substring(0, 8);
			if (mask.StartsWith("d"))	// demo
			{
				int seconds = Convert.ToInt32(mask.Substring(2));	// seconds
				sn = new SerialNumber()
				{
					License = LicenseType.Demo,
					MachineId = Guid.Empty.ToString("n"),
					CreateTime = Global.Config.InstallTime,
					ExpireTime = Global.Config.InstallTime.AddSeconds(seconds)
				};
			}
			else if (mask.StartsWith("s"))	// single
			{
				var mid = new MachineInfo().GetMachineId(HardwareType.Driver);
				var md5 = bfbd.Common.Encryption.MD5(mid, "bfbd");
				ulong val = Convert.ToUInt32(md5.Substring(0, 8), 16);
				val ^= Convert.ToUInt32(md5.Substring(8, 8), 16);
				val ^= Convert.ToUInt32(md5.Substring(16, 8), 16);
				val ^= Convert.ToUInt32(md5.Substring(24, 8), 16);
				bool valid = (mask.Substring(1) == string.Format("{0:X8}", val).Substring(1));

				sn = new SerialNumber()
				{
					License = LicenseType.Single,
					MachineId = Guid.Empty.ToString("n"),
					CreateTime = Global.Config.InstallTime,
					ExpireTime = valid ? Global.Config.InstallTime.AddYears(50) : Global.Config.InstallTime,
				};
			}
			return sn;
		}

		public static bool IsLicenseVerify(string lic)
		{
			Debug.Assert(!string.IsNullOrEmpty(lic));

			try
			{
				var mask = lic.Substring(0, 8);
				if (mask.StartsWith("d"))	// demo
				{
					int seconds = Convert.ToInt32(mask.Substring(2));	// seconds
					return (Global.Config.InstallTime < DateTime.Now.AddSeconds(-seconds));
				}
				else if (mask.StartsWith("s"))	// single
				{
					var mid = new bfbd.Common.License.MachineInfo().GetMachineId(bfbd.Common.License.HardwareType.Driver);
					var md5 = bfbd.Common.Encryption.MD5(mid, "bfbd");
					ulong val = Convert.ToUInt32(md5.Substring(0, 8), 16);
					val ^= Convert.ToUInt32(md5.Substring(8, 8), 16);
					val ^= Convert.ToUInt32(md5.Substring(16, 8), 16);
					val ^= Convert.ToUInt32(md5.Substring(24, 8), 16);
					return (mask.Substring(1) == string.Format("{0:X8}", val).Substring(1));
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				return false;
			}
		}
	}
}
