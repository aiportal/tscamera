using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	using bfbd.Common;

	partial class HttpServer : HttpServerBase
	{
		private FileStorage _files = new FileStorage();
		private string _login = "/login.htm";
		private string _default = "/main.htm";

		public HttpServer(int port = 88, string prefix = null)
		{
			base.Port = port;
			base.Prefix = prefix;
		}

		protected override void ProcessRequest(HttpListenerContext ctx)
		{
			try
			{
				if (!IsAccessValid(ctx))
				{
					ctx.Response.Abort();
					return;
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

			HttpRequest Request = new HttpRequest(ctx.Request);
			HttpResponse Response = new HttpResponse(ctx.Response);
			try
			{
				// authenticate:
				// if LoginPath, do authenticate.
				//		if authenticate return true, redirect to root page.
				//		if authenticate return false, show login page.
				// if not LoginPath, check authenticate.
				//		if authenticated, show page.
				//		if not authenticated, redirect to LoginPage.

				string fpath = Request.Url.AbsolutePath == "/" ? _default : Request.Url.AbsolutePath;
				string referrer = Request.UrlReferrer == null ? null : Request.UrlReferrer.AbsolutePath;
				TraceLogger.Instance.WriteLineVerbos(string.Format("fpath: {0}, referrer: {1}", fpath, referrer));
				if (fpath == _login || referrer == _login)
				{
					if (Request.QueryString.Count == 0)
					{
						// show login page.
						byte[] bsFile = _files.Extract(fpath);
						if (bsFile != null)
							Response.SendStaticFile(fpath, bsFile);
						else
							Response.SendErrorPage(404, "File not found: " + fpath);
					}
					else
					{
						// authenticate request, return json result.
						bool valid = Authenticate(Request.QueryString);
						Response.SetAuthenticated(valid);
						Response.SendJsonObject(valid, false);
						///? unsupport submit login now.
					}
				}
				else
				{
					if (!Request.IsAuthenticated())
					{
						if (_files.Exists(fpath))
						{
							// if request file, redirect to LoginPage.
							Response.Redirect(_login);
						}
						else
						{
							// if request service, return error info by json.
							IHttpHandler handler = MatchHandler(fpath);
							if (handler != null)
								Response.SendJsonError(401, "not authenticated.");
							else
								Response.Redirect(_login);
						}
					}
					else
					{
						// if request file, show page.
						if (_files.Exists(fpath))
						{
							Response.SendStaticFile(fpath, _files.Extract(fpath));
						}
						else
						{
							// if request service, call service.
							IHttpHandler handler = MatchHandler(fpath);
							if (handler != null)
								handler.ProcessRequest(Request, Response);
							else
								Response.SendErrorPage(404, "File not found: " + fpath);
						}
					}
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				try
				{
					if (!SuppressClicentException)
						Response.SendException(ex);
					else
						Response.SendJsonObject(new { Exception = ex.Message }, false);

					if (OnException != null)
						OnException(Request, ex);
				}
				catch (Exception) { }
			}
			finally
			{
				try { Response.Close(); }
				catch (Exception) { }
			}
		}

		public string LoginPage { get { return _login; } set { _login = value; } }
		public string DefaultPage { get { return _default; } set { _default = value; } }
		public bool SuppressClicentException { get; set; }
		public AuthenticateCallback Authenticate { get; set; }
		public AccessPolicy AccessPolicy { get; set; }

		public event HttpExceptionHandler OnException;
	}

	partial class HttpServer
	{
		private bool IsAccessValid(HttpListenerContext ctx)
		{
			bool valid = true;
			if (AccessPolicy != null)
			{
				if (AccessPolicy.ValidAccess(ctx.Request.RemoteEndPoint))
					ctx.Response.KeepAlive = true;
				else
					valid = false;
			}
			return valid;
		}
	}

	partial class HttpServer
	{
		private List<IHttpHandler> _handlers = new List<IHttpHandler>();

		public void RegisterHandler(IHttpHandler handler)
		{
			_handlers.Add(handler);
		}

		public IHttpHandler MatchHandler(string url)
		{
			IHttpHandler handler = null;
			foreach(var h in _handlers)
			{
				if (h.IsMatch(url))
					handler = h;
			}
			return handler;
		}
	}

	delegate bool AuthenticateCallback(NameValueCollection parameters);

	delegate void HttpExceptionHandler(HttpRequest Response, Exception exception);

	interface IHttpHandler
	{
		bool IsMatch(string url);
		void ProcessRequest(HttpRequest Request, HttpResponse Response);
	}
}
