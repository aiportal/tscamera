using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace bfbd.UltraRecord.Client
{
	partial class ImageCompressEngine
	{
		public static Image ShortColorImage(Image srcImage)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format32bppArgb);
			Bitmap bmp = new Bitmap(srcImage.Width, srcImage.Height, PixelFormat.Format16bppRgb555);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.DrawImage(srcImage, 0, 0);
			}
			return bmp;
		}

		public static Image GrayScaleImage(Image srcImage)
		{
			try
			{
				int width = srcImage.Width;
				int height = srcImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);

				Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed) { Palette = GrayScalePalette };
				using (Bitmap srcBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
				{
					using (Graphics g = Graphics.FromImage(srcBmp))
						g.DrawImage(srcImage, 0, 0);

					BitmapData srcData = srcBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					BitmapData newData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
					Debug.Assert(srcData.Stride > 0);
					Debug.Assert(newData.Stride > 0);
					int srcStride = srcData.Stride;
					int newStride = newData.Stride;
					unsafe
					{
						byte* pSrc = (byte*)srcData.Scan0.ToPointer();
						byte* pNew = (byte*)newData.Scan0.ToPointer();
						Argb* ps;
						byte* pn;
						for (int y = 0; y < height; ++y)
						{
							ps = (Argb*)(pSrc + (srcStride * y));
							pn = (byte*)(pNew + (newStride * y));
							for (int x = 0; x < width; ++x)
							{
								Argb color = *ps;
								*pn = (byte)((color.red * 0.299) + (color.green * 0.587) + (color.blue * 0.114) + 0.5);
								++ps;
								++pn;
							}
						}
					}
					srcBmp.UnlockBits(srcData);
					newBmp.UnlockBits(newData);
				}
				return newBmp;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private static ColorPalette _grayScalePalette;
		private static ColorPalette GrayScalePalette
		{
			get
			{
				if (_grayScalePalette == null)
				{
					ColorPalette pal;
					using (var bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
						pal = bmp.Palette;

					uint nColors = 256;
					for (uint i = 0; i < nColors; i++)
					{
						uint Alpha = 0xFF;
						uint Intensity = i * 0xFF / (nColors - 1);    // Even distribution. 

						if (i == 0)
							Alpha = 0;
						pal.Entries[i] = Color.FromArgb((int)Alpha, (int)Intensity, (int)Intensity, (int)Intensity);
					}
					_grayScalePalette = pal;
				}
				return _grayScalePalette;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Argb
		{
			public byte alpha;
			public byte blue;
			public byte green;
			public byte red;
		}
	}

	partial class ImageCompressEngine
	{
		public static byte[] GrayScaleImage(byte[] bsImage, ImageFormat format)
		{
			Image newImage;
			using (MemoryStream msImage = new MemoryStream(bsImage))
			{
				var srcImage = Image.FromStream(msImage);
				newImage = GrayScaleImage(srcImage);
				//newImage = new ImageColorsConverter().SaveImageWithNewColorTable(srcImage);
				srcImage.Dispose();
			}
			byte[] bsData;
			using (MemoryStream msData = new MemoryStream())
			{
				newImage.Save(msData, format);
				bsData = msData.ToArray();
				newImage.Dispose();
			}
			return bsData;
		}

		public static byte[] ShortColorImage(byte[] bsImage, ImageFormat format)
		{
			Image newImage;
			using (MemoryStream msImage = new MemoryStream(bsImage))
			{
				var srcImage = Image.FromStream(msImage);
				newImage = ShortColorImage(srcImage);
				srcImage.Dispose();
			}
			byte[] bsData;
			using (MemoryStream msData = new MemoryStream())
			{
				newImage.Save(msData, format);
				bsData = msData.ToArray();
				newImage.Dispose();
			}
			return bsData;
		}
	}

	partial class ImageCompressEngine
	{
		public static Image TransparentColorImage(Image bgImage, Image srcImage, float ratio)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format16bppRgb555);
			Debug.Assert(bgImage != null && bgImage.PixelFormat == PixelFormat.Format16bppRgb555);
			Debug.Assert(srcImage.Width == bgImage.Width && srcImage.Height == bgImage.Height);
			Debug.Assert(0 < ratio && ratio < 1);

			Image newImage = null;
			try
			{
				int width = bgImage.Width;
				int height = bgImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);
				long diffMax = (int)(width * height * ratio);
				long diffCount = 0;

				Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format16bppArgb1555);
				using (Bitmap srcBmp = new Bitmap(srcImage))
				using (Bitmap bgBmp = new Bitmap(bgImage))
				{
					BitmapData srcData = srcBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format16bppRgb555);
					BitmapData bgData = bgBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format16bppRgb555);
					BitmapData newData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
					Debug.Assert(srcData.Stride == bgData.Stride && srcData.Stride == newData.Stride);
					int stride = srcData.Stride;
					unsafe
					{
						byte* pSrc = (byte*)srcData.Scan0.ToPointer();
						byte* pBkg = (byte*)bgData.Scan0.ToPointer();
						byte* pNew = (byte*)newData.Scan0.ToPointer();
						ushort* ps, pg, pn;
						for (int y = 0; y < height; ++y)
						{
							ps = (ushort*)(pSrc + (stride * y));
							pg = (ushort*)(pBkg+ (stride * y));
							pn = (ushort*)(pNew + (stride * y));
							for (int x = 0; x < width; ++x)
							{
								//if (0 == *ps)
								//    *pn = (ushort)(*ps | 0x8000);
								//else
									*pn = (ushort)((*pg == *ps) ? 0 : (*ps | 0x8000));
								++ps;
								++pg;
								++pn;

								diffCount += ((*pg == *ps) ? 0 : 1);
							}
							if (diffCount >= diffMax)
								break;
						}
					}
					srcBmp.UnlockBits(srcData);
					bgBmp.UnlockBits(bgData);
					newBmp.UnlockBits(newData);
				}

				if (diffCount < diffMax)
				{
					//newBmp.MakeTransparent(Color.Empty);
					newImage = newBmp;
				}
				else
				{
					newBmp.Dispose();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static byte[] XorGrayscaleImage(Image bgImage, Image srcImage, float ratio)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(bgImage != null && bgImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(srcImage.Width == bgImage.Width && srcImage.Height == bgImage.Height);
			Debug.Assert(0 < ratio && ratio < 1);

			byte[] bsXor = null;
			try
			{
				int width = bgImage.Width;
				int height = bgImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);
				long diffMax = (int)(width * height * ratio);
				long diffCount = 0;

				using (Bitmap srcBmp = new Bitmap(srcImage))
				using (Bitmap bkgBmp = new Bitmap(bgImage))
				using (Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
				{
					BitmapData srcData = srcBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					BitmapData bkgData = bkgBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					BitmapData newData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
					Debug.Assert(srcData.Stride == bkgData.Stride && srcData.Stride == newData.Stride);
					Debug.Assert(srcData.Stride % 4 == 0);
					int stride = srcData.Stride;
					unsafe
					{
						byte* pSrc = (byte*)srcData.Scan0.ToPointer();
						byte* pBkg = (byte*)bkgData.Scan0.ToPointer();
						byte* pNew = (byte*)newData.Scan0.ToPointer();
						uint* ps, pg, pn;
						for (int y = 0; y < height; ++y)
						{
							ps = (uint*)(pSrc + (stride * y));
							pg = (uint*)(pBkg + (stride * y));
							pn = (uint*)(pNew + (stride * y));
							for (int x = 0; x < width; x += 4)
							{
								*pn = *pg ^ *ps;
								++ps;
								++pg;
								++pn;

								diffCount += ((*pg == *ps) ? 0 : 4);
							}
							if (diffCount >= diffMax)
								break;
						}
					}
					if (diffCount < diffMax)
					{
						bsXor = new byte[newData.Stride * newData.Height];
						Marshal.Copy(newData.Scan0, bsXor, 0, bsXor.Length);
					}
					srcBmp.UnlockBits(srcData);
					bkgBmp.UnlockBits(bkgData);
					newBmp.UnlockBits(newData);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return bsXor;
		}

		public static Image CombineGrayscaleXor(Image srcImage, byte[] bsXor)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Image newImage = null;
			try
			{
				int width = srcImage.Width;
				int height = srcImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);

				Bitmap srcBmp = new Bitmap(srcImage);
				srcBmp.Palette = GrayScalePalette;
				BitmapData srcData = srcBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
				Debug.Assert(srcData.Stride * srcData.Height == bsXor.Length);
				Debug.Assert(srcData.Stride % 4 == 0);
				int stride = srcData.Stride;
				unsafe
				{
					fixed (byte* pXor = bsXor)
					{
						byte* pSrc = (byte*)srcData.Scan0.ToPointer();
						uint* ps, px;
						for (int y = 0; y < height; ++y)
						{
							ps = (uint*)(pSrc + (stride * y));
							px = (uint*)(pXor + (stride * y));
							for (int x = 0; x < width; x += 4)
							{
								*ps = (*ps ^ *px);
								++ps;
								++px;
							}
						}
					}
				}
				srcBmp.UnlockBits(srcData);
				newImage = srcBmp;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static Image TransparentImage8(Image bgImage, Image srcImage, float ratio)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(bgImage != null && bgImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(srcImage.Width == bgImage.Width && srcImage.Height == bgImage.Height);
			Debug.Assert(0 < ratio && ratio < 1);

			Image newImage = null;
			try
			{
				int width = bgImage.Width;
				int height = bgImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);
				long diffMax = (int)(width * height * ratio);
				long diffCount = 0;

				Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				newBmp.Palette = srcImage.Palette;
				using (Bitmap srcBmp = new Bitmap(srcImage))
				using (Bitmap bkgBmp = new Bitmap(bgImage))
				{
					BitmapData srcData = srcBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					BitmapData bkgData = bkgBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					BitmapData newData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
					unsafe
					{
						byte* pSrc = (byte*)srcData.Scan0.ToPointer();
						byte* pBkg = (byte*)bkgData.Scan0.ToPointer();
						byte* pNew = (byte*)newData.Scan0.ToPointer();
						byte* ps, pg, pn;
						for (int y = 0; y < height; ++y)
						{
							ps = pSrc + (y * srcData.Stride);
							pg = pBkg + (y * bkgData.Stride);
							pn = pNew + (y * newData.Stride);
							for (int x = 0; x < width; ++x)
							{
								if (0 == *ps)
									*pn = 0xFF;
								else
									*pn = (byte)((*pg == *ps) ? 0 : *ps);
								++ps;
								++pg;
								++pn;

								diffCount += ((*pg == *ps) ? 0 : 1);
							}
							if (diffCount >= diffMax)
								break;
						}
					}
					srcBmp.UnlockBits(srcData);
					bkgBmp.UnlockBits(bkgData);
					newBmp.UnlockBits(newData);
				}

				if (diffCount < diffMax)
				{
					newBmp.MakeTransparent(Color.Empty);
					newImage = newBmp;
				}
				else
				{
					newBmp.Dispose();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static Image _TransparentImage8(Image bgImage, Image srcImage, float ratio, out bool transparent)
		{
			Debug.Assert(srcImage != null && srcImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(bgImage != null && bgImage.PixelFormat == PixelFormat.Format8bppIndexed);
			Debug.Assert(srcImage.Width == bgImage.Width && srcImage.Height == bgImage.Height);
			Debug.Assert(0 < ratio && ratio < 1);

			transparent = false;
			Image newImage = srcImage;
			try
			{
				int width = bgImage.Width;
				int height = bgImage.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);
				long diffMax = (int)(width * height * ratio);
				long diffCount = 0;

				Bitmap newBmp = new Bitmap(srcImage);
				using (Bitmap bgBmp = new Bitmap(bgImage))
				{
					BitmapData bgData = bgBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					BitmapData newData = newBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
					unsafe
					{
						byte* pBkg = (byte*)bgData.Scan0.ToPointer();
						byte* pNew = (byte*)newData.Scan0.ToPointer();
						byte* pg = pBkg;
						byte* pn = pNew;
						for (int y = 0; y < height; ++y)
						{
							pg = pBkg + (y * bgData.Stride);
							pn = pNew + (y * newData.Stride);
							for (int x = 0; x < width; ++x)
							{
								diffCount += ((*pg == *pn) ? 0 : 1);
								if (0 == *pn)
									*pn = 1;
								else
									*pn = (byte)((*pg == *pn) ? 0 : *pn);
								++pg;
								++pn;
							}
							if (diffCount >= diffMax)
								break;
						}
					}
					newBmp.UnlockBits(newData);
					bgBmp.UnlockBits(bgData);
				}

				if (diffCount < diffMax)
				{
					newBmp.MakeTransparent(Color.Empty);
					newImage = newBmp;
					transparent = true;
				}
				else
				{
					newBmp.Dispose();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static Bitmap _MakeTransparentGif(Bitmap bitmap, Color color)
		{
			byte R = color.R;
			byte G = color.G;
			byte B = color.B;

			MemoryStream fin = new MemoryStream();
			bitmap.Save(fin, System.Drawing.Imaging.ImageFormat.Gif);

			MemoryStream fout = new MemoryStream((int)fin.Length);
			int count = 0;
			byte[] buf = new byte[256];
			byte transparentIdx = 0;
			fin.Seek(0, SeekOrigin.Begin);
			//header
			count = fin.Read(buf, 0, 13);
			if ((buf[0] != 71) || (buf[1] != 73) || (buf[2] != 70)) return null; //GIF

			fout.Write(buf, 0, 13);

			int i = 0;
			if ((buf[10] & 0x80) > 0)
			{
				i = 1 << ((buf[10] & 7) + 1) == 256 ? 256 : 0;
			}

			for (; i != 0; i--)
			{
				fin.Read(buf, 0, 3);
				if ((buf[0] == R) && (buf[1] == G) && (buf[2] == B))
				{
					//transparentIdx = (byte)(256 - i);
				}
				fout.Write(buf, 0, 3);
			}

			bool gcePresent = false;
			while (true)
			{
				fin.Read(buf, 0, 1);
				fout.Write(buf, 0, 1);
				if (buf[0] != 0x21) 
					break;
				fin.Read(buf, 0, 1);
				fout.Write(buf, 0, 1);
				gcePresent = (buf[0] == 0xf9);
				while (true)
				{
					fin.Read(buf, 0, 1);
					fout.Write(buf, 0, 1);
					if (buf[0] == 0) 
						break;
					count = buf[0];
					if (fin.Read(buf, 0, count) != count) return null;
					if (gcePresent)
					{
						if (count == 4)
						{
							buf[0] |= 0x01;
							buf[3] = transparentIdx;
						}
					}
					fout.Write(buf, 0, count);
				}
			}
			while (count > 0)
			{
				count = fin.Read(buf, 0, 1);
				fout.Write(buf, 0, 1);
			}
			fin.Close();
			fout.Flush();

			return new Bitmap(fout);
		}  
	}
}
