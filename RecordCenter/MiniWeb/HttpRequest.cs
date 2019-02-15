using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	public partial class HttpRequest
	{
		private readonly HttpListenerRequest Request;
		public HttpListenerRequest R { get { return Request; } }

		public HttpRequest(HttpListenerRequest requestInner)
		{
			Request = requestInner;
		}

		public static implicit operator HttpListenerRequest(HttpRequest r) { return r.Request; }
		public static implicit operator HttpRequest(HttpListenerRequest r) { return new HttpRequest(r); }

		public Uri Url { get { return Request.Url; } }
		public Uri UrlReferrer { get { return Request.UrlReferrer; } }
		public NameValueCollection QueryString { get { return Request.QueryString; } }
		public string[] AcceptTypes { get { return Request.AcceptTypes; } }
	}
	
	partial class HttpRequest
	{
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;
		public bool IsAuthenticated()
		{
			var authenticated = false;
			var cookie = Request.Cookies["tsa_session"];
			if (cookie != null && cookie.Value != null)
				authenticated = (_cache[cookie.Value] != null);
#if DEBUG
			authenticated = true;
#endif
			return authenticated;
		}

		public bool CanCompress { get { return Array.Exists<string>(Request.AcceptTypes, s => s == "gzip"); } }

		public NameValueCollection Parameters
		{
			get
			{
				NameValueCollection parameters = Request.QueryString;
				foreach (var key in parameters.AllKeys)
					parameters[key] = System.Web.HttpUtility.UrlDecode(parameters[key]);
				if (Request.HasEntityBody)
				{
					string query = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
					var ps = System.Web.HttpUtility.ParseQueryString(query, Encoding.UTF8);
					foreach (string key in ps.AllKeys)
						parameters[key] = System.Web.HttpUtility.UrlDecode(ps[key]);
				}
				return parameters;
			}
		}
	}

}
