using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Caching;

namespace bfbd.TSCamera.Client
{
	using bfbd.Common;
	using bfbd.TSCamera.Core;

	partial class RecordServiceCore
	{
		void UpdateConfigurations(object state)
		{
			// download configurations from Server.
			// update SharedMemory data to new configurations.
			// (configurations contains license info).
		}
	}

	partial class RecordServiceCore
	{
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		void RecordingSessions(object state)
		{
			if (!Global.Config.RecordEnabled)
				return;

			bool update = state is bool ? (bool)state : false;

			int[] winSessions = WTSEngine.GetActiveSessions();
			int[] rcdSessions = GetRecordingSessions();
			foreach (var sid in winSessions.Except(rcdSessions))
			{
				string user = WTSEngine.GetDomainUserBySessionId(sid);
				if (IsUserRecording(user, update))
				{
					TraceLog.WriteLineInfo("Start recording by configuration. user: " + user);
					StartRecordingSession(sid);
					TraceLog.WriteLineInfo("End recording by configuration. user: " + user);
				}
			}
			Array.ForEach(rcdSessions, sid => UpdateRecordingSession(sid));
		}

		#region RecordingSessions

		private int[] GetRecordingSessions()
		{
			List<int> sessions = new List<int>();
			string dir = AppDomain.CurrentDomain.BaseDirectory;
			foreach (var p in Process.GetProcessesByName("rcda"))
			{
				Call.Execute(() =>
				{
					if (Path.GetDirectoryName(p.MainModule.FileName) == dir)
						sessions.Add(p.SessionId);
				});
				p.Dispose();
			}
			return sessions.ToArray();
		}

		private bool IsUserRecording(string user, bool update)
		{
			object val = _cache["rcd_" + user];
			if (!update && val is bool)
			{
				return (bool)val;
			}
			else
			{
				return (bool)(_cache["rcd_" + user] = UserPolicy.IsUserRecording(user));
			}
		}

		private void StartRecordingSession(int sessionId)
		{
			string program = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rcda.exe");
			string recordId = Guid.NewGuid().ToString("n");

			TraceLog.WriteLineInfo("Try start record process...");
			int pid;
			if (bfbd.Common.Windows.OSInfo.IsVista)
				pid = ProcessEngine.CreateProcessAsAdmin(sessionId, program, recordId);
			else
				pid = ProcessEngine.CreateProcessAsUser(sessionId, program, recordId);

			if (pid != 0)
			{
				TraceLog.WriteLineInfo("Record process has started.");
				TraceLog.WriteLineInfo(string.Format("sessionId={0}, processId={1}, recordId={2}", sessionId, pid, recordId));
				_cache.Add("session_" + sessionId, recordId, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 1, 0), CacheItemPriority.Normal,
					(key, value, reason) => { new CacheManager().WriteSessionEnd(value as string); });
			}
			else
			{
				TraceLog.WriteLineError("CreateProcessAsUser Fail. sessionId=" + sessionId);
			}
		}

		private void UpdateRecordingSession(int sessionId)
		{
			var recordId = _cache["session_" + sessionId] as string;
			DebugLog.Assert(!string.IsNullOrEmpty(recordId));
			if (string.IsNullOrEmpty(recordId))
				TraceLog.WriteLineError("ERROR: Session cache empty for sessionId: " + sessionId, "UpdateRecordingSession");
		}

		#endregion RecordingSessions
	}

	partial class RecordServiceCore
	{
		private CacheManager _cacheMgr = new CacheManager();

		void ScanningCacheFiles(object state)
		{
			using (RemotingStorage _storage = new RemotingStorage())
			{
				foreach (SessionInfo si in _cacheMgr.EnumSessions())
				{
					try
					{
						IEnumerable<Snapshot> snapshtos = _cacheMgr.EnumSnapshots(si.SessionId);
						//if (snapshtos.Any())
						//    _storage.WriteSessionInfo(si);

						//    ImageCompressor compressor = new ImageCompressor();
						//    foreach (Snapshot sshot in snapshtos)
						//    {
						//        Debug.Assert(sshot.ImageData != null && sshot.ImageData.Length > 0);
						//        try
						//        {
						//            if (IsRecordingApp(sshot.ProcessName))
						//            {
						//                compressor.CompressSnapshot(sshot, Global.Config.GrayScale);
						//                _storage.WriteSnapshot(sshot);
						//            }
						//            _cache.RemoveSnapshot(sshot.SessionId, sshot.SnapshotId);
						//        }
						//        catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
						//    }
					}
					catch (Exception ex) { TraceLog.WriteException(ex); }
					try
					{
						//if (si.IsEnd)
						//    _storage.WriteSessionEnd(si.SessionId);
						//_cache.TryRemoveSession(si.SessionId);
					}
					catch (Exception ex) { TraceLog.WriteException(ex); }
				}
			}
		}
	}
}
