using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Reflection;

namespace bfbd.MiniWeb.Core
{
	using bfbd.Common;
	using bfbd.Common.Data;

	partial class HttpService : IHttpHandler
	{
		private Hashtable _services = Hashtable.Synchronized(new Hashtable());

		public HttpService(params object[] services) 
		{
			if (services == null)
				return;

			foreach(var svc in services)
			{
				string name = svc.GetType().Name;
				var attrs = svc.GetType().GetCustomAttributes(typeof(HttpServiceAttribute), true);
				if (attrs.Length > 0)
					name = (attrs[0] as HttpServiceAttribute).Name;
				_services.Add(name, svc);
			}
		}
	}

	partial class HttpService : IHttpHandler
	{
		/// <summary>
		/// http://{prefix}/{name}.svc
		/// </summary>
		public bool IsMatch(HttpListenerRequest Request, string prefix)
		{
			bool match = false;
			string fpath = Request.GetFilePath(prefix);
			if (fpath != null && fpath.EndsWith(".svc"))
			{
				if (!fpath.TrimStart('/').Contains("/"))
				{
					string name = System.IO.Path.GetFileNameWithoutExtension(fpath);
					match = _services.ContainsKey(name);
				}
			}
			return match;
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
							var prams = MakeInvokeParameters(mi, Request, Response);
							var result = mi.Invoke(svc, prams);
							Response.SendJsonObject(result, Request.CanCompress());
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
			var attrs = mi.GetCustomAttributes(false);
			if (Array.Exists(attrs, a => a is RawRequestAttribute))
			{
				return Array.Exists(attrs, a => a is RawResponseAttribute) ?
					new object[] { Request, Response } :
					new object[] { Request };
			}
			else if (Array.Exists(attrs, a => a is RawParametersAttribute))
			{
				return Array.Exists(attrs, a => a is RawResponseAttribute) ?
						new object[] { Request.GetParameters(), Response } :
						new object[] { Request.GetParameters() };
			}
			else
			{
				List<object> ps = new List<object>();
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
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	class RawRequestAttribute : Attribute { }
	[AttributeUsage(AttributeTargets.Method)]
	class RawResponseAttribute : Attribute { }
	[AttributeUsage(AttributeTargets.Method)]
	class RawParametersAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Class)]
	class HttpServiceAttribute : Attribute
	{
		public HttpServiceAttribute(string name) { Name = name; }

		public string Name { get; set; }
	}
}
