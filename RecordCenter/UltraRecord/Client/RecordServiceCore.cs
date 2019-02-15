using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common.Task;
	using bfbd.UltraRecord.Core;

	partial class RecordServiceCore
	{
		private static readonly string RecordCoreApp = System.IO.Path.Combine(Application.StartupPath, "rcda.exe");

		private Hashtable _watchers = Hashtable.Synchronized(new Hashtable(5));
		private PeriodTask _tasks;
		private bfbd.UltraRecord.Server.AdminServiceCore _adminWeb;

		public void Start()
		{
			TraceLogger.Instance.WriteLineInfo("Privileges ajusting...");
			bool succeed = WTSEngine.SetProcessPrivileges(Process.GetCurrentProcess().Id,
				bfbd.WindowsAPI.WTS.NtPrivileges.SE_ASSIGNPRIMARYTOKEN_NAME,
				bfbd.WindowsAPI.WTS.NtPrivileges.SE_INCREASE_QUOTA_NAME,
				bfbd.WindowsAPI.WTS.NtPrivileges.SE_TCB_NAME);
			TraceLogger.Instance.WriteLineInfo("Privileges ajusted: " + succeed);

			TraceLogger.Instance.WriteLineInfo("Record Service is starting...");
			_tasks = new PeriodTask(1000);
			_tasks.AddTask("License", this.UpdateLicenseInfo, 60 * 60, 0);
			_tasks.AddTask("Configuration", this.UpdateConfigurationFile, 60, 0);
			_tasks.AddTask("Session", this.ScanWinSessionsToRecordOrEnd, 2, 10);
			_tasks.AddTask("Storage", StorageEngine.ScanAndStoreCacheFiles, 5, 15);
			_tasks.AddTask("Restrict", StorageEngine.ScanAndRestrictLocalStore, 60 * 60, 60 * 60);
			_tasks.AddTask("AccessPolicy", this.UpdateWebAccessPolicy, 60, 0);
			_tasks.Start();
			TraceLogger.Instance.WriteLineInfo("Record Service is started.");
			
			if (Global.Config.AdminWebPort > 80)
			{
				try
				{
					TraceLogger.Instance.WriteLineInfo("Admin Service is starting...");
					_adminWeb = new Server.AdminServiceCore();
					_adminWeb.Start();
					TraceLogger.Instance.WriteLineInfo("Admin Service is started.");
				}
				catch (Exception ex)
				{
					TraceLogger.Instance.WriteException(ex);
					_adminWeb = null;
				}
			}
		}

		public void Stop()
		{
			TraceLogger.Instance.WriteLineInfo("Record Service stoping...");
			if (_tasks != null)
			{
				_tasks.Stop();
				_tasks = null;
			}
			TraceLogger.Instance.WriteLineInfo("Record Service stoped.");

			if (_adminWeb != null)
			{
				TraceLogger.Instance.WriteLineInfo("Admin Service is stoping...");
				_adminWeb.Stop();
				_adminWeb = null;
				TraceLogger.Instance.WriteLineInfo("Admin Service is stoped.");
			}
		}

		public void SessionLogon(int winSessionId)
		{
			if (!Global.Config.RecordEnabled)
				return;
			try
			{
				while (!WTSEngine.IsSessionActive(winSessionId))
					System.Threading.Thread.Sleep(1000);

				//if (checkUserPolicy && (!IsUserRecording(winSessionId)))
				//    return;

				string rcdSessionId = Guid.NewGuid().ToString("n");
				string rcdProgram = System.IO.Path.Combine(Application.StartupPath, "rcda.exe");
				int pid;
				if (bfbd.Common.OSVersion.IsVista)
					pid = ProcessEngine.CreateProcessAsAdmin(winSessionId, rcdProgram, rcdSessionId);
				else
					pid = ProcessEngine.CreateProcessAsUser(winSessionId, rcdProgram, rcdSessionId);

				if (pid != 0)
				{
					TraceLogger.Instance.WriteLineInfo("Record process has started.");
					TraceLogger.Instance.WriteLineInfo(string.Format("winSessionId={0}, ProcessId={1}, rcdSessionId={2}", winSessionId, pid, rcdSessionId));

					SessionWatcher watcher = new SessionWatcher()
					{
						WinSessionId = winSessionId,
						RcdSessionId = rcdSessionId,
						ProcessId = pid,
					};
					_watchers[winSessionId] = watcher;
				}
				else
				{
					TraceLogger.Instance.WriteLineError("CreateProcessAsUser Fail. SessionId=" + winSessionId);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		public void SessionLogoff(int winSessionId)
		{
			try
			{
				if (_watchers.ContainsKey(winSessionId))
				{
					SessionWatcher sw = _watchers[winSessionId] as SessionWatcher;
					_watchers.Remove(winSessionId);
					new CacheManager().WriteSessionEnd(sw.RcdSessionId);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		//private bool IsUserRecording(int winSessionId)
		//{
		//    string user = WTSEngine.GetDomainUserBySessionId(winSessionId);
		//    return new UserPolicy().IsUserRecording(user);
		//}
	}

	class SessionWatcher
	{
		public int WinSessionId { get; set; }
		public string RcdSessionId { get; set; }
		public int ProcessId { get; set; }
	}
}
