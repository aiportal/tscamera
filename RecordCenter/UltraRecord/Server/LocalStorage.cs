using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Data.SQLite;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.Data;
	using bfbd.UltraRecord.Core;

	[Obsolete]
	sealed partial class LocalStorage : bfbd.UltraRecord.Client.IStorage
	{
		#region IStorage
	
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
				Debug.Assert(sshot.EventsData.Length > 0 && sshot.EventsData.Length % 16 == 0);
				if (!string.IsNullOrEmpty(sshot.WindowUrl) && sshot.Url == null)
					TraceLogger.Instance.WriteLineInfo("Url can not be parsed: " + sshot.WindowUrl);

				long imgPos, imgLen;
				string path = Path.Combine(DataPath, sshot.SessionId + ".rdt");
				using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read))
				{
					if (sshot.ImageData != null && sshot.ImageData.Length > 0)
					{
						imgPos = fs.Position;
						fs.Write(sshot.ImageData, 0, sshot.ImageData.Length);
						fs.Write(sshot.EventsData, 0, sshot.EventsData.Length);
						imgLen = sshot.ImageData.Length;
					}
					else
					{
						imgPos = fs.Position;
						fs.Write(sshot.EventsData, 0, sshot.EventsData.Length);
						imgLen = 0;
					}
				}

				using (Database db = new Database())
				{
					var ss = new
					{
						SessionId = sshot.SessionId,
						SnapshotId = sshot.SnapshotId,
						SnapTime = sshot.SnapTime,
						ProcessId = sshot.ProcessId,
						ProcessName = sshot.ProcessName,
						WindowHandle = sshot.WindowHandle,
						WindowRect = DataConverter.Serialize(sshot.WindowRect),

						WindowTitle = sshot.WindowTitle,
						WindowUrl = sshot.Url == null ? sshot.WindowUrl : sshot.Url.AbsoluteUri,
						UrlHost = sshot.Url == null ? null : sshot.Url.Host,

						//MouseState = DataConverter.Serialize(sshot.Mouse),
						ImagePos = imgPos,
						ImageLength = imgLen,
						IsGrayScale = sshot.IsGrayScale,

						ControlText = sshot.ControlText,
						InputText = sshot.InputText,
						EventsCount = sshot.EventsCount,
					};

					db.InsertDistinct("Snapshots", ss, new { SnapshotId = sshot.SnapshotId });
					if (!string.IsNullOrEmpty(sshot.ProcessName))
						db.InsertDistinct("ApplicationInfo", new { ProcessName = ss.ProcessName });
					if (!string.IsNullOrEmpty(ss.UrlHost))
					{
						string name = ss.UrlHost.StartsWith("www.") ? ss.UrlHost.Substring(4) : ss.UrlHost;
						name = name.EndsWith(".com") ? name.Substring(0, name.Length - 4) : name;
						db.InsertDistinct("HostInfo", new { HostUrl = ss.UrlHost, HostName = name }, new { HostUrl = ss.UrlHost });
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

		#endregion IStorage
	}

	partial class LocalStorage
	{
		[Obsolete]
		public void RemoveSessionData(string sessionId)
		{
			try
			{
				using (Database db = new Database())
				{
					db.Delete("SessionInfo", new { SessionId = sessionId });
					db.Delete("Snapshots", new { SessionId = sessionId });
				}
				string path = Path.Combine(DataPath, sessionId + ".rdt");
				if (File.Exists(path))
					File.Delete(path);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		[Obsolete]
		public byte[] ReadImage(string snapshotId)
		{
			byte[] bsImage = null;
			try
			{
				var sshot = Database.Invoke(db => db.SelectRow("Snapshots", new { SnapshotId = snapshotId },
					"SessionId", "WindowRect", "MouseState", "ImagePos", "ImageLength"));
				if (sshot != null)
				{
					Guid sessionId = new Guid(sshot["SessionId"].ToString());
					string path = Path.Combine(DataPath, sessionId.ToString("n") + ".rdt");
					if (File.Exists(path))
					{
						using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (BinaryReader br = new BinaryReader(fs))
						{
							br.BaseStream.Seek(Convert.ToInt64(sshot["ImagePos"]), SeekOrigin.Begin);
							bsImage = br.ReadBytes(Convert.ToInt32(sshot["ImageLength"]));
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return bsImage;
		}

		[Obsolete]
		public void UpdateConfigurations(Dictionary<string, object> dic)
		{
			try
			{
				using (Database db = new Database())
				{
					foreach (string key in dic.Keys)
					{
						db.InsertOrUpdate("SystemConfig",
							new { Subject = "Global", ItemName = key, ItemValue = dic[key] },
							new { Subject = "Global", ItemName = key });
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		[Obsolete]
		public void SetInstallTime()
		{
			using (Database db = new Database())
			{
				var condition = new { Subject = "Global", ItemName = "InstallTime" };
				var vals = new { Subject = "Global", ItemName = "InstallTime", ItemValue = DateTime.Now };
				if (!db.IsExist("SystemConfig", condition))
					db.InsertOrUpdate("SystemConfig", vals, condition);
			}
		}

		//public long GetTotalSpace()
		//{
		//    long total = 0;
		//    DirectoryInfo di = new DirectoryInfo(DataPath);
		//    foreach(var f in di.GetFiles("*.rdt"))
		//        total += f.Length;
		//    return total;
		//}

		//public DateTime MinSessionDate()
		//{
		//    return Database.Invoke(db => db.SelectSingle<DateTime>("SessionInfo", "Min(CreateTime)", null));
		//}
	}
}

//[Obsolete]
//public bfbd.Common.License.LicenseInfo LoadLicense()
//{
//    bfbd.Common.License.LicenseInfo lic = null;
//    string path = Path.Combine(Path.GetDirectoryName(Application.StartupPath), "license.lic");
//    if (System.IO.File.Exists(path))
//    {
//        string xml = Encryption.Decrypt(File.ReadAllText(path));
//        lic = Serialization.FromXml<bfbd.Common.License.LicenseInfo>(xml);
//        lic.IsVerified = bfbd.Common.License.RSA.VerifyXml(xml);
//        if (lic.MachineId == Guid.Empty.ToString("n").Replace("0", "F"))
//            lic.MachineId = new bfbd.Common.License.MachineInfo().GetMachineId(Common.License.HardwareType.Driver);
//    }
//    else
//    {
//        lic = new Common.License.LicenseInfo();
//        lic.Version = "1.2";
//        lic.MachineId = new bfbd.Common.License.MachineInfo().GetMachineId(Common.License.HardwareType.Driver);
//    }
//    return lic;
//}