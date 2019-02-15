using System;
using System.Collections.Generic;

using System.Text;

using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace bfbd.UltraRecord.Core
{
	class ImageColorsConverter
	{
		private int _imgColors = 8;
		private ColorPalette pal;

		public ImageColorsConverter()
		{
			this.pal = this.CreateNewColorPalette();
			this.pal = this.FillPalette(this.pal, false);
		}

		private ColorPalette CreateNewColorPalette()
		{
			PixelFormat format = PixelFormat.Format1bppIndexed;
			ColorPalette palette = null;
			Bitmap bitmap = null;
			try
			{
				if (this._imgColors > 2)
				{
					format = PixelFormat.Format4bppIndexed;
				}
				if (this._imgColors > 0x10)
				{
					format = PixelFormat.Format8bppIndexed;
				}
				bitmap = new Bitmap(1, 1, format);
				palette = bitmap.Palette;
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			finally
			{
				bitmap.Dispose();
			}
			return palette;
		}

		private ColorPalette FillPalette(ColorPalette palette, bool fTransparent)
		{
			for (uint i = 0; i < this._imgColors; i++)
			{
				uint alpha = 0xff;
				uint red = (uint)((i * 0xff) / (this._imgColors - 1));
				if ((i == 0) && fTransparent)
				{
					alpha = 0;
				}
				palette.Entries[i] = Color.FromArgb((int)alpha, (int)red, (int)red, (int)red);
			}
			return palette;
		}

		public unsafe Image SaveImageWithNewColorTable(Image image)
		{
			Bitmap bitmap = null;
			Bitmap bitmap2 = null;
			Graphics graphics = null;
			BitmapData bitmapdata = null;
			BitmapData data2 = null;
			if (this._imgColors > 0x100)
			{
				this._imgColors = 0x100;
			}
			if (this._imgColors < 2)
			{
				this._imgColors = 2;
			}
			int width = image.Width;
			int height = image.Height;
			try
			{
				byte* numPtr;
				byte* numPtr2;
				bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				bitmap.Palette = this.pal;
				bitmap2 = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				graphics = Graphics.FromImage(bitmap2);
				graphics.PageUnit = GraphicsUnit.Pixel;
				graphics.DrawImage(image, 0, 0, width, height);
				Rectangle rect = new Rectangle(0, 0, width, height);
				bitmapdata = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
				data2 = bitmap2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				IntPtr ptr = bitmapdata.Scan0;
				IntPtr ptr2 = data2.Scan0;
				if (bitmapdata.Stride > 0)
				{
					numPtr = (byte*)ptr.ToPointer();
				}
				else
				{
					numPtr = (byte*)ptr.ToPointer() + (bitmapdata.Stride * (height - 1));
				}
				if (data2.Stride > 0)
				{
					numPtr2 = (byte*)ptr2.ToPointer();
				}
				else
				{
					numPtr2 = (byte*)ptr2.ToPointer() + (data2.Stride * (height - 1));
				}
				uint num3 = (uint)Math.Abs(bitmapdata.Stride);
				uint num4 = (uint)Math.Abs(data2.Stride);
				for (uint i = 0; i < height; i++)
				{
					PixelData* dataPtr = (PixelData*)(numPtr2 + (i * num4));
					byte* numPtr3 = numPtr + (i * num3);
					for (uint j = 0; j < width; j++)
					{
						double num7 = ((dataPtr->red * 0.299) + (dataPtr->green * 0.587)) + (dataPtr->blue * 0.114);
						numPtr3[0] = (byte)(((num7 * (this._imgColors - 1)) / 255.0) + 0.5);
						dataPtr++;
						numPtr3++;
					}
				}
				bitmap.UnlockBits(bitmapdata);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			finally
			{
				bitmap2.UnlockBits(data2);
				bitmap2.Dispose();
				graphics.Dispose();
				bitmapdata = null;
				data2 = null;
			}
			return bitmap;
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
