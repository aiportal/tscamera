using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace bfbd.Common.Task
{
	abstract class PeriodTaskBase : IDisposable
	{
		private Thread _thread;
		private bool _running = false;

		public virtual void Start()
		{
			_running = true;
			_thread = new Thread(this.Run);
			_thread.Start();
		}

		public virtual void Stop()
		{
			if (_thread != null)
			{
				_running = false;
				_thread = null;
			}
		}

		public void Dispose()
		{
			Stop();
		}

		private void Run()
		{
			Thread.Sleep(Wait);
			while (_running)
			{
				Execute();
				Thread.Sleep(Pulse);
			}
		}

		protected abstract void Execute();

		public int Wait { get; protected set; }
		public int Pulse { get; protected set; }
		public bool IsRunning { get { return _running; } }
	}
}
