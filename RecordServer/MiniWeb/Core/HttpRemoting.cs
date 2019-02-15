using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;

namespace bfbd.MiniWeb.Core
{
	using bfbd.Common;

	partial class HttpRemoting : IHttpHandler
	{
		private Hashtable _services = Hashtable.Synchronized(new Hashtable());

		public HttpRemoting(params object[] services) 
		{
			if (services == null)
				return;

			foreach(var svc in services)
			{
				var attrs = svc.GetType().GetCustomAttributes(typeof(HttpRemotingAttribute), true);
				if (attrs.Length > 0)
				{
					string name = (attrs[0] as HttpRemotingAttribute).Name;
					_services.Add(name, svc);
				}
			}
		}

		/// <summary>
		/// http://{prefix}/{name}/{method}
		/// </summary>
		public bool IsMatch(HttpListenerRequest Request, string prefix)
		{
			string fpath = Request.GetFilePath(prefix);
			if (string.IsNullOrEmpty(fpath) || fpath.Contains("/"))
				return false;
			else
				return _services.ContainsKey(fpath);
		}

		public void ProcessRequest(HttpListenerRequest Request, HttpListenerResponse Response, string prefix = null)
		{
			try
			{
				string name = System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath);
				var svc = _services[name];
				if (Request.QueryString.Count == 0)
				{
					// return proxy script of the service.
					string script = ScriptEngine.GenerateServiceScript(name, svc);
					Response.SendJsonScript(script);
				}
				else
				{
					// call method.
					string method = Request.QueryString["$method"] ?? Request.QueryString["$m"];
					MethodInfo mi = svc.GetType().GetMethod(method);
					if (mi != null)
					{
						try
						{
							//var prams = MakeInvokeParameters(mi, Request, Response);
							//var result = mi.Invoke(svc, prams);
							//Response.SendJsonObject(result, Request.CanCompress());
						}
						catch (Exception ex)
						{
							TraceLog.WriteLineError(string.Format(@"Service Exception at: {0}::{1}", svc, method));
							TraceLog.WriteException(ex.InnerException == null ? ex : ex.InnerException); throw;
						}
					}
					else
						throw new NotImplementedException("Service method not found: " + method);
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		private object[] MakeInvokeParameters(MethodInfo mi, HttpListenerRequest Request, HttpListenerResponse Response)
		{
			List<object> ps = new List<object>();
			ps.Add(SerializeEngine.Deserialize(Request.InputStream));

			var prams = Request.GetParameters();
			foreach (var p in mi.GetParameters())
			{
				if (prams[p.Name] != null)
					ps.Add(DataConverter.ChangeType(prams[p.Name], p.ParameterType));
				else
					ps.Add(p.RawDefaultValue == DBNull.Value ? null : p.RawDefaultValue);
			};
			return ps.ToArray();
		}

		public bool Encryption { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class)]
	class HttpRemotingAttribute : Attribute
	{
		public HttpRemotingAttribute(string name) { Name = name; }

		public string Name { get; set; }
	}
}
