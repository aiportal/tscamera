using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace bfbd.MiniWeb
{
	using JsonConvert = Newtonsoft.Json.JsonConvert;
	using Compress = bfbd.Common.Compress;

	static class ResponseExtension
	{
		private static System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		public static void SetAuthenticated(this HttpListenerResponse resp, bool valid, string cookieName)
		{
			if (valid)
			{
				string ticket = Guid.NewGuid().ToString("N").ToUpper();
				Cookie cookie = new Cookie(cookieName, ticket);
				//cookie.Secure = true;
				resp.SetCookie(cookie);
				_cache[ticket] = true;
			}
			else
			{
				resp.SetCookie(new Cookie(cookieName, null));
			}
		}

		public static void SendErrorPage(this HttpListenerResponse resp, int errCode, string errMsg)
		{
			Debug.Assert(errCode > 0);

			byte[] bsMsg = Encoding.UTF8.GetBytes(errMsg);
			resp.StatusCode = errCode;
			resp.StatusDescription = errMsg;

			SetExpireTime(resp, 0);
			WriteFileContent(resp, bsMsg, "text/plain", false);
		}

		public static void SendException(this HttpListenerResponse resp, Exception ex, bool detail = false)
		{
			if (!detail)
			{
				while (ex.InnerException != null)
					ex = ex.InnerException;
			}
			var obj = new { Exception = (detail ? ex.ToString() : ex.Message) };
			byte[] bsJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));

			SetExpireTime(resp, 0);
			WriteFileContent(resp, bsJson, "text/json", false);
		}

		public static void SendJsonObject(this HttpListenerResponse resp, object result, bool compress = true)
		{
			// json serialize
			byte[] bsJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));

			SetExpireTime(resp, 0);
			WriteFileContent(resp, bsJson, "text/json", compress);
		}

		public static void SendJsonScript(this HttpListenerResponse resp, string script, bool compress = true, int expireMinutes = 24*60)
		{
			byte[] bsJson = Encoding.UTF8.GetBytes(script);

			SetExpireTime(resp, expireMinutes);
			WriteFileContent(resp, bsJson, "text/json", compress);
		}

		public static void SendStaticFile(this HttpListenerResponse resp, string fpath, byte[] bsFile, int expireMinutes = 24*60)
		{
			Debug.Assert(!string.IsNullOrEmpty(fpath));
			Debug.Assert(bsFile != null && bsFile.Length > 0);

			var mime = MimeTypes.Get(Path.GetExtension(fpath));
			SetExpireTime(resp, expireMinutes);
			WriteFileContent(resp, bsFile, mime, false);
		}

		private static void WriteFileContent(HttpListenerResponse resp, byte[] data, string mime, bool compress)
		{
			if (compress)
			{
				resp.Headers[HttpResponseHeader.ContentEncoding] = "gzip";
				data = Compress.GZip(data, true);
			}

			resp.ContentType = mime;
			resp.ContentEncoding = Encoding.UTF8;

			SetDebugMode(resp);
			resp.OutputStream.Write(data, 0, data.Length);
			resp.OutputStream.Flush();
			resp.Close();
		}

		private static void SetExpireTime(HttpListenerResponse resp, int minutes)
		{
			if (minutes == 0)
			{
				resp.Headers[HttpResponseHeader.Pragma] = "no-cache";
				resp.Headers[HttpResponseHeader.CacheControl] = "no-cache, must-revalidate";
			}
			resp.Headers[HttpResponseHeader.Expires] = DateTime.Now.AddMinutes(minutes).ToString();
		}

		[Conditional("DEBUG")]
		private static void SetDebugMode(HttpListenerResponse resp)
		{
			// no cache
			resp.Headers[HttpResponseHeader.Pragma] = "no-cache";
			resp.Headers[HttpResponseHeader.CacheControl] = "no-cache, must-revalidate";
			resp.Headers[HttpResponseHeader.Expires] = DateTime.Now.ToString();

			// allow other site
			resp.Headers["Access-Control-Allow-Origin"] = "*";

			// plain json result
			if (resp.ContentType == "text/json")
				resp.ContentType = "text/plain";
		}
	}
}
