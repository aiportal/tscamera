using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Client
{
	class SessionMonitor
	{
		private List<SessionWatcher> _watchers = new List<SessionWatcher>();

		public void UpdateSessionWatchers(object state)
		{
			int[] winSessions = WTSEngine.GetActiveSessions();
			Process[] rcdProcesses = GetRcordingProcesses();

			_watchers.Clear();
			foreach (int wsid in winSessions)
			{
				var watcher = new SessionWatcher() { WinSessionId = wsid };
				_watchers.Add(watcher);

				var proc = Array.Find(rcdProcesses, p => p.SessionId == wsid);
				if (proc != null)
				{
					watcher.RecordProcess = proc;
					watcher.State = SessionState.Recording;
				}
				else
				{
					string user = WTSEngine.GetDomainUserBySessionId(wsid);
					if (UserPolicy.IsUserRecording(user))
					{
						// logon
					}
					else
					{
						watcher.State = SessionState.Exclude;
					}
				}
			}
		}

		public void ScanSessionWatchers(object state)
		{
			foreach (var w in _watchers)
			{
				if (w.State == SessionState.Exclude)
					continue;
				if (w.State == SessionState.Recording)
				{
					if (w.RecordProcess.HasExited)
					{
						w.State = SessionState.Unkown;
						// logon
					}
				}
				else
				{
					// logon
				}
			}
		}

		private Process[] GetRcordingProcesses()
		{
			List<Process> rcdProcesses = new List<Process>();
			foreach (var p in Process.GetProcessesByName("rcda"))
			{
				try
				{
					if (Path.GetDirectoryName(p.MainModule.FileName) == Application.StartupPath)
						rcdProcesses.Add(p);
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			}
			return rcdProcesses.ToArray();
		}

		class SessionWatcher
		{
			public int WinSessionId;
			//public string RecordId;
			public SessionState State;
			public Process RecordProcess;
		}

		enum SessionState
		{
			Unkown = 0,
			Recording = 1,
			Exclude = 2
		}
	}
}
