using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Diagnostics;

namespace bfbd.UltraRecord.Client
{
	class SystemEventsCenter
	{
		public event EventHandler<ProcessEventArgs> ProcessStart;
		public event EventHandler<ProcessEventArgs> ProcessExit;

		private readonly int UtcOffsetHours = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
		private List<ManagementEventWatcher> _watchers = new List<ManagementEventWatcher>();

		public void StartWatchers(params string[] wqls)
		{
			foreach (string wql in wqls)
			{
				ManagementEventWatcher watcher = new ManagementEventWatcher(wql);
				watcher.EventArrived += new EventArrivedEventHandler(watcher_EventArrived);
				watcher.Start();
				_watchers.Add(watcher);
			}
		}

		void watcher_EventArrived(object sender, EventArrivedEventArgs e)
		{
			ManagementBaseObject obj = e.NewEvent;
			string clsName = obj.ClassPath.ClassName;
			switch (clsName)
			{
				case "Win32_ProcessStartTrace":
				case "Win32_ProcessStopTrace":
					{
						ProcessEventArgs arg = new ProcessEventArgs()
						{
							SessionId = Convert.ToInt32(obj["SessionID"]),
							ProcessId = Convert.ToInt32(obj["ProcessID"]),
							ProcessName = obj["ProcessName"] as string,
							TimeCreated = DateTime.FromFileTimeUtc(Convert.ToInt64(obj["TIME_CREATED"])).AddHours(UtcOffsetHours),
						};
						if (clsName == "Win32_ProcessStartTrace" && this.ProcessStart != null)
							this.ProcessStart(this, arg);
						if (clsName == "Win32_ProcessStopTrace" && this.ProcessExit != null)
							this.ProcessExit(this, arg);
					}
					break;
				default:
					break;

			}
		}

		public void StopWatchers()
		{
			foreach(ManagementEventWatcher watcher in _watchers)
			{
				watcher.Stop();
				watcher.Dispose();
			}
			_watchers.Clear();
		}
	}

	delegate void ProcessEventHandler(int sessionId, int processId, string processName, DateTime timeCreated);

	sealed class SystemEventsQuery
	{
		public const string Process = @"select * from Win32_ProcessTrace";
		public const string ProcessStart = @"select * from Win32_ProcessStartTrace";
		public const string ProcessStop = @"select * from Win32_ProcessStopTrace";

		public static string ProcessName(string processName)
		{
			return string.Format(@"SELECT * FROM Win32_ProcessTrace WHERE ProcessName = '{0}'", processName);
		}

		public static string ProcessStartForName(string processName)
		{
			return string.Format(@"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{0}'", processName);
		}
	}

	class ProcessEventArgs : EventArgs
	{
		public int SessionId;
		public int ProcessId;
		public string ProcessName;
		public DateTime TimeCreated;
	}
}
