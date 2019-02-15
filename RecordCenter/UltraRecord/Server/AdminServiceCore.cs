using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.MiniWeb;

	class AdminServiceCore : IDisposable
	{
		private HttpServer _webServer;
		
		public void Start()
		{
			try
			{
				_webServer = new HttpServer(Global.Config.AdminWebPort)
				{
					LoginPage = "/login.htm",
					DefaultPage = "/main.htm",
					Authenticate = this.Authenticate,
					SuppressClicentException = true,
				};
				_webServer.AccessPolicy = new AccessPolicy()
				{
					PermitUser = Global.Config.ForbidUsers ? Global.Config.PermitUser : null,
					PermitAddress = Global.Config.PermitAddressValue
				};

				JsonServiceHandler svc = new JsonServiceHandler();
				svc.RegisterService("DataQuery", new DataQueryService());
				svc.RegisterService("Configuration", new ConfigurationService());
				svc.RegisterService("Statistic", new StatisticService());
				_webServer.RegisterHandler(svc);
				_webServer.Start();
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
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
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		public void Dispose()
		{
			this.Stop();
		}

		private bool Authenticate(NameValueCollection parameters)
		{
			var user = parameters["user"];
			var pwd = parameters["pwd"];

			bool valid = false;
			if (user == Global.Config.AdminAccount)
			{
				//var salt = DateTime.Now.Hour.ToString();
				string salt = "bfbd";
				string val = Encryption.MD5(Encryption.Decrypt(Global.Config.AdminPassword), salt).ToLower();
				if (val == pwd)
				{
					valid = true;
				}
				//else if (DateTime.Now.Minute > 5)
				//{
				//    salt = (DateTime.Now.Hour - 1).ToString();
				//    val = Encryption.MD5(Encryption.Decrypt(Global.Config.AdminPassword), salt).ToLower();
				//    valid = (val == pwd);
				//}
			}
			return valid;
		}

		public void UpdateAccessPolicy()
		{
			if (_webServer.AccessPolicy != null)
			{
				_webServer.AccessPolicy.PermitUser = Global.Config.ForbidUsers ? Global.Config.PermitUser : null;
				_webServer.AccessPolicy.PermitAddress = Global.Config.PermitAddressValue;
			}
		}
	}
}
