using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace bfbd.TSCamera.Client
{
	using bfbd.Common;
	using bfbd.Common.Tasks;

	partial class RecordServiceCore
	{
		private static readonly string RecordCoreApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rcda.exe");
		private PeriodTask _tasks;

		public void Start()
		{
			TraceLog.WriteLineInfo("Privileges ajusting...");
			bool succeed = PrivilegeEngine.SetProcessPrivileges(Process.GetCurrentProcess().Id,
				PrivilegeEngine.SE_ASSIGNPRIMARYTOKEN_NAME,
				PrivilegeEngine.SE_INCREASE_QUOTA_NAME,
				PrivilegeEngine.SE_TCB_NAME);
			TraceLog.WriteLineInfo("Privileges ajusted: " + succeed);

			TraceLog.WriteLineInfo("Record Service is starting...");
			_tasks = new PeriodTask(1000);
			_tasks.AddTask(Global.Tasks.UpdateConfigurations, this.UpdateConfigurations, null, 60, 0);
			_tasks.AddTask(Global.Tasks.RecordingSessions, this.RecordingSessions, null, 5, 10);

			//_tasks.AddTask("License", this.UpdateLicenseInfo, 60 * 60, 0);
			//_tasks.AddTask("Configuration", this.UpdateConfigurationFile, 60, 0);
			//_tasks.AddTask("Session", this.ScanWinSessionsToRecordOrEnd, 2, 10);
			//_tasks.AddTask("Storage", StorageEngine.ScanAndStoreCacheFiles, 5, 15);
			//_tasks.AddTask("Restrict", StorageEngine.ScanAndRestrictLocalStore, 60 * 60, 60 * 60);
			_tasks.Start();
			TraceLog.WriteLineInfo("Record Service is started.");
		}

		public void Stop()
		{
			TraceLog.WriteLineInfo("Record Service stoping...");
			if (_tasks != null)
			{
				_tasks.Stop();
				_tasks = null;
			}
			TraceLog.WriteLineInfo("Record Service stoped.");
		}

		public void SessionLogon(int sessionId)
		{
			try
			{
				while (!WTSEngine.IsSessionActive(sessionId))
					System.Threading.Thread.Sleep(1000);

				PeriodTask.PostMessage(Global.Tasks.RecordingSessions, sessionId);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); }
		}

		public void SessionLogoff(int sessionId)
		{
			//PeriodTask.PostMessage(Global.Tasks.RecordingSessions, -sessionId);
		}
	}
}
