using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.License;
	using StorageEngine = bfbd.UltraRecord.Client.StorageEngine;

	public partial class ConfigurationService
	{
		private static readonly string _licensePath = Path.Combine(Path.GetDirectoryName(Application.StartupPath), "license.sn");

		public object GetLicenseInfo()
		{
			var sn = new SerialNumber();
			var mid = sn.MachineId;
			if (File.Exists(_licensePath))
			{
				string lic = File.ReadAllText(_licensePath);
				sn = SerialNumber.DeSerialize(lic.Substring(8), Global.Config.InstallTime);
				sn.MachineId = mid;
			}

			string type = null;
			if (sn.License == LicenseType.Demo)
				type = "Trial";
			else if (sn.License == LicenseType.Professional)
				type = "Professional";

			List<object> array = new List<object>();
			array.Add(new { name = "Version", value = "2.0.0" });
			array.Add(new { name = "LicenseType", value = type });
			array.Add(new { name = "MachineId", value = sn.MachineId });
			array.Add(new { name = "CreateTime", value = sn.CreateTime > DateTime.MinValue ? sn.CreateTime.ToLongDateString() : null });
			array.Add(new { name = "ExpireTime", value = sn.ExpireTime > DateTime.MinValue ? sn.ExpireTime.ToLongDateString() : null });
			array.Add(new { name = "IsValid", value = sn.IsValid() });
			array.Add(new { name = "Website", value = "<a href='http://www.ultragis.com' target='_blank'>http://www.ultragis.com</a>" });
			return new { total = array.Count, rows = array.ToArray() };
		}

		public bool RegisterLicense(string lic)
		{
			try
			{
				lic = lic.Replace('*', '+').Replace('-', '/').Replace('_', '=');
				var sn = SerialNumber.DeSerialize(lic.Substring(8), Global.Config.InstallTime);
				if (sn.IsValid())
				{
					File.WriteAllText(_licensePath, lic);
					bfbd.Common.Task.PeriodTask.PostMessage("License");
					bfbd.Common.Task.PeriodTask.PostMessage("Configuration");
					return true;
				}
				else
					return false;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); return false; }
		}

		public object[] GetConfigurations()
		{
			List<object> objs = new List<object>();
			var dic = Serialization.ToDictionary(Global.Config);
			foreach (var key in dic.Keys)
			{
				string val = DataConverter.Serialize(dic[key]);
				if (dic[key] != null && dic[key].GetType() == typeof(DateTime))
					val = ((DateTime)dic[key]).ToLongDateString();
				objs.Add(new
				{
					Name = key,
					Value = val,
				});
			}
			long freeSpace = new DriveInfo(Application.StartupPath).AvailableFreeSpace / (1024 * 1024 * 1024);
			objs.Add(new { name = "DiskFree", value = freeSpace - 1 });

			return objs.ToArray();
		}

		[bfbd.MiniWeb.RawParameters("prams")]
		public bool SetConfigurations(System.Collections.Specialized.NameValueCollection prams)
		{
			Dictionary<string, object> dic = new Dictionary<string, object>();
			foreach (string key in prams.AllKeys)
			{
				if (!key.StartsWith("$"))
					dic[key] = prams[key];
			}
			StorageEngine.UpdateConfigurations(dic);
			Global.Config.SetConfigurations(dic);

			bfbd.Common.Task.PeriodTask.PostMessage("AccessPolicy");
			return true;
		}

		public bool SetAdminPassword(string pwd)
		{
			if (!string.IsNullOrEmpty(pwd))
			{
				Dictionary<string, object> dic = new Dictionary<string, object>();
				dic.Add("AdminPassword", Encryption.Encrypt(pwd));
				StorageEngine.UpdateConfigurations(dic);
				Global.Config.SetConfigurations(dic);
				return true;
			}
			return false;
		}
	}
}
