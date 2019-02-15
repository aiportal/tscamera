using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace bfbd.TSCamera.Server
{
	using bfbd.Common;

	class WinServiceCore : IDisposable
	{
		WebServiceCore _web = null;

		public void Start()
		{
			try
			{
				ChannelServices.RegisterChannel(new TcpChannel(Global.Config.AdminServerPort), false);
				RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotingStorage), "Storage", WellKnownObjectMode.Singleton);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }

			try
			{
				_web = new WebServiceCore();
				_web.Start();
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public void Stop()
		{
			if (_web != null)
			{
				_web.Stop();
				_web = null;
			}
		}

		public void Dispose()
		{
			this.Stop();
		}
	}
}
