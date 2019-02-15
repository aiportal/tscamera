using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace bfbd.TSCamera.Core
{
	using bfbd.Common;

	public sealed partial class CacheManager
	{
		public static readonly string CachePath = Path.Combine(Path.GetDirectoryName(Application.StartupPath), "Cache");

		public void WriteSesionInfo(SessionInfo session)
		{
			try
			{
				string dir = Path.Combine(CachePath, session.SessionId);
				string rds = Path.Combine(dir, "0.rds");
				Debug.Assert(!Directory.Exists(dir));
				Debug.Assert(!File.Exists(rds));

				Directory.CreateDirectory(dir);
				SerializeEngine.Serialize(session, rds);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public void WriteSnapshot(SessionInfo session, Snapshot sshot)
		{
			try
			{
				string dir = Path.Combine(CachePath, session.SessionId);
				string rds = Path.Combine(dir, "0.rds");
				string rdi = Path.Combine(dir, sshot.SnapshotId + ".rdi");
				Debug.Assert(Directory.Exists(dir));
				Debug.Assert(File.Exists(rds));
				Debug.Assert(!File.Exists(rdi));

				if (!Directory.Exists(dir) || !File.Exists(rds))
				{
					Directory.CreateDirectory(dir);
					SerializeEngine.Serialize(session, rds);
				}
				SerializeEngine.Serialize(sshot, rdi);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public void WriteSessionEnd(string sessionId)
		{
			///? Is WriteSessionEnd called right?
			try
			{
				string dir = Path.Combine(CachePath, sessionId);
				string rds = Path.Combine(dir, "0.rds");
				string rde = Path.Combine(dir, "0.rde");
				Debug.Assert(Directory.Exists(dir));
				Debug.Assert(File.Exists(rds));

				if (Directory.Exists(dir) && !File.Exists(rde))
					File.WriteAllText(rde, sessionId);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public SessionInfo LoadSessionInfo(string sessionId)
		{
			try
			{
				string dir = Path.Combine(CachePath, sessionId);
				string rds = Path.Combine(dir, "0.rds");
				if (File.Exists(rds))
					return SerializeEngine.Deserialize(rds) as SessionInfo;
				else
					return null;
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}
	}

	partial class CacheManager
	{
		public IEnumerable<SessionInfo> EnumSessions()
		{
			foreach (string dir in Directory.GetDirectories(CacheManager.CachePath))
			{
				SessionInfo si = null;
				try
				{
					string rds = Path.Combine(dir, "0.rds");
					if (File.Exists(rds))
						si = SerializeEngine.Deserialize(rds) as SessionInfo;
				}
				catch (Exception ex) { TraceLog.WriteException(ex); }

				if (si != null)
				{
					string rde = Path.Combine(dir, "0.rde");
					if (File.Exists(rde))
						si.IsEnd = true;
					yield return si;
				}
			}
		}

		public IEnumerable<Snapshot> EnumSnapshots(string sessionId)
		{
			string dir = Path.Combine(CacheManager.CachePath, sessionId);
			FileInfo[] files = new DirectoryInfo(dir).GetFiles("*.rdi");
			if (files.Length > 0)
			{
				Array.Sort<FileInfo>(files, (f1, f2) => DateTime.Compare(f1.CreationTime, f2.CreationTime));
				for (int i = 0; i < files.Length - 1; ++i)
				{
					Snapshot sshot = null;
					try
					{
						string fpath = files[i].FullName;
						if (File.Exists(fpath))
							sshot = SerializeEngine.Deserialize(fpath) as Snapshot;
					}
					catch (Exception ex) { TraceLog.WriteException(ex); }
					if (sshot != null)
						yield return sshot;
				}

				FileInfo fi = files[files.Length - 1];
				if (fi.LastWriteTime.AddMinutes(1) < DateTime.Now)
				{
					Snapshot sshot = null;
					try
					{
						string fpath = fi.FullName;
						if (File.Exists(fpath))
							sshot = SerializeEngine.Deserialize(fpath) as Snapshot;
					}
					catch (Exception ex) { TraceLog.WriteException(ex); }
					if (sshot != null)
						yield return sshot;
				}
			}
		}

		public bool TryRemoveSession(string sessionId)
		{
			bool removed = false;
			try
			{
				string dir = Path.Combine(CachePath, sessionId);
				string rde = Path.Combine(dir, "0.rde");
				int rdiCount = Directory.GetFiles(dir, "*.rdi").Length;
				//Debug.Assert(rdiCount == 0);
				if (File.Exists(rde) && rdiCount == 0)
				{
					Directory.Delete(dir, true);
					removed = true;
				}
				else if (Directory.GetLastWriteTime(dir).AddMinutes(30) < DateTime.Now)
				{
					Directory.Delete(dir, true);
					removed = true;
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return removed;
		}

		public void RemoveSnapshot(string sessionId, string snapshotId)
		{
			try
			{
				string dir = Path.Combine(CachePath, sessionId);
				string rdi = Path.Combine(dir, snapshotId + ".rdi");
				if (File.Exists(rdi))
					File.Delete(rdi);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}
	}
}
