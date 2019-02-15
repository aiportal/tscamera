using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace bfbd.MiniWeb
{
	abstract class HttpServerBase
	{
		private System.Net.HttpListener _listener;

		public virtual void Start()
		{
			try
			{
				_listener = new System.Net.HttpListener();
				_listener.Prefixes.Add(string.Format("http://+:{0}/{1}", Port, Prefix));
				_listener.Start();
				_listener.BeginGetContext(this.Listener_Request, _listener);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
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
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public void RunSync()
		{
			try
			{
				_listener = new System.Net.HttpListener();
				_listener.Prefixes.Add(string.Format("http://+:{0}/{1}", Port, Prefix));
				_listener.Start();

				while (true)
				{
					HttpListenerContext ctx = _listener.GetContext();
					if (ctx != null)
					{
						System.Threading.ThreadPool.QueueUserWorkItem(p=>
							ProcessRequest(ctx)
						);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
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
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

			if (ctx != null)
			{
				try
				{
					if (OnRequest != null)
						OnRequest(ctx.Request);
				}
				catch (Exception) {  }

				try
				{
					ProcessRequest(ctx);
				}
				catch (Exception) { }
			}
		}

		protected abstract void ProcessRequest(HttpListenerContext ctx);
		protected int Port { get; set; }
		protected string Prefix { get; set; }
		public event Action<HttpListenerRequest> OnRequest;
	}
}
