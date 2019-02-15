using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Security.AccessControl;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace bfbd.UltraRecord
{
	using bfbd.UltraRecord.Core;

	static class Program
	{
		//[System.Diagnostics.Conditional("DEBUG")]
		static void ConsoleStart()
		{
			//TestImageCompress();
			//return;

			// set install time
			bfbd.UltraRecord.Client.StorageEngine.WriteInstallationInfo();
			var config = new bfbd.UltraRecord.Client.LocalStorage().GetConfigurations();
			bfbd.UltraRecord.Global.Config.SetConfigurations(config);

			// debug runtime
			{
				//string dbPath = @"D:\Program Files\Terminal Camera\Data\main.rdb";
				//string connStr = string.Format("Data Source={0};Version=3;Password={{0}};", dbPath);
				//bfbd.Common.Database.AddConnectionSettings("main", connStr, "System.Data.SQLite");
			}

			var rcd = new bfbd.UltraRecord.Client.RecordServiceCore();
			rcd.Start();
			Application.Run();
			rcd.Stop();
		}

		static void TestImageCompress()
		{
			string dbPath = @"D:\Program Files\Terminal Camera\Data\main.rdb";
			//string dbPath = @"D:\MyWork\UltraRecord\bin\Debug\Demo\main.rdb";
			string connStr = string.Format("Data Source={0};Version=3;Password={{0}};", dbPath);
			bfbd.Common.Database.AddConnectionSettings("main", connStr, "System.Data.SQLite");

			//var demo = new bfbd.UltraRecord.Demo.ImageCompressDemo(@"D:\Program Files\Terminal Camera\Data\2013\1104");
			//demo.DumpOriginImages("cc336e64e09d4f398f03ca6ae86ddb8c");
			//bfbd.UltraRecord.Client.CompressionDemo.TransparentCompress("48d3d1af83fb4970a9749b4c69fd8e05");
		}

		static void Main(string[] args)
		{
			{
				// set data path
				string dbPath = Path.Combine(Path.GetDirectoryName(Application.StartupPath), @"Data\main.rdb");
				string connStr = string.Format("Data Source={0};Version=3;Password={{0}};", dbPath);
				bfbd.Common.Database.AddConnectionSettings("main", connStr, "System.Data.SQLite");
			}

			if (!Environment.UserInteractive)
			{
				StartService(args);
			}
			else
			{
				if (args.Length > 0)
					StartInstaller(args);
				else
					ConsoleStart();
			}
		}

		private static void StartService(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += (o, ev) => { bfbd.TraceLogger.Instance.WriteException(ev.ExceptionObject as Exception); };

			ServiceBase.Run(new ServiceBase[] { 
				new bfbd.UltraRecord.Client.RecordService() 
			});
		}

		private static void StartInstaller(string[] args)
		{
			switch (args[0])
			{
				case "/i":
				case "/install":
						Install(args);
					break;
				case "/u":
				case "/uninstall":
						Uninstall(args);
					break;
				case "/r":
				case "/repair":
						// repaire service installation.
					break;
			}
		}

		private static void Install(string[] args)
		{
			try
			{
				// set install time.
				bfbd.UltraRecord.Client.StorageEngine.WriteInstallationInfo();

				// install service.
				new bfbd.UltraRecord.Client.ServiceInstaller().Install(args);
				bfbd.UltraRecord.Client.ServiceInstaller.SetDirectoryPermission();

				// start service.
				new System.ServiceProcess.ServiceController("RecordService").Start();

				// if win 2008, open firewall
				if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major == 6)
					OpenFirewallPort("RecordAdmin", bfbd.UltraRecord.Global.Config.AdminWebPort);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message);
				throw;
			}
		}

		private static void Uninstall(string[] args)
		{
			try
			{
				// check admin password.
				//string pwd = args.Length > 1 ? args[1] : null;
				//if (bfbd.Common.Encryption.Encrypt(pwd) == bfbd.UltraRecord.Global.Config.AdminPassword)
				{
					// uninstall.
					new bfbd.UltraRecord.Client.ServiceInstaller().Uninstall(args);

					// kill agents.
					try { foreach (var p in System.Diagnostics.Process.GetProcessesByName("rcda")) p.Kill(); }
					catch (Exception) { }

					//try
					//{
					//    Directory.Delete(bfbd.UltraRecord.Client.LocalStorage.DataPath, true);
					//    Directory.Delete(bfbd.UltraRecord.Core.CacheManager.CachePath, true);
					//}
					//catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
				}
				//else
				//MessageBox.Show(null, "Incorrect password.", "Terminal Camera");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message);
				throw;
			}
		}

		private static void OpenFirewallPort(string name, int port)
		{
			try
			{
				//Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
				//NetFwTypeLib.INetFwPolicy2 fwPolicy2 = (NetFwTypeLib.INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
				//var currentProfiles = fwPolicy2.CurrentProfileTypes;
				NetFwTypeLib.INetFwRule2 rule = (NetFwTypeLib.INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
				rule.Name = name;
				rule.Enabled = true;
				rule.Protocol = 6; // TCP
				rule.LocalPorts = port.ToString();
				//rule.Profiles = currentProfiles;

				NetFwTypeLib.INetFwPolicy2 policy = (NetFwTypeLib.INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
				rule.Profiles = policy.CurrentProfileTypes;
				policy.Rules.Add(rule);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}
		//private static void _OpenFirewallPort(string name, int port)
		//{
		//    try
		//    {
		//        string cmd = string.Format(@"netsh advfirewall firewall add rule name=""{0}"" protocol=TCP dir=in localport={1} action=allow", name, port);

		//        var p = new System.Diagnostics.Process();
		//        p.StartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe")
		//        {
		//            UseShellExecute = false,
		//            CreateNoWindow = true,
		//            RedirectStandardInput = true,
		//            RedirectStandardOutput = true,
		//        };
		//        p.Start();
		//        p.StandardInput.WriteLine(cmd + Environment.NewLine);
		//        string result = p.StandardOutput.ReadToEnd();
		//    }
		//    catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		//}
	}
}
