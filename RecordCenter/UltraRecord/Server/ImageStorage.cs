using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;

	class ImageStorage
	{
		private static string DataPath = LocalStorage.DataPath;

		public long WriteImageData(string sessionId, byte[] imgData, byte[] addData)
		{
			Debug.Assert(sessionId != null && imgData != null && imgData.Length > 0);
			long pos;
			try
			{
				string path = Path.Combine(DataPath, sessionId + ".rdm");
				using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read))
				{
					pos = fs.Position;
					fs.Write(imgData, 0, imgData.Length);
					fs.Write(addData, 0, addData.Length);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return pos;
		}
		
		public byte[] ReadImageData(string snapshotId)
		{
			byte[] bsImage = null;
			try
			{
				var sshot = Database.Invoke(db => db.SelectRow("Snapshots", new { SnapshotId = snapshotId },
					"SessionId", "WindowRect", "MouseState", "ImagePos", "ImageLength"));
				if (sshot != null)
				{
					Guid sessionId = new Guid(sshot["SessionId"].ToString());
					string path = Path.Combine(DataPath, sessionId.ToString("n") + ".rdm");
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
	}
}
