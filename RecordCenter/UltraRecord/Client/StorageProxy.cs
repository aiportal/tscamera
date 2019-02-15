using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace bfbd.UltraRecord.Client
{
	[Obsolete]
	class StorageProxy : IStorage
	{
		private IStorage _localStorage = new bfbd.UltraRecord.Server.LocalStorage();

		public Dictionary<string, object> GetConfigurations()
		{
			try
			{
				if (!string.IsNullOrEmpty(Global.Config.StorageService))
				{
					WebRequest request = HttpWebRequest.Create(Global.Config.StorageService.Replace("{0}", "configuration"));
					WebResponse response = request.GetResponse();
					//response.GetResponseStream();
					
					return null;
				}
				else
				{
					return _localStorage.GetConfigurations();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}
		
		public void WriteSessionInfo(Core.SessionInfo session)
		{
			try
			{
				if (!string.IsNullOrEmpty(Global.Config.StorageService))
				{
					WebRequest request = HttpWebRequest.Create(Global.Config.StorageService.Replace("{0}", "session"));
					request.Method = "POST";
					//request.GetRequestStream();
					WebResponse response = request.GetResponse();
					//response.GetResponseStream();
				}
				else
				{
					_localStorage.WriteSessionInfo(session);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void WriteSnapshot(Core.Snapshot sshot)
		{
			try
			{
				if (!string.IsNullOrEmpty(Global.Config.StorageService))
				{
					WebRequest request = HttpWebRequest.Create(Global.Config.StorageService.Replace("{0}", "snapshot"));
					request.Method = "POST";
					//request.GetRequestStream();
					WebResponse response = request.GetResponse();
					//response.GetResponseStream();
				}
				else
				{
					_localStorage.WriteSnapshot(sshot);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void WriteSessionEnd(string sessionId)
		{
			try
			{
				if (!string.IsNullOrEmpty(Global.Config.StorageService))
				{
					WebRequest request = HttpWebRequest.Create(Global.Config.StorageService.Replace("{0}", "end"));
					request.Method = "POST";
					//request.GetRequestStream();
					WebResponse response = request.GetResponse();
					//response.GetResponseStream();
				}
				else
				{
					_localStorage.WriteSessionEnd(sessionId);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}
	}
}
