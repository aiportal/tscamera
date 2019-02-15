using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.UltraRecord.Core;

	class CompressStorage : bfbd.UltraRecord.Client.IStorage
	{
		public static readonly string DataPath = System.IO.Path.Combine(Path.GetDirectoryName(Application.StartupPath), "Data");

		#region IStorage

		public Dictionary<string, object> GetConfigurations()
		{
			try
			{
				return Database.Invoke(db => db.SelectDictionary<string, object>("SystemConfig", "ItemName", "ItemValue", new { Subject = "Global" }));
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

		public void WriteSessionEnd(string sessionId)
		{
			try
			{
				Database.Invoke(db => db.Update("SessionInfo", new { IsEnd = true }, new { SessionId = sessionId }));
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		#endregion

		public void WriteSnapshot(Snapshot sshot)
		{
			try
			{
				Debug.Assert(sshot.EventsData.Length > 0 && sshot.EventsData.Length % 16 == 0);
				if (!string.IsNullOrEmpty(sshot.WindowUrl) && sshot.Url == null)
					TraceLogger.Instance.WriteLineInfo("Url can not be parsed: " + sshot.WindowUrl);

				long imgPos =0 ; int imgLen = 0;
				WriteImageData(sshot, out imgPos, out imgLen);

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
						db.InsertDistinct("HostInfo", new { HostUrl = ss.UrlHost }, null);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private void WriteImageData(Snapshot sshot, out long imgPos, out int imgLen)
		{
			try
			{
				switch ((ImageStorageType)Global.Config.ImageStorage)
				{
					case ImageStorageType.TextOnly:
						imgPos = imgLen = 0;
						break;
					case ImageStorageType.GrayScale:
						break;
					case ImageStorageType.RawImage:
						break;
				}
				
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
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private string _backgroundId = null;
		private Image _backgroundImage = null;
		private void CompressImageData(Snapshot sshot)
		{
			Debug.Assert(sshot.ImageData != null && sshot.ImageData.Length > 0);
			Debug.Assert(sshot.BackgroundId == null);
			try
			{
				Image img = null;
				using (MemoryStream ms = new MemoryStream(sshot.ImageData))
					img = Image.FromStream(ms);
				Debug.Assert(img.PixelFormat != PixelFormat.Format8bppIndexed);

				if (_backgroundImage == null)
				{
					_backgroundId = sshot.SnapshotId;
					_backgroundImage = img;
				}
				else
				{
					Debug.Assert(_backgroundImage != null && _backgroundImage != img);
					Debug.Assert(_backgroundImage.PixelFormat == img.PixelFormat);

					Rectangle rc = CompareImageDifference(_backgroundImage, img);
					if (rc.Width * rc.Height * 100 / img.Width * img.Height > 80)
					{
						// change background.
						_backgroundId = sshot.SnapshotId;
						_backgroundImage = img;
					}
					else
					{
						// clip
						Bitmap bmp = new Bitmap(rc.Width, rc.Height);
						using (Graphics g = Graphics.FromImage(bmp))
						{
							g.DrawImage(img, 0, 0, rc, GraphicsUnit.Pixel);
						}
						sshot.BackgroundId = _backgroundId;
						sshot.WindowRect = rc;
						using (MemoryStream ms = new MemoryStream())
						{
							bmp.Save(ms, ImageFormat.Png);
							sshot.ImageData = ms.ToArray();
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private Rectangle CompareImageDifference(Image src, Image dst)
		{
			Debug.Assert(src != null && dst != null);
			Debug.Assert(src.Width == dst.Width && src.Height == dst.Height);
			Debug.Assert(src.PixelFormat == dst.PixelFormat);

			int width = src.Width;
			int height = src.Height;
			Rectangle rect = new Rectangle(0, 0, width, height);
			try
			{
				int minX = -1, maxX = -1, minY = -1, maxY = -1;
				Bitmap b0 = new Bitmap(src);
				BitmapData d0 = b0.LockBits(rect, ImageLockMode.ReadOnly, src.PixelFormat);
				Bitmap b1 = new Bitmap(dst);
				BitmapData d1 = b1.LockBits(rect, ImageLockMode.ReadOnly, dst.PixelFormat);
				unsafe
				{
					int* p0 = (int*)d0.Scan0;
					int* p1 = (int*)d1.Scan0;
					int offset = 0;
					for (int y = 0; y < height; ++y)
					{
						for (int x = 0; x < width; ++x)
						{
							offset = width * y + x;
							if ((*(p0 + offset)) != (*(p1 + offset)))
							{
								minX = minX < 0 ? x : (x < minX ? x : minX);
								maxX = maxX < 0 ? x : (x > maxX ? x : maxX);
								minY = minY < 0 ? y : (y < minY ? y : minY);
								maxY = maxY < 0 ? y : (y > maxY ? y : maxY);
							}
						}
					}
				}
				b0.UnlockBits(d0);
				b1.UnlockBits(d1);

				minX = minX < 0 ? 0 : minX;
				maxX = maxX < 0 ? 0 : maxX;
				minY = minY < 0 ? 0 : minY;
				maxY = maxY < 0 ? 0 : maxY;
				Debug.Assert(minX <= maxX && minY <= maxY);

				rect.X = minX;
				rect.Y = minY;
				rect.Width = (maxX == minX) ? 1 : (maxX - minX);
				rect.Height = (maxY == minY) ? 1 : (maxY - minY);
				Debug.Assert(0 <= rect.X && rect.X < width && 0 <= rect.Y && rect.Y < height);
				Debug.Assert(rect.Width > 0 && rect.Height > 0);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return rect;
		}
	}
}
