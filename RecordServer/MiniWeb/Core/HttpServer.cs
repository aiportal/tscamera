using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;

namespace bfbd.MiniWeb.Core
{
	using bfbd.Common;
	using bfbd.MiniWeb.Security;

	partial class HttpServer : HttpServerBase
	{
		private const string COOKIE_NAME = "MiniWeb";
		private ZipStorage _files = new ZipStorage();
		private string _loginUrl = "login.htm";
		private string _defaultUrl = "main.htm";

		public HttpServer(int port, string prefix, string loginPage, string defaultPage)
		{
			base.Port = port;
			base.Prefix = string.IsNullOrEmpty(prefix) ? null : prefix.Trim('/') + "/";
			_loginUrl = "/" + base.Prefix + loginPage;
			_defaultUrl = "/" + base.Prefix + defaultPage;
		}

		public AccessPolicy AccessPolicy { get; set; }
		///> public TimeSpan FileExpireTime { get; set; }
	}

	partial class HttpServer
	{
		#region IHttpHandler

		private List<IHttpHandler> _handlers = new List<IHttpHandler>();

		public void RegisterHandler(IHttpHandler handler)
		{
			_handlers.Add(handler);
		}

		public IHttpHandler MatchHandler(HttpListenerRequest request)
		{
			IHttpHandler handler = null;
			foreach (var h in _handlers)
			{
				if (h.IsMatch(request, base.Prefix))
					handler = h;
			}
			return handler;
		}

		#endregion IHttpHandler
	}

	partial class HttpServer
	{
		#region ProcessRequest

		protected override void ProcessRequest(HttpListenerContext ctx)
		{
			if (!IsAccessValid(ctx))
			{
				ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				ctx.Response.Close();
				return;
			}

			HttpListenerRequest Request = ctx.Request;
			HttpListenerResponse Response = ctx.Response;
			try
			{
				if (Authenticate == null || Request.IsAuthenticated(COOKIE_NAME))
				{
					if (Request.IsRoot(base.Prefix))
					{
						Response.Redirect(_defaultUrl);
					}
					else
					{
						string fpath = Request.GetFilePath(base.Prefix);
						if (_files.Exists(fpath))
						{
							// if request file, show page.
							Response.SendStaticFile(fpath, _files.Extract(fpath));
						}
						else
						{
							// if request service, call service.
							IHttpHandler handler = MatchHandler(Request);
							if (handler != null)
								handler.ProcessRequest(Request, Response, base.Prefix);
							else
								Response.SendErrorPage(404, "File not found: " + fpath);
						}
					}
				}
				else
				{
					// authenticate
					if (Request.IsRoot(base.Prefix))
					{
						Response.Redirect(_loginUrl);
					}
					else if (Request.QueryString.Count == 0)
					{
						if (IsLoginReferrer(Request))
						{
							// show login page.
							string fpath = Request.GetFilePath(base.Prefix);
							if (_files.Exists(fpath))
								Response.SendStaticFile(fpath, _files.Extract(fpath));
							else
								Response.SendErrorPage(404, "File not found: " + Request.Url);
						}
						else
							Response.Redirect(_loginUrl);
					}
					else
					{
						IHttpHandler handler = MatchHandler(Request);
						if (handler != null)
						{
							if (IsLoginReferrer(Request))
								handler.ProcessRequest(Request, Response, base.Prefix);
							else
								Response.SendException(new Exception("not authenticated."));
						}
						else
						{
							this.Assert(Authenticate != null);
							this.Assert(Request.QueryString.Count > 0);

							// do authenticate.
							bool valid = Authenticate(Request.QueryString);
							Response.SetAuthenticated(valid, COOKIE_NAME);
							Response.SendJsonObject(valid, false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				TraceLog.WriteException(ex);
				try
				{
					Response.SendException(ex, !SuppressClicentException);
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

		private bool IsLoginReferrer(HttpListenerRequest req)
		{
			string url = req.GetFilePath(null);
			if (string.Equals(url, _loginUrl))
				return true;

			string referrer = req.GetReferrer(null);
			if (string.Equals(referrer, _loginUrl))
				return true;

			return false;
		}

		private bool IsAccessValid(HttpListenerContext ctx)
		{
			bool valid = true;
			try
			{
				if (AccessPolicy != null)
				{
					if (AccessPolicy.IsAccessValid(ctx.Request.RemoteEndPoint))
						ctx.Response.KeepAlive = true;
					else
						valid = false;
				}
			}
			catch (Exception ex)
			{
				TraceLog.WriteException(ex);
				valid = false;
			}
			return valid;
		}

		public AuthenticateCallback Authenticate { get; set; }
		public bool SuppressClicentException { get; set; }
		public event HttpExceptionHandler OnException;

		#endregion ProcessRequest
	}

	delegate bool AuthenticateCallback(NameValueCollection parameters);
	delegate void HttpExceptionHandler(HttpListenerRequest Request, Exception exception);

	interface IHttpHandler
	{
		bool IsMatch(HttpListenerRequest Request, string prefix);
		void ProcessRequest(HttpListenerRequest Request, HttpListenerResponse Response, string prefix);
	}
}
