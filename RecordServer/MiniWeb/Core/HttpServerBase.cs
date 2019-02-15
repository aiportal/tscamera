using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace bfbd.MiniWeb.Core
{
	using bfbd.Common;

	abstract class HttpServerBase
	{
		private System.Net.HttpListener _listener;

		public virtual void Start()
		{
			try
			{
				Debug.Assert(string.IsNullOrEmpty(Prefix) || Prefix.EndsWith("/"));

				_listener = new HttpListener();
				_listener.IgnoreWriteExceptions = true;
#if DEBUG
				_listener.Prefixes.Add(string.Format("http://localhost:{0}/{1}", Port, Prefix));
#else
				_listener.Prefixes.Add(string.Format("http://+:{0}/{1}", Port, Prefix));
#endif
				
				_listener.Start();
				_listener.BeginGetContext(this.Listener_Request, _listener);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public virtual void Stop()
		{
			if (_listener != null)
			{
				try
				{
					_listener.Stop();
					_listener = null;
				}
				catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			}
		}

		public void RunSync()
		{
			try
			{
				_listener = new System.Net.HttpListener();
				_listener.Prefixes.Add(string.Format("http://+:{0}/{1}", Port, Prefix));
				_listener.Start();

				while (true)
				{
					Call.Execute(() =>
					{
						HttpListenerContext ctx = _listener.GetContext();
						if (ctx != null)
						{
							System.Threading.ThreadPool.QueueUserWorkItem(p =>
								ProcessRequest(ctx)
							);
						}
					});
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		private void Listener_Request(IAsyncResult ar)
		{
			HttpListenerContext ctx = null;
			try
			{
				HttpListener listener = ar.AsyncState as HttpListener;
				ctx = listener.EndGetContext(ar);
				if (listener.IsListening)
					listener.BeginGetContext(this.Listener_Request, listener);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); }

			if (ctx != null)
			{
				try
				{
					if (OnRequest != null)
						OnRequest(ctx.Request);
				}
				catch (Exception) { }

				try
				{
					ProcessRequest(ctx);
				}
				catch (Exception) { }
			}
		}

		protected abstract void ProcessRequest(HttpListenerContext ctx);
		public int Port { get; protected set; }
		public string Prefix { get; protected set; }
		public event Action<HttpListenerRequest> OnRequest;
	}
}
