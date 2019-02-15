using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace bfbd.MiniWeb
{
	static class RequestExtension
	{
		private static System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		public static bool IsAuthenticated(this HttpListenerRequest request, string cookieName)
		{
			var cookie = request.Cookies[cookieName];
			if (cookie != null && cookie.Value != null)
				return (_cache[cookie.Value] != null);
			else
				return false;
		}

		public static bool IsRoot(this HttpListenerRequest request, string prefix)
		{
			var url = request.Url.AbsolutePath;
			if (string.IsNullOrEmpty(prefix))
				return url == "/";
			else
				return (url == "/" + prefix) || (url == "/" + prefix.TrimEnd('/'));
		}

		public static string GetFilePath(this HttpListenerRequest request, string prefix)
		{
			var url = request.Url.AbsolutePath;
			return (prefix == null) ? url: Regex.Replace(url, @"^/" + prefix, "/");
		}

		public static string GetReferrer(this HttpListenerRequest request, string prefix)
		{
			if (request.UrlReferrer == null)
				return null;
			else
			{
				var url = request.UrlReferrer.AbsolutePath;
				return (prefix == null) ? url : Regex.Replace(url, @"^/" + prefix, "/");
			}
		}

		public static bool CanCompress(this HttpListenerRequest request)
		{
			return Array.Exists<string>(request.AcceptTypes, s => s == "gzip");
		}

		public static NameValueCollection GetParameters(this HttpListenerRequest request)
		{
			NameValueCollection parameters = request.QueryString;
			foreach (var key in parameters.AllKeys)
				parameters[key] = System.Web.HttpUtility.UrlDecode(parameters[key]);

			if (request.HasEntityBody)
			{
				string data = string.Empty; 
				using(var sr = new System.IO.StreamReader(request.InputStream))
					data = sr.ReadToEnd();

				var ps = System.Web.HttpUtility.ParseQueryString(data, Encoding.UTF8);
				foreach (string key in ps.AllKeys)
					parameters[key] = System.Web.HttpUtility.UrlDecode(ps[key]);
			}
			return parameters;
		}
	}
}
