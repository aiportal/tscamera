using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	public partial class HttpResponse
	{
		private readonly HttpListenerResponse Response;
		public HttpListenerResponse R { get { return Response; } }

		public HttpResponse(HttpListenerResponse responseInner)
		{
			Response = responseInner;
		}

		public static implicit operator HttpListenerResponse(HttpResponse r) { return r.Response; }
		public static implicit operator HttpResponse(HttpListenerResponse r) { return new HttpResponse(r); }

		public void Close() { Response.Close(); }
		public void Redirect(string url) { Response.Redirect(url); }
	}

	partial class HttpResponse
	{
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;
		public void SetAuthenticated(bool valid)
		{
			if (valid)
			{
				string ticket = Guid.NewGuid().ToString("N").ToUpper();
				Cookie cookie = new Cookie("tsa_session", ticket);
				Response.SetCookie(cookie);
				_cache[ticket] = true;
			}
			else
			{
				Response.SetCookie(new Cookie("tsa_session", null));
			}
		}

		public void SendStaticFile(string fpath, byte[] bsFile)
		{
			Debug.Assert(!string.IsNullOrEmpty(fpath));
			Debug.Assert(bsFile != null && bsFile.Length > 0);
			
			SetExpires(0);
			Response.ContentType = ApacheMimeTypes.Get(Path.GetExtension(fpath));
			Response.ContentEncoding = Encoding.UTF8;
			
			SetDebugHeaders();
			if (Response.OutputStream.CanWrite)
			{
				Response.OutputStream.Write(bsFile, 0, bsFile.Length);
				Response.OutputStream.Flush();
			}
			//Response.Close();
		}

		public void SendErrorPage(int errCode, string errMsg)
		{
			Debug.Assert(errCode > 0);

			byte[] bsMsg = Encoding.UTF8.GetBytes(errMsg);
			Response.StatusCode = errCode;
			Response.StatusDescription = errMsg;

			SetExpires(0);
			Response.ContentType = "text/plain";
			Response.ContentEncoding = Encoding.UTF8;

			SetDebugHeaders();
			Response.OutputStream.Write(bsMsg, 0, bsMsg.Length);
			Response.OutputStream.Flush();
			Response.Close();
		}

		public void SendException(Exception ex)
		{
			byte[] bsException = Encoding.UTF8.GetBytes(ex.ToString());

			SetExpires(0);
			Response.ContentType = "text/plain";
			Response.ContentEncoding = Encoding.UTF8;

			SetDebugHeaders();
			Response.OutputStream.Write(bsException, 0, bsException.Length);
			Response.OutputStream.Flush();
		}

		public void SendJsonObject(object result, bool compress = true)
		{
			// json serialize
			byte[] bsJson = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(result));
			if (compress)
			{
				Response.Headers[HttpResponseHeader.ContentEncoding] = "gzip";
				bsJson = bfbd.Common.Compress.GZip(bsJson, true);
			}

			SetExpires(0);
			Response.ContentType = "text/json";
			Response.ContentEncoding = Encoding.UTF8;

			SetDebugHeaders();
			Response.OutputStream.Write(bsJson, 0, bsJson.Length);
			Response.OutputStream.Flush();
			Response.Close();
		}

		public void SendJsonError(int errCode, string errMsg)
		{
			byte[] bsMsg = Encoding.UTF8.GetBytes(errMsg);

			SetExpires(0);
			Response.ContentType = "text/plain";
			Response.ContentEncoding = Encoding.UTF8;

			SetDebugHeaders();
			Response.OutputStream.Write(bsMsg, 0, bsMsg.Length);
			Response.OutputStream.Flush();
			Response.Close();
		}

		public void SendJsonScript(string script, bool compress = true, bool expires = false)
		{
			byte[] bsJson = Encoding.UTF8.GetBytes(script);
			if (compress)
			{
				Response.Headers[HttpResponseHeader.ContentEncoding] = "gzip";
				bsJson = bfbd.Common.Compress.GZip(bsJson, true);
			}

			SetExpires(expires ? 0 : 600);
			Response.ContentType = "text/plain";
			Response.ContentEncoding = Encoding.UTF8;
			
			SetDebugHeaders();
			Response.OutputStream.Write(bsJson, 0, bsJson.Length);
			Response.OutputStream.Flush();
			Response.Close();
		}

		public void SendImage(byte[] bsImage, string fileExtension)
		{
			SetExpires(600);
			Response.ContentType = ApacheMimeTypes.Get(fileExtension);
			Response.ContentEncoding = Encoding.UTF8;
			
			SetDebugHeaders();
			Response.OutputStream.Write(bsImage, 0, bsImage.Length);
			Response.OutputStream.Flush();
			Response.Close();
		}

		private void SetExpires(int minutes)
		{
			if (minutes > 0)
			{
				Response.Headers[HttpResponseHeader.Expires] = DateTime.Now.AddMinutes(minutes).ToString();
			}
			else
			{
				Response.Headers[HttpResponseHeader.Pragma] = "no-cache";
				Response.Headers[HttpResponseHeader.CacheControl] = "no-cache, must-revalidate";
				Response.Headers[HttpResponseHeader.Expires] = DateTime.Now.ToString();
			}
		}

		[Conditional("DEBUG")]
		private void SetDebugHeaders()
		{
			SetExpires(0);
			Response.Headers["Access-Control-Allow-Origin"] = "*";
			if (Response.ContentType == "text/json")
				Response.ContentType = "text/plain";
		}
	}
}
