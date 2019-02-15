using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

namespace bfbd.UltraRecord.Core
{
	using bfbd.Common;

	partial class ImageCompressor
	{
		private float _ratio = 0.1f;
		private Background _background = null;

		public void CompressSnapshot(bfbd.UltraRecord.Core.Snapshot sshot, bool isGrayScale)
		{
			Debug.Assert(sshot.ImageData != null && sshot.ImageData.Length > 0);

			using (MemoryStream ms = new MemoryStream(sshot.ImageData))
			{
				var img = Image.FromStream(ms);
				var bmp = isGrayScale ? ImageCompressEngine.GrayScaleBitmap(img) : ImageCompressEngine.ColorValueBitmap(img);
				img.Dispose();

				bool isBackground;
				byte[] bsData = CompressImageData(sshot.SnapshotId, bmp, out isBackground);

				Debug.Assert(sshot.ScreenWidth == bmp.Width && sshot.ScreenHeight == bmp.Height);
				sshot.ScreenWidth = bmp.Width;
				sshot.ScreenHeight = bmp.Height;
				sshot.IsGrayScale = isGrayScale;
				sshot.ImageData = bsData;
				if (!isBackground)
					sshot.BackgroundId = _background.SnapshotId;

				bmp.Dispose();
			}
		}

		private byte[] CompressImageData(string ssid, Bitmap bmp, out bool isBackground)
		{
			byte[] bsData;
			isBackground = true;
			try
			{
				int width = bmp.Width;
				int height = bmp.Height;
				bool indexed = (bmp.PixelFormat == PixelFormat.Format8bppIndexed);
				byte[] data = ImageCompressEngine.ExtractImageData(bmp);

				if (_background != null)
				{
					if (width == _background.Width && height == _background.Height || indexed != _background.IsIndexed)
					{
						Debug.Assert(_background.Data.Length == data.Length);
						float diff = ImageCompressEngine.XorImageData(_background.Data, 0, data, 0, _background.Data.Length);
						if (diff < _ratio)
						{
							bsData = Compress.Deflate(data, true);
							isBackground = false;
						}
						else
						{
							data = ImageCompressEngine.ExtractImageData(bmp);
							UpdateBackground(ssid, width, height, indexed, data);
							bsData = Compress.Deflate(data, true);
						}
					}
					else
					{
						UpdateBackground(ssid, width, height, indexed, data);
						bsData = Compress.Deflate(data, true);
					}
				}
				else
				{
					UpdateBackground(ssid, width, height, indexed, data);
					bsData = Compress.Deflate(data, true);
				}
			}
			catch (Exception ex)
			{
				bsData = null;
				TraceLogger.Instance.WriteException(ex);
				throw;
			}
			return bsData;
		}

		private void UpdateBackground(string ssid, int width, int height, bool isIndexed, byte[] data)
		{
			_background = new Background()
			{
				SnapshotId = ssid,
				Width = width,
				Height = height,
				IsIndexed = isIndexed,
				Data = data
			};
		}

		class Background
		{
			public string SnapshotId;
			public int Width;
			public int Height;
			public bool IsIndexed;
			public byte[] Data;
		}

		public float Ratio { get { return _ratio; } set { _ratio = value; } }
	}
}
