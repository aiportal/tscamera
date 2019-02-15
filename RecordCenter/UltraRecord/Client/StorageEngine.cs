using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common;
	using bfbd.UltraRecord.Core;
	using bfbd.UltraRecord.Server;

	partial class StorageEngine
	{
		private static readonly CacheManager _cache = new CacheManager();
		private static readonly IStorage _storage = new LocalStorage();

		public static void ScanAndStoreCacheFiles(object state)
		{
			foreach (SessionInfo si in _cache.EnumSessions())
			{
				try
				{
					IEnumerable<Snapshot> snapshtos = _cache.EnumSnapshots(si.SessionId);
					if (snapshtos.GetEnumerator().MoveNext())
						_storage.WriteSessionInfo(si);

					ImageCompressor compressor = new ImageCompressor();
					foreach (Snapshot sshot in snapshtos)
					{
						Debug.Assert(sshot.ImageData != null && sshot.ImageData.Length > 0);
						try
						{
							if (IsRecordingApp(sshot.ProcessName))
							{
								compressor.CompressSnapshot(sshot, Global.Config.GrayScale);
								_storage.WriteSnapshot(sshot);
							}
							_cache.RemoveSnapshot(sshot.SessionId, sshot.SnapshotId);
						}
						catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
				if (si.IsEnd)
					_storage.WriteSessionEnd(si.SessionId);
				_cache.TryRemoveSession(si.SessionId);
			}
		}
		public static void _ScanAndStoreCacheFiles(object state)
		{
			foreach (SessionInfo si in _cache.EnumSessions())
			{
				try
				{
					IEnumerable<Snapshot> snapshtos = _cache.EnumSnapshots(si.SessionId);
					if (snapshtos.GetEnumerator().MoveNext())
						_storage.WriteSessionInfo(si);
					foreach (Snapshot sshot in snapshtos)
					{
						Debug.Assert(sshot.ImageData != null && sshot.ImageData.Length > 0);
						try
						{
							if (IsRecordingApp(sshot.ProcessName))
								_storage.WriteSnapshot(sshot);
							_cache.RemoveSnapshot(sshot.SessionId, sshot.SnapshotId);
						}
						catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
				if (si.IsEnd)
					_storage.WriteSessionEnd(si.SessionId);
				_cache.TryRemoveSession(si.SessionId);					
			}
		}

		public static void ScanAndRestrictLocalStore(object state)
		{
			if (Global.Config.DiskQuota > 0 || Global.Config.DaysLimit > 0)
			{
				var sessions = Database.Invoke(db => db.SelectObjects<SessionInfo>("SessionView", null, "IsEnd, LastActiveTime DESC", 0,
					"SessionId", "IsEnd", "LastActiveTime", "DataLength"));

				if (Global.Config.DiskQuota > 0)
				{
					long total = 0;
					long capacity = Global.Config.DiskQuota * 1024 * 1024 * 1024;
					foreach (var s in sessions)
					{
						if (!s.IsEnd)
							total += s.DataLength;
						else if (total < capacity)
							total += s.DataLength;
						else
							TryRemoveSessionData(s.SessionId.Replace("-", ""));
					}
				}
				if (Global.Config.DaysLimit > 0)
				{
					var date = DateTime.Now.AddDays(-Global.Config.DaysLimit);
					foreach (var s in sessions)
					{
						if (s.LastActiveTime < date)
							TryRemoveSessionData(s.SessionId.Replace("-", ""));
					}
				}
			}
		}

		private static void TryRemoveSessionData(string sessionId)
		{
			try
			{
				using (Database db = new Database())
				{
					db.Delete("SessionInfo", new { SessionId = sessionId });
					db.Delete("Snapshots", new { SessionId = sessionId });
				}
				string path = Path.Combine(LocalStorage.DataPath, sessionId + ".rdt");
				if (File.Exists(path))
					File.Delete(path);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		private static bool IsRecordingApp(string appName)
		{
			if (!string.IsNullOrEmpty(appName))
			{
				if (Global.Config.ExcludeApps != null)
					return !Array.Exists(Global.Config.ExcludeApps, s => string.Equals(s, appName, StringComparison.OrdinalIgnoreCase));
				else if (Global.Config.IncludeApps != null)
					return Array.Exists(Global.Config.IncludeApps, s => string.Equals(s, appName, StringComparison.OrdinalIgnoreCase));
				else
					return true;
			}
			else
				return true;
		}

		//private static bool IsRecordingHost(string url)
		//{
		//    bfbd.Common.Windows.WebHost host;
		//    if (bfbd.Common.Windows.WebHost.TryParse(url, out host))
		//    {
		//        if (Global.Config.ExcludeHosts != null && Global.Config.ExcludeApps.Length > 0)
		//            return !Array.Exists(Global.Config.ExcludeHosts, s => string.Equals(s, host.HostName, StringComparison.OrdinalIgnoreCase));
		//        else if (Global.Config.IncludeHosts != null)
		//            return Array.Exists(Global.Config.IncludeHosts, s => string.Equals(s, host.HostName, StringComparison.OrdinalIgnoreCase));
		//        else
		//            return false;
		//    }
		//    else
		//    {
		//        return true;
		//    }
		//}
	}

	partial class StorageEngine
	{
		public static void WriteInstallationInfo()
		{
			using (Database db = new Database())
			{
				if (!db.IsExist("SystemConfig", new { ItemName = "InstallTime" }))
					db.Insert("SystemConfig", new { ItemName = "InstallTime", ItemValue = DateTime.Now });
				if (!db.IsExist("SystemConfig", new { ItemName = "PermitUser" }))
					db.Insert("SystemConfig", new { ItemName = "PermitUser", ItemValue = bfbd.Common.Windows.DomainUser.Current });
			}
		}

		public static void UpdateConfigurations(Dictionary<string, object> dic)
		{
			try
			{
				using (Database db = new Database())
				{
					foreach (string key in dic.Keys)
					{
						db.InsertOrUpdate("SystemConfig",
							new { ItemName = key, ItemValue = dic[key] },
							new { ItemName = key });
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public static byte[] ReadImage(string snapshotId)
		{
			byte[] bsImage = null;
			try
			{
				SnapImage sshot;
				string fpath = null;
				using (Database db = new Database())
				{
					sshot = db.SelectObject<SnapImage>("SnapshotData", new { SnapshotId = snapshotId },
						"SessionId", "BackgroundId", "WindowRect", "MouseState", "IsGrayScale", "ImagePos", "ImageLen");
					if (sshot != null)
					{
						var sessionDate = db.SelectSingle<DateTime>("SessionInfo", "CreateTime", new { SessionId = sshot.SessionId });
						fpath = Path.Combine(LocalStorage.DataPath, string.Format(@"{0:yyyy}\{0:MMdd}\{1}.rdt", sessionDate, sshot.SessionId));
						TraceLogger.Instance.WriteLineInfo("Reading session file: " + fpath);
					}
				}
				if (sshot != null && File.Exists(fpath))
				{
					using (FileStream fs = File.Open(fpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (BinaryReader br = new BinaryReader(fs))
					{
						br.BaseStream.Seek(sshot.ImagePos, SeekOrigin.Begin);
						bsImage = br.ReadBytes(sshot.ImageLen);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return bsImage;
		}

		private static SnapImage LoadSnapshot(string snapshotId, Database db)
		{
			SnapImage snap = null;
			try
			{
				var sshot = db.SelectObject<SnapImage>("SnapshotData", new { SnapshotId = snapshotId },
					"SessionId", "SnapshotId", "BackgroundId", "ScreenWidth", "ScreenHeight", "MouseState", "IsGrayScale", "ImagePos", "ImageLen");
				if (sshot != null)
				{
					var sessionDate = db.SelectSingle<DateTime>("SessionInfo", "CreateTime", new { SessionId = sshot.SessionId });
					string fpath = Path.Combine(LocalStorage.DataPath, string.Format(@"{0:yyyy}\{0:MMdd}\{1}.rdt", sessionDate, sshot.SessionId));

					using (FileStream fs = File.Open(fpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (BinaryReader br = new BinaryReader(fs))
					{
						br.BaseStream.Seek(sshot.ImagePos, SeekOrigin.Begin);
						byte[] bsData = br.ReadBytes(sshot.ImageLen);

						snap = sshot;
						snap.ImageData = bsData;
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return snap;
		}

		public static byte[] LoadSnapshotImage(string snapshotId)
		{
			using (Database db = new Database())
			{
				SnapImage snap = LoadSnapshot(snapshotId, db);
				if (snap.BackgroundId != null && snap.BackgroundId != snap.SnapshotId)
				{
					SnapImage background = LoadSnapshot(snap.BackgroundId, db);
					byte[] bsBackground = Compress.Deflate(background.ImageData, false);
					byte[] bsData = Compress.Deflate(snap.ImageData, false);
					ImageCompressEngine.XorImageData(bsBackground, 0, bsData, 0, bsBackground.Length);

					var pal = snap.IsGrayScale ? ImageCompressEngine.GrayScalePalette : null;
					Image img = ImageCompressEngine.CreateImageByData(bsData, snap.ScreenWidth, snap.ScreenHeight, pal);
					using (MemoryStream ms = new MemoryStream())
					{
						img.Save(ms, ImageFormat.Png);
						return ms.ToArray();
					}
				}
				else
				{
					byte[] bsData = Compress.Deflate(snap.ImageData, false);
					var pal = snap.IsGrayScale ? ImageCompressEngine.GrayScalePalette : null;
					Image img = ImageCompressEngine.CreateImageByData(bsData, snap.ScreenWidth, snap.ScreenHeight, pal);
					using (MemoryStream ms = new MemoryStream())
					{
						img.Save(ms, ImageFormat.Png);
						return ms.ToArray();
					}
				}
			}
		}
	}
	
	public class SnapImage
	{
		public string SessionId;
		public string SnapshotId;
		public string BackgroundId;
		public int ScreenWidth;
		public int ScreenHeight;
		public string MouseState;
		public bool IsGrayScale;
		public long ImagePos;
		public int ImageLen;
		public byte[] ImageData;
	}
}
