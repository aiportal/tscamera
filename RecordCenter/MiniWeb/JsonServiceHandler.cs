using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Reflection;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	using bfbd.Common;

	partial class JsonServiceHandler : IHttpHandler
	{
		private Hashtable _services = Hashtable.Synchronized(new Hashtable());

		public JsonServiceHandler(object[] svcObjects = null)
		{
			if (svcObjects != null)
			{
				foreach (var svc in svcObjects)
					_services[svc.GetType().Name] = svc;
			}
		}

		public void RegisterService(string svcName, object svcObject)
		{
			Debug.Assert(!string.IsNullOrEmpty(svcName));
			Debug.Assert(svcObject != null);

			_services[svcName] = svcObject;
		}

		public bool IsMatch(string url)
		{
			bool match = false;
			if (url != null && url.EndsWith(".svc"))
			{
				if (!url.TrimStart('/').Contains("/"))
				{
					string name = System.IO.Path.GetFileNameWithoutExtension(url);
					match = _services.ContainsKey(name);
				}
			}
			return match;
		}

		public void ProcessRequest(HttpRequest Request, HttpResponse Response)
		{
			try
			{
				string name = System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath);
				Debug.Assert(_services.ContainsKey(name));
				var svc = _services[name];
				if (Request.QueryString.Count == 0)
				{
					// return proxy script of the service.
					var proxy = new JsonServiceScript(svc);
					string script = proxy.GenerateScript(name);
					Response.SendJsonScript(script, false);
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
							if (Array.Exists(mi.GetCustomAttributes(false), a => a is RawRequestAttribute))
							{
								var result = mi.Invoke(svc, new object[] { Request });
								Response.SendJsonObject(result, Request.CanCompress);
							}
							else if (Array.Exists(mi.GetCustomAttributes(false), a => a is RawParametersAttribute))
							{
								var result = mi.Invoke(svc, new object[] { Request.Parameters });
								Response.SendJsonObject(result, Request.CanCompress);
							}
							else if (Array.Exists(mi.GetCustomAttributes(false), a => a is RawResponseAttribute))
							{
								mi.Invoke(svc, MakeInvokeParameters(mi, Request.Parameters, Response));
								Response.Close();
							}
							else
							{
								// json response.
								var result = mi.Invoke(svc, MakeInvokeParameters(mi, Request.Parameters));
								Response.SendJsonObject(result, Request.CanCompress);
							}
						}
						catch (Exception ex)
						{
							TraceLogger.Instance.WriteLineError(string.Format(@"Service Exception at: {0}::{1}", svc, method));
							TraceLogger.Instance.WriteException(ex.InnerException == null ? ex : ex.InnerException); throw;
						}
					}
					else
						throw new NotImplementedException("Service method not found: " + method);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private object[] MakeInvokeParameters(MethodInfo mi, NameValueCollection prams, HttpResponse Response = null)
		{
			List<object> ps = new List<object>();
			foreach(var p in mi.GetParameters())
			{
				if (prams[p.Name] != null)
					ps.Add(DataConverter.ChangeType(prams[p.Name], p.ParameterType));
				else
					ps.Add(null);
			};
			if (ps.Count > 0 && Response != null)
				ps[0] = Response;
			return ps.ToArray();
		}
	}

	public class RawRequestAttribute : Attribute
	{
		public RawRequestAttribute(string name) { Name = name; }
		public string Name { get; set; }
	}
	public class RawResponseAttribute : Attribute 
	{
		public RawResponseAttribute(string name) { Name = name; }
		public string Name { get; set; }
	}
	public class RawParametersAttribute : Attribute
	{
		public RawParametersAttribute(string name) { Name = name; }
		public string Name { get; set; }
	}
}
