using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;

namespace bfbd.TSCamera.Web
{
	using bfbd.Common;
	using bfbd.Common.Data;
	using bfbd.Common.License;

	[bfbd.MiniWeb.Core.HttpService("Configuration")]
	public partial class ConfigurationService
	{
		public object GetLicenseInfo()
		{
			string lic = Database.Invoke(db => db.GetConfiguration("LicenseKey"));
			var sn = new SerialNumber();
			if (!string.IsNullOrEmpty(lic))
			{
				Call.Execute(() =>
				{
					sn = SerialNumber.DeSerialize(lic.Substring(8), "Monkey", Global.Config.InstallTime);
				});
			}

			List<object> array = new List<object>();
			array.Add(new { name = "Version", value = "2.0.0" });
			array.Add(new { name = "LicenseType", value = sn.LicenseType.ToString() });
			array.Add(new { name = "MachineId", value = sn.MachineId });
			array.Add(new { name = "CreateTime", value = sn.CreateTime > DateTime.MinValue ? sn.CreateTime.ToLongDateString() : null });
			array.Add(new { name = "ExpireTime", value = sn.ExpireTime > DateTime.MinValue ? sn.ExpireTime.ToLongDateString() : null });
			array.Add(new { name = "IsValid", value = sn.IsValid() });
			array.Add(new { name = "Website", value = "<a href='http://www.ultragis.com' target='_blank'>http://www.ultragis.com</a>" });
			array.Add(new { name = "Support", value = "<a href='mailto:support@ultragis.com' target='_blank'>support@ultragis.com</a>" });
			return new { total = array.Count, rows = array.ToArray() };
		}

		public bool RegisterLicense(string lic)
		{
			try
			{
				lic = lic.Replace('*', '+').Replace('-', '/').Replace('_', '=');
				var sn = SerialNumber.DeSerialize(lic.Substring(8), "Monkey", Global.Config.InstallTime);
				if (sn.IsValid())
				{
					Database.Invoke(db => db.SetConfiguration("LicenseKey", lic));
					bfbd.Common.Tasks.PeriodTask.PostMessage("License");
					bfbd.Common.Tasks.PeriodTask.PostMessage("Configuration");
					return true;
				}
				else
					return false;
			}
			catch (Exception ex) { TraceLog.WriteException(ex); return false; }
		}

		public object[] GetConfigurations()
		{
			List<object> objs = new List<object>();
			var dic = DataConverter.ToDictionary(Global.Config, false);
			foreach (var key in dic.Keys)
			{
				string val = DataConverter.Convert<string>(dic[key]);
				if (dic[key] != null && dic[key].GetType() == typeof(DateTime))
					val = ((DateTime)dic[key]).ToLongDateString();
				objs.Add(new
				{
					Name = key,
					Value = val,
				});
			}
			long freeSpace = new DriveInfo(AppDomain.CurrentDomain.BaseDirectory).AvailableFreeSpace / (1024 * 1024 * 1024);
			objs.Add(new { name = "DiskFree", value = freeSpace - 1 });

			return objs.ToArray();
		}

		[bfbd.MiniWeb.Core.RawParameters()]
		public bool SetConfigurations(NameValueCollection prams)
		{
			Dictionary<string, object> dic = new Dictionary<string, object>();
			foreach (string key in prams.AllKeys)
			{
				if (!key.StartsWith("$"))
					dic[key] = prams[key];
			}

			Database.Invoke(db => db.SetConfigurations(dic));
			Global.Config.SetConfigurations(dic, false);
			bfbd.Common.Tasks.PeriodTask.PostMessage("AccessPolicy");

			return true;
		}

		public bool SetAdminPassword(string pwd)
		{
			if (!string.IsNullOrEmpty(pwd))
			{
				Database.Invoke(db => db.SetConfiguration("AdminPassword", Encryption.Encrypt(pwd)));
				Global.Config.SetConfigurations(new Dictionary<string, object>() { { "AdminPassword", Encryption.Encrypt(pwd) } });
				return true;
			}
			return false;
		}
	}

	#region ConfigurationExtension

	static class ConfigurationExtension
	{
		internal static string GetConfiguration(this Database db, string name)
		{
			return db.SelectSingle<string>("SystemConfig", new { Subject = "Global", ItemName = name }, "ItemValue");
		}

		internal static bool SetConfiguration(this Database db, string name, string value)
		{
			return db.InsertOrUpdate("SystemConfig",
				new { Subject = "Global", ItemName = name, ItemValue = value },
				new { Subject = "Global", ItemName = name }) > 0;
		}

		internal static Dictionary<string, string> GetConfigurations(this Database db)
		{
			return db.SelectDictionary<string, string>("SystemConfig", new { Subject = "Global" }, "ItemName", "ItemValue");
		}

		internal static bool SetConfigurations(this Database db, Dictionary<string, object> dic)
		{
			foreach (string key in dic.Keys)
			{
				db.InsertOrUpdate("SystemConfig",
					new { ItemName = key, ItemValue = dic[key] },
					new { ItemName = key });
			}
			return true;
		}
	}

	#endregion ConfigurationExtension
}
