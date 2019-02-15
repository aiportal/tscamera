using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.Windows;

	public partial class DataQueryService
	{
		private static Hashtable _users = Hashtable.Synchronized(new Hashtable(10));
		private static Hashtable _progs = Hashtable.Synchronized(new Hashtable(10));
		private static Hashtable _hosts = Hashtable.Synchronized(new Hashtable(10));
		private static Hashtable _drives = Hashtable.Synchronized(new Hashtable(3));

		public object[] GetUsers()
		{
			var users = Database.Execute(db => db.SelectObjects<DomainUser>("SessionInfo", null, "DISTINCT UserName", "Domain"));
			List<object> objs = new List<object>();
			_users.Clear();
			foreach (var user in users)
			{
				_users[user] = null;
				objs.Add(new { Name = user.ToString() });
			}
			return objs.ToArray();
		}

		public object[] GetApplications()
		{
			var apps = Database.Execute(db => db.SelectArray<string>("ApplicationInfo", "DISTINCT ProcessName", null));
			List<object> objs = new List<object>();
			_progs.Clear();
			foreach (var app in apps)
			{
				_progs[app] = null;
				objs.Add(new { Name = app });
			}
			return objs.ToArray();
		}

		public object[] GetHosts()
		{
			var hosts = Database.Execute(db => db.SelectArray<string>("HostInfo", "DISTINCT HostUrl", null));
			List<object> objs = new List<object>();
			_hosts.Clear();
			foreach (var host in hosts)
			{
				_hosts[host] = null;
				objs.Add(new { Name = host });
			}
			return objs.ToArray();
		}

		public object[] GetDrives()
		{
			var drvs = System.IO.DriveInfo.GetDrives();
			List<object> objs = new List<object>();
			_drives.Clear();
			foreach (var drv in drvs)
			{
				string name = drv.Name.TrimEnd('\\');
				_drives[name] = null;
				objs.Add(new { Name = name });
			}
			return objs.ToArray();
		}

		private bool ValidUser(string user)
		{
			return _users.ContainsKey(user);
		}

		private bool ValidProgram(string prog)
		{
			return _progs.ContainsKey(prog);
		}

		private bool ValidHost(string url)
		{
			return _hosts.ContainsKey(url);
		}

		private bool ValidDrive(string drive)
		{
			return _drives.ContainsKey(drive);
		}

		private bool ValidMd5(string str)
		{			
			return str != null && str.Length< 50 && !str.Contains(" ");
		}

		private bool ValidKey(string str)
		{
			return str != null && str.Length < 30 && !str.Contains("'");
		}
	}

	partial class DataQueryService
	{
		static DataQueryService()
		{
			var service = new DataQueryService();
			service.GetUsers();
			service.GetApplications();
			service.GetHosts();
			service.GetDrives();
		}
	}
}
