using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.UltraRecord.Core;

	class CompressEngine
	{
		private string DataPath = LocalStorage.DataPath;

		public void CompressSession(string sessionId)
		{
			try
			{
				var snapshots = Database.Invoke(db => db.SelectObjects<SnapImage>("Snapshots", new { SessionId = sessionId }, "SnapTime", 0,
					"SessionId", "SnapshotId", "WindowRect", "ImagePos", "ImageLength"));

				Image bgImg = null;
				foreach(var ss in snapshots)
				{
					if (bgImg == null)
					{
						bgImg = LoadImage(ss);
						continue;
					}
					else
					{
						//Debug.Assert(bg != null && bg.Image != null);
						ss.Image = LoadImage(ss);
						if (ss.Image != null)
						{
							CompareImage(bgImg, ss.Image);
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private Image LoadImage(SnapImage sshot)
		{
			byte[] bsImage = null;
			string path = Path.Combine(DataPath, sshot.SessionId + ".rdt");
			if (File.Exists(path))
			{
				using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (BinaryReader br = new BinaryReader(fs))
				{
					br.BaseStream.Seek(sshot.ImagePos, SeekOrigin.Begin);
					bsImage = br.ReadBytes(sshot.ImageLength);
				}
			}

			Image img = null;
			if (bsImage != null && bsImage.Length > 0)
			{
				using (MemoryStream ms = new MemoryStream(bsImage))
					img = Image.FromStream(ms);
			}
			return img;
		}

		private Rectangle CompareImage(Image src, Image dst)
		{
			Debug.Assert(src != null && dst != null);
			Debug.Assert(src.Width == dst.Width && src.Height == dst.Height);
			Debug.Assert(src.PixelFormat == dst.PixelFormat);

			int width = src.Width;
			int height = src.Height;
			Rectangle rect = new Rectangle(0, 0, width, height);

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
				minX = minX < 0 ? 0 : minX;
				maxX = maxX < 0 ? 0 : maxX;
				minY = minY < 0 ? 0 : minY;
				maxY = maxY < 0 ? 0 : maxY;
				Debug.Assert(minX <= maxX && minY <= maxY);
			}
			b0.UnlockBits(d0);
			b1.UnlockBits(d1);

			rect.X = minX;
			rect.Y = minY;
			rect.Width = maxX - minX;
			rect.Height = maxY - minY;
			return rect;
		}

		class SnapImage
		{
			public string SessionId = null;
			public string SnapshotId = null;
			public long ImagePos = 0;
			public int ImageLength = 0;
			public Image Image = null;
		}
	}
}
