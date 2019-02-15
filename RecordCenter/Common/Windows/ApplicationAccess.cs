using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.IO;

namespace bfbd.Common.Windows
{
	class ApplicationAccess
	{
		public ApplicationInfo[] GetInstalledApplications()
		{
			List<ApplicationInfo> apps = new List<ApplicationInfo>();
			try
			{
				string wql = @"SELECT * FROM Win32_Product WHERE InstallLocation IS NOT NULL";
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2", wql))
				{
					foreach (var mo in searcher.Get())
					{
						var app = new ApplicationInfo()
						{
							Name = Convert.ToString(mo["Name"]),
							Version = Convert.ToString(mo["Version"]),
							Description = Convert.ToString(mo["Description"]),
							Location = Convert.ToString(mo["InstallLocation"]),
							InstallDate = DateTime.ParseExact(Convert.ToString(mo["InstallDate"]),"yyyyMMdd", null)
						};
						apps.Add(app);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return apps.ToArray();
		}

		public ApplicationInfo GetApplicationInfo(string fpath)
		{
			var fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(fpath);
			return new ApplicationInfo()
			{
				Name = fv.ProductName,
				Version = fv.ProductVersion,
				Description = fv.FileDescription,
				Location = Path.GetDirectoryName(fpath),
				InstallDate = File.GetCreationTime(fpath)
			};
		}
	}

	class ApplicationInfo
	{
		public string Name;
		public string Version;
		public string Description;
		public string Location;
		public DateTime InstallDate;
	}
}
