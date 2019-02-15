using System;using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.IO;

namespace bfbd.TSCamera
{
	using bfbd.Common;
	using bfbd.Common.Data;

	static class Program
	{
		static Program()
		{
			// set data path
			string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"main.db");
			string connStr = string.Format("Data Source={0};Version=3;Password={{0}};", dbPath);
			Database.AddConnectionSettings("main", connStr, "System.Data.SQLite");
		}

		static void Main(string[] args)
		{
			if (Environment.UserInteractive)
			{
				StartConsole(args);
			}
			else
			{
				StartService(args);
			}
		}

		static void StartConsole(string[] args)
		{
			var service = new bfbd.TSCamera.Server.WinServiceCore();
			service.Start();
			Console.ReadLine();
			service.Stop();
		}

		static void StartService(string[] args)
		{
			// if args[0] is hex, start service by prefex.

			ServiceBase.Run(new ServiceBase[] { });
		}
	}
}
