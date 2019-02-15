using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera.Server
{
	using bfbd.Common;
	using bfbd.MiniWeb.Core;

	class WebServiceCore : IDisposable
	{
		HttpServer _webServer;

		public void Start()
		{
			try
			{
				_webServer = new HttpServer(Global.Config.AdminWebPort, null, "login.htm", "main.htm")
				{
					//Authenticate = this.Authenticate,
					SuppressClicentException = true,
				};
				//_webServer.AccessPolicy = new AccessPolicy()
				{
					//PermitUser = Global.Config.ForbidUsers ? Global.Config.PermitUser : null,
					//PermitAddress = Global.Config.PermitAddressValue
				};

				HttpService svc = new HttpService(
					new bfbd.TSCamera.Web.ConfigurationService()
				);
				_webServer.RegisterHandler(svc);

				HttpRemoting remoting = new HttpRemoting(
					new bfbd.TSCamera.Web.StorageRemoting()
				);
				_webServer.RegisterHandler(remoting);

				//JsonServiceHandler svc = new JsonServiceHandler();
				//svc.RegisterService("DataQuery", new DataQueryService());
				//svc.RegisterService("Configuration", new ConfigurationService());
				//svc.RegisterService("Statistic", new StatisticService());
				//_webServer.RegisterHandler(svc);
				_webServer.Start();
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public void Stop()
		{
			try
			{
				if (_webServer != null)
				{
					_webServer.Stop();
					_webServer = null;
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); }
		}

		public void Dispose()
		{
			this.Stop();
		}
	}
}
