using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace bfbd.UltraRecord.Core
{
	partial class ImageCompressEngine
	{
		public static Bitmap GrayScaleBitmap(Image img)
		{
			Bitmap newImage = null;
			try
			{
				int nColors = 8;
				int width = img.Width;
				int height = img.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);

				Bitmap newBitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				newBitmap.Palette = GrayScalePalette;
				using (Bitmap srcBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
				{
					using (Graphics g = Graphics.FromImage(srcBitmap))
					{
						g.PageUnit = GraphicsUnit.Pixel;
						g.DrawImage(img, 0, 0);
					}
					var srcData = srcBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
					var newData = newBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
					unsafe
					{
						int srcStride = Math.Abs(srcData.Stride);
						int newStride = Math.Abs(newData.Stride);
						byte* pSrc = (byte*)srcData.Scan0.ToPointer() + ((srcData.Stride > 0) ? 0 : (srcData.Stride * (height - 1)));
						byte* pNew = newData.Stride > 0 ? (byte*)newData.Scan0.ToPointer() : (byte*)newData.Scan0.ToPointer() + (newData.Stride * (height - 1));
						PixelData* ps;
						byte* pn;
						for (uint y = 0; y < height; y++)
						{
							ps = (PixelData*)(pSrc + (srcStride * y));
							pn = pNew + (newStride * y);
							for (uint x = 0; x < width; x++)
							{
								double dv = ((ps->red * 0.299) + (ps->green * 0.587)) + (ps->blue * 0.114);
								pn[0] = (byte)(((dv * (nColors - 1)) / 255.0) + 0.5);

								++ps;
								++pn;
							}
						}
					}
					srcBitmap.UnlockBits(srcData);
					newBitmap.UnlockBits(newData);
					srcData = null;
					newData = null;
				}
				newImage = newBitmap;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static Bitmap IndexedColorBitmap(Image img)
		{
			Bitmap newImage = null;
			try
			{
				int width = img.Width;
				int height = img.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);

				Bitmap newBitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				newBitmap.Palette = ColorIndexedPalette;
				using (Bitmap srcBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
				{
					using (Graphics g = Graphics.FromImage(srcBitmap))
					{
						g.PageUnit = GraphicsUnit.Pixel;
						g.DrawImage(img, 0, 0);
					}
					var srcData = srcBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
					var newData = newBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
					unsafe
					{
						int srcStride = Math.Abs(srcData.Stride);
						int newStride = Math.Abs(newData.Stride);
						byte* pSrc = (byte*)srcData.Scan0.ToPointer() + (srcData.Stride > 0 ? 0 : (srcData.Stride * (height - 1)));
						byte* pNew = (byte*)newData.Scan0.ToPointer() + (newData.Stride > 0 ? 0 : (newData.Stride * (height - 1)));
						PixelData* ps;
						byte* pn;
						for (uint y = 0; y < height; y++)
						{
							ps = (PixelData*)(pSrc + (srcStride * y));
							pn = pNew + (newStride * y);
							for (uint x = 0; x < width; x++)
							{
								//*pn = (byte)(((ps->red >> 6) << 4) | ((ps->green >> 6) << 2) | (ps->blue >> 6));
								byte r = (byte)(ps->red * 3 / 255.0);
								byte g = (byte)(ps->green * 3 / 255.0);
								byte b = (byte)(ps->blue * 3 / 255.0);
								Debug.Assert(r < 4 && g < 4 && b < 4);
								*pn = (byte)(r << 4 | g << 2 | b);

								++ps;
								++pn;
							}
						}
					}
					srcBitmap.UnlockBits(srcData);
					newBitmap.UnlockBits(newData);
					srcData = null;
					newData = null;
				}
				newImage = newBitmap;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return newImage;
		}

		public static Bitmap ColorValueBitmap(Image img)
		{
			int width = img.Width;
			int height = img.Height;
			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format16bppRgb555);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.PageUnit = GraphicsUnit.Pixel;
				g.DrawImage(img, 0, 0);
			}
			return bmp;
		}

		public static byte[] ExtractImageData(Bitmap bitmap)
		{
			byte[] bsData = null;
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				Rectangle rect = new Rectangle(0, 0, width, height);

				var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
				try
				{
					IntPtr pData = new IntPtr(data.Scan0.ToInt64() + (data.Stride > 0 ? 0 : data.Stride * (height - 1)));
					int length = Math.Abs(data.Stride) * data.Height;
					bsData = new byte[length];
					Marshal.Copy(pData, bsData, 0, length);
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
				finally
				{
					bitmap.UnlockBits(data);
				}
			}
			return bsData;
		}

		public static float XorImageData(byte[] srcData, int srcIndex, byte[] destData, int destIndex, int length)
		{
			Debug.Assert(srcData.Length >= srcIndex + length && destData.Length >= destIndex + length);
			int diffCount = 0;
			for (int i = srcIndex, j = destIndex; i < srcIndex + length && j < destIndex + length; ++i, ++j)
			{
				diffCount += (destData[j] == srcData[i]) ? 0 : 1;
				destData[j] = (byte)(destData[j] ^ srcData[i]);
			}
			return ((float)diffCount / length);
		}

		public static Image CreateImageByData(byte[] bsData, int width, int height, ColorPalette palette)
		{
			Image img;
			try
			{
				Rectangle rect = new Rectangle(0, 0, width, height);
				PixelFormat format = palette == null ? PixelFormat.Format16bppRgb555 : PixelFormat.Format8bppIndexed;

				Bitmap bmp = new Bitmap(width, height, format);
				if (palette != null)
					bmp.Palette = palette;

				var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, format);
				{
					int length = Math.Abs(bmpData.Stride) * bmpData.Height;
					IntPtr pData = new IntPtr(bmpData.Scan0.ToInt64() + (bmpData.Stride > 0 ? 0 : bmpData.Stride * (bmpData.Height - 1)));

					Debug.Assert(bsData.Length >= length);
					Marshal.Copy(bsData, 0, pData, length);
				}
				bmp.UnlockBits(bmpData);
				img = bmp;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return img;
		}
	}

	partial class ImageCompressEngine
	{
		private static ColorPalette _grayScalePalette = null;
		public static ColorPalette GrayScalePalette
		{
			get
			{
				if (_grayScalePalette == null)
				{
					ColorPalette pal;
					using (var bitmap = new Bitmap(1, 1, PixelFormat.Format4bppIndexed))
						pal = bitmap.Palette;

					int nColors = 8;
					for (int i = 0; i < nColors; ++i)
					{
						//int alpha = (i == 0) ? 0 : 0xff;		// transparent.
						int alpha = 0xff;
						int red = ((i * 0xff) / (nColors - 1));
						pal.Entries[i] = Color.FromArgb(alpha, red, red, red);
					}
					_grayScalePalette = pal;
				}
				return _grayScalePalette;
			}
		}

		private static ColorPalette _colorIndexedPalette = null;
		public static ColorPalette ColorIndexedPalette
		{
			get
			{
				if (_colorIndexedPalette == null)
				{
					ColorPalette pal;
					using (var bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
						pal = bitmap.Palette;

					int range = 255 / 3;
					for (int r = 0; r < 4; ++r)
					{
						for (int g = 0; g < 4; ++g)
						{
							for (int b = 0; b < 4; ++b)
							{
								int pos = r << 4 | g << 2 | b;
								pal.Entries[pos] = Color.FromArgb(0xFF, r * range, g * range, b * range);
							}
						}
					}
					_colorIndexedPalette = pal;
				}
				return _colorIndexedPalette;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PixelData
		{
			public byte blue;
			public byte green;
			public byte red;
		}
	}
}
