using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace bfbd.TSCamera
{
	using bfbd.Common;
	using bfbd.TSCamera.Core;

	static class Program
	{
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
			string url = string.Format("tcp://{0}:{1}/Storage", Global.Config.AdminServerAddr, Global.Config.AdminServerPort);
			var storage = Activator.GetObject(typeof(IStorage), url) as IStorage;
			var config = storage.GetConfigurations();			

			//var service = new bfbd.TSCamera.Client.WinServiceCore();
			//service.Start();
			//Console.ReadLine();
			//service.Stop();
		}

		static void StartService(string[] args)
		{
			// if args[0] is hex, start service by prefex.

			ServiceBase.Run(new ServiceBase[] { });
		}
	}
}
