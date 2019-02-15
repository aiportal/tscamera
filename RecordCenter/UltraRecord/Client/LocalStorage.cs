using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Data.SQLite;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common;
	using bfbd.Common.Data;
	using bfbd.Common.Windows;
	using bfbd.UltraRecord.Core;

	sealed class LocalStorage : bfbd.UltraRecord.Client.IStorage
	{
		public static readonly string DataPath = System.IO.Path.Combine(Path.GetDirectoryName(Application.StartupPath), "Data");

		public Dictionary<string, object> GetConfigurations()
		{
			try
			{
				return Database.Invoke(db => db.SelectDictionary<string, object>("SystemConfig",
					"ItemName", "ItemValue",
					new { Subject = "Global" }));
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void WriteSessionInfo(SessionInfo session)
		{
			try
			{
				using (Database db = new Database())
				{
					var si = new
					{
						SessionId = session.SessionId,
						CreateTime = session.CreateTime,
						LastActiveTime = session.CreateTime,
						UserName = session.UserName,
						Domain = session.Domain,
						ClientName = session.ClientName,
						ClientAddress = session.ClientAddress,
					};
					db.InsertDistinct("SessionInfo", si, new { SessionId = si.SessionId });
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void WriteSnapshot(Snapshot sshot)
		{
			try
			{
				using (Database db = new Database())
				{
					WriteSnapshotData(sshot, db);
					
					if (!db.IsExist("Snapshots", new { SnapshotId = sshot.SnapshotId }))
					{
						WindowUrl host = WindowUrl.Create(sshot.WindowUrl);
						var snap = new
						{
							SessionId = sshot.SessionId,
							SnapshotId = sshot.SnapshotId,
							BackgroundId = sshot.BackgroundId,
							SnapTime = sshot.SnapTime,
							ProcessId = sshot.ProcessId,
							ProcessName = sshot.ProcessName,
							WindowTitle = sshot.WindowTitle,
							WindowUrl = host == null ? sshot.WindowUrl : host.AbsoluteUri,
							UrlHost = host == null ? null : host.HostName,
							ControlText = sshot.ControlText,
							InputText = sshot.InputText,
						};
						db.Insert("Snapshots", snap);

						WriteApplicationInfo(sshot, db);
						if (host != null)
							WriteHostInfo(host, db);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}
		
		public void WriteSessionEnd(string sessionId)
		{
			try
			{
				Database.Invoke(db => db.Update("SessionInfo", new { IsEnd = true }, new { SessionId = sessionId }));
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private void WriteSnapshotData(Snapshot sshot, Database db)
		{
			try
			{
				if (!db.IsExist("SnapshotData", new { SnapshotId = sshot.SnapshotId }))
				{
					long imgPos = WriteImageData(sshot, db);
					int imgLen = sshot.ImageData == null ? 0 : sshot.ImageData.Length;
					var data = new
					{
						SessionId = sshot.SessionId,
						SnapshotId = sshot.SnapshotId,
						BackgroundId = sshot.BackgroundId,
						ScreenWidth = sshot.ScreenWidth,
						ScreenHeight = sshot.ScreenHeight,
						WindowHandle = sshot.WindowHandle,
						WindowRect = DataConverter.Serialize(sshot.WindowRect),
						MouseState = DataConverter.Serialize(sshot.Mouse),
						ImagePos = imgPos,
						ImageLen = imgLen,
						IsGrayScale = sshot.IsGrayScale,
						EventsCount = sshot.EventsCount
					};
					db.Insert("SnapshotData", data);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private long WriteImageData(Snapshot sshot, Database db)
		{
			long position;
			try
			{
				var sessionDate = db.SelectSingle<DateTime>("SessionInfo", "CreateTime", new { SessionId = sshot.SessionId });
				string path = Path.Combine(DataPath, string.Format(@"{0:yyyy}\{0:MMdd}\{1}.rdt", sessionDate, sshot.SessionId));
				if (!Directory.Exists(Path.GetDirectoryName(path)))
					Directory.CreateDirectory(Path.GetDirectoryName(path));
				using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read))
				{
					position = fs.Position;
					if (sshot.ImageData != null && sshot.ImageData.Length > 0)
					{
						fs.Write(sshot.ImageData, 0, sshot.ImageData.Length);
					}
					Debug.Assert(sshot.EventsData != null && sshot.EventsData.Length > 0 && sshot.EventsData.Length % 16 == 0);
					fs.Write(sshot.EventsData, 0, sshot.EventsData.Length);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return position;
		}

		private void WriteApplicationInfo(Snapshot sshot, Database db)
		{
			if (string.IsNullOrEmpty(sshot.ProcessName))
				return;
			if (db.IsExist("ApplicationInfo", new { ProcessName = sshot.ProcessName }))
				return;

			try
			{
				if (!string.IsNullOrEmpty(sshot.FileName))
				{
					var fv = FileVersionInfo.GetVersionInfo(sshot.FileName);
					var app = new
					{
						ProcessName = sshot.ProcessName,
						FileName = sshot.FileName,
						Description = fv.FileDescription,
					};
					db.Insert("ApplicationInfo", app);
				}
				else
				{
					db.Insert("ApplicationInfo", new { ProcessName = sshot.ProcessName });
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		private void WriteHostInfo(WindowUrl host, Database db)
		{
			Debug.Assert(host != null);
			if (!host.IsFile && !string.IsNullOrEmpty(host.HostName))
			{
				if (!db.IsExist("HostInfo", new { HostUrl = host.HostName }))
					db.Insert("HostInfo", new { HostUrl = host.HostName, HostName = host.HostSimpleName });
			}
		}
	}
}

//long imgPos, imgLen;
//string path = Path.Combine(DataPath, sshot.SessionId + ".rdt");
//using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read))
//{
//    if (sshot.ImageData != null && sshot.ImageData.Length > 0)
//    {
//        imgPos = fs.Position;
//        fs.Write(sshot.ImageData, 0, sshot.ImageData.Length);
//        fs.Write(sshot.EventsData, 0, sshot.EventsData.Length);
//        imgLen = sshot.ImageData.Length;
//    }
//    else
//    {
//        imgPos = fs.Position;
//        fs.Write(sshot.EventsData, 0, sshot.EventsData.Length);
//        imgLen = 0;
//    }
//}

