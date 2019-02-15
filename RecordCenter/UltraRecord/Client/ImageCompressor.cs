using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace bfbd.UltraRecord.Client
{
	using Snapshot = bfbd.UltraRecord.Core.Snapshot;

	class ImageCompressor
	{
		private Snapshot _bkgSnapshot = null;
		private Image _bkgImage = null;

		public bool CompressImageData(bfbd.UltraRecord.Core.Snapshot sshot)
		{
			bool transparent = false;
			Image srcImage = Deserialize(sshot.ImageData);
			if (Global.Config.GrayScale)
			{
				var grayImage = ImageCompressEngine.GrayScaleImage(srcImage);
				srcImage.Dispose();

				sshot.ImageData = Serialize(grayImage);
				sshot.IsGrayScale = true;
				grayImage.Dispose();

				UpdateBackground(null, null);
			}
			else
			{
				var colorImage = ImageCompressEngine.ShortColorImage(srcImage);
				srcImage.Dispose();

				if (_bkgImage == null)
					UpdateBackground(sshot, colorImage);
				else if (_bkgSnapshot.WindowHandle != sshot.WindowHandle)
					UpdateBackground(sshot, colorImage);
				else if (colorImage.Width != _bkgImage.Width || colorImage.Height != _bkgImage.Height)
					UpdateBackground(sshot, colorImage);
				else
				{
					var transparentImage = ImageCompressEngine.TransparentColorImage(_bkgImage, colorImage, 0.1f);
					if (transparentImage == null)
						UpdateBackground(sshot, colorImage);
					else
					{
						colorImage.Dispose();
						colorImage = transparentImage;
						transparent = true;
					}
				}
				
				sshot.IsGrayScale = false;
				sshot.ImageData = Serialize(colorImage);
				if (transparent)
					sshot.BackgroundId = _bkgSnapshot.SnapshotId;
			}
			return transparent;
		}

		private void UpdateBackground(Snapshot sshot, Image img)
		{
			if (_bkgImage != null)
				_bkgImage.Dispose();
			_bkgSnapshot = sshot;
			_bkgImage = img;
		}

		private byte[] Serialize(Image img)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				img.Save(ms, ImageFormat.Png);
				return ms.ToArray();
			}
		}

		private Image Deserialize(byte[] bsImage)
		{
			using (MemoryStream ms = new MemoryStream(bsImage))
			{
				return Image.FromStream(ms);
			}
		}
	}
}
