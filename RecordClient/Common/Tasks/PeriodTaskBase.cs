﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace bfbd.Common.Tasks
{
	abstract class PeriodTaskBase : IDisposable
	{
		private Thread _thread;
		private bool _running = false;

		public virtual void Start()
		{
			_running = true;
			_thread = new Thread(this.Run);
			_thread.Priority = this.Priority;
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
			Thread.Sleep(this.Wait);
			while (_running)
			{
				Execute();
				Thread.Sleep(this.Pulse);
			}
		}

		protected abstract void Execute();

		public int Wait { get; protected set; }
		public int Pulse { get; protected set; }
		public ThreadPriority Priority { get; protected set; }

		public bool IsRunning { get { return _running; } }
	}
}
