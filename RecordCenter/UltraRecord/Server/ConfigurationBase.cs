using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;

	partial class ConfigurationService
	{
		public object[] GetSystemUsers()
		{
			var users = new bfbd.Common.Windows.SystemUserAccess().GetUsers();
			List<object> objs = new List<object>();
			foreach (var u in users)
			{
				objs.Add(new
				{
					Name = u.ToString(),
					FullName = u.FullName,
					Desc = u.Description,
					Domain = u.Domain
				});
			}
			if (Global.Config.ADValid)
			{
				try
				{
					var cfg = Global.Config;
					var ada = new bfbd.Common.Windows.ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
					foreach (var item in ada.EnumADItems(bfbd.Common.Windows.ADSchema.User))
					{
						objs.Add(new
						{
							Name = string.Format(@"{0}\{1}", ada.DomainName, item.Name),
							FullName = item.DisplayName,
							Desc = item.Description,
							Domain = ada.DomainName
						});
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			}
			return objs.ToArray();
		}

		public object[] GetSystemGroups()
		{
			var groups = new bfbd.Common.Windows.SystemUserAccess().GetGroups();
			List<object> objs = new List<object>();
			foreach (var g in groups)
			{
				objs.Add(new
				{
					Name = g.Name,
					Desc = g.Description,
					Domain = bfbd.Common.Windows.DomainUser.CurrentDomain
				});
			}
			if (Global.Config.ADValid)
			{
				try
				{
					var cfg = Global.Config;
					var ada = new bfbd.Common.Windows.ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
					foreach (var item in ada.EnumADItems(bfbd.Common.Windows.ADSchema.Group))
					{
						objs.Add(new
						{
							Name = string.Format(@"{0}\{1}", ada.DomainName, item.Name),
							Desc = item.Description,
							Domain = ada.DomainName
						});
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			}
			return objs.ToArray();
		}

		public object[] GetApplications()
		{
			var apps = Database.Execute(db => db.SelectObjects<Program>("ApplicationInfo", new { HasWindow = true },
				"DISTINCT ProcessName", "FileName", "Description"));
			List<object> objs = new List<object>();
			foreach (var app in apps)
			{
				objs.Add(new
				{
					Name = app.ProcessName,
					File = app.FileName,
					Desc = app.Description
				});
			}
			return objs.ToArray();
		}

		public object[] GetHosts()
		{
			var hosts = Database.Execute(db => db.SelectObjects<Host>("HostInfo", null,
				"DISTINCT HostUrl", "HostName", "Description"));
			List<object> objs = new List<object>();
			foreach (var h in hosts)
			{
				objs.Add(new
				{
					Name = h.HostName,
					Url = h.HostUrl,
					Desc = h.Description
				});
			}
			return objs.ToArray();
		}

		public object[] GetSystemInfo()
		{
			List<object> objs = new List<object>();
			{
				var drv = new DriveInfo(Application.StartupPath);
				objs.Add(new { Name = "CurrentTime", Value = DateTime.Now });
				//objs.Add(new { Name = "DiskFree", Value = drv.AvailableFreeSpace / (1024 * 1024 * 1024) });
				//array.Add(new { name = "DiskUsage", value = storage.GetTotalSpace() / (1024 * 1024 * 1024) });
				//array.Add(new { name = "EarliestDate", value = storage.MinSessionDate().ToLongDateString() });
			}
			return objs.ToArray();
		}

		public class Program
		{
			public string ProcessName;
			public string FileName;
			public string Description;
		}

		public class Host
		{
			public string HostUrl;
			public string HostName;
			public string Description;
		}
	}

	partial class ConfigurationService
	{
		public object[] GetADItems()
		{
			List<object> objs = new List<object>();
			if (Global.Config.ADValid)
			{
				try
				{
					var cfg = Global.Config;
					var ad = new bfbd.Common.Windows.ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
					foreach (var item in ad.EnumADItems(bfbd.Common.Windows.ADSchema.All))
					{
						objs.Add(new
						{
							Id = item.Id,
							ParentId = item.ParentId,
							Schema = item.Schema,
							Name = item.Name,
							Caption = item.DisplayName,
							Desc = item.Description,
							Domain = ad.DomainName
						});
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			}
			return objs.ToArray();
		}
	}
}
