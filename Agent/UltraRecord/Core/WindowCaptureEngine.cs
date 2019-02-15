using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	class WindowCaptureEngine
	{
		public static Rectangle GetWindowRect(IntPtr hWnd)
		{
			User32.RECT rc;
			User32.GetWindowRect(hWnd, out rc);
			return (Rectangle)rc;
		}

		public static Image CaptureScreen()
		{
			return CaptureWindow(User32.GetDesktopWindow());
		}

		public static Image CaptureWindow(IntPtr handle)
		{
			int width, height;
			{
				User32.RECT windowRect;
				User32.GetWindowRect(handle, out windowRect);
				width = windowRect.right - windowRect.left;
				height = windowRect.bottom - windowRect.top;
			}
			Debug.Assert(width > 0 && height > 0);
			Image img = null;
			{
				IntPtr hdcSrc = User32.GetWindowDC(handle);
				IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
				IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
				{
					IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
					GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCDOPY | GDI32.CAPTUREBLT);
					GDI32.SelectObject(hdcDest, hOld);
					img = Image.FromHbitmap(hBitmap);
				}
				GDI32.DeleteObject(hBitmap);
				GDI32.DeleteDC(hdcDest);
				User32.ReleaseDC(handle, hdcSrc);
			}
			return img;
		}

		public static Bitmap PrintWindow(IntPtr hWnd)
		{
			IntPtr hscrdc = User32.GetWindowDC(hWnd);
			Control control = Control.FromHandle(hWnd);
			IntPtr hbitmap = GDI32.CreateCompatibleBitmap(hscrdc, control.Width, control.Height);
			IntPtr hmemdc = GDI32.CreateCompatibleDC(hscrdc);
			GDI32.SelectObject(hmemdc, hbitmap);
			User32.PrintWindow(hWnd, hmemdc, 0);
			Bitmap bmp = Bitmap.FromHbitmap(hbitmap);
			GDI32.DeleteDC(hscrdc);//删除用过的对象  
			GDI32.DeleteDC(hmemdc);//删除用过的对象  
			return bmp;
		}

		#region GDI32
		private class GDI32
		{
			public const int SRCDOPY = 0x00CC0020;
			public const int CAPTUREBLT = 1073741824;

			[DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
			public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);
			[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap", SetLastError = true)]
			public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
			[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
			public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
			[DllImport("gdi32.dll", EntryPoint = "DeleteDC", SetLastError = true)]
			public static extern bool DeleteDC(IntPtr hDC);
			[DllImport("gdi32.dll", EntryPoint = "DeleteObject", SetLastError = true)]
			public static extern bool DeleteObject(IntPtr hObject);
			[DllImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true)]
			public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
		}
		#endregion GDI32

		#region User32
		private class User32
		{
			[DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
			public static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT rect);

			[DllImport("user32.dll", EntryPoint = "GetDesktopWindow", SetLastError = true)]
			public static extern IntPtr GetDesktopWindow();
			
			[DllImport("user32.dll", EntryPoint = "GetWindowDC", SetLastError = true)]
			public static extern IntPtr GetWindowDC(IntPtr hWnd);
	
			[DllImport("user32.dll", EntryPoint = "ReleaseDC", SetLastError = true)]
			public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

			[DllImport("user32.dll", EntryPoint = "PrintWindow", SetLastError = true)]
			public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, UInt32 nFlags);

			[Serializable, StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int left;
				public int top;
				public int right;
				public int bottom;

				public static explicit operator System.Drawing.Rectangle(RECT rc)
				{
					return new System.Drawing.Rectangle(Math.Min(rc.left, rc.right), Math.Min(rc.top, rc.bottom), Math.Abs(rc.right - rc.left), Math.Abs(rc.bottom - rc.top));
				}
			}
		}
		#endregion User32
	}
}


//public static Bitmap GetDesktopImage()
//{
//    //In size variable we shall keep the size of the screen.
//    SIZE size;

//    //Variable to keep the handle to bitmap.
//    IntPtr hBitmap;

//    //Here we get the handle to the desktop device context.
//    IntPtr hDC = User32.GetDC(User32.GetDesktopWindow());

//    //Here we make a compatible device context in memory for screen device context.
//    IntPtr hMemDC = GDI32.CreateCompatibleDC(hDC);

//    //We pass SM_CXSCREEN constant to GetSystemMetrics to get the X coordinates of screen.
//    size.cx = User32.GetSystemMetrics(PlatformInvokeUSER32.SM_CXSCREEN);

//    //We pass SM_CYSCREEN constant to GetSystemMetrics to get the Y coordinates of screen.
//    size.cy = User32.GetSystemMetrics(PlatformInvokeUSER32.SM_CYSCREEN);

//    //We create a compatible bitmap of screen size using screen device context.
//    hBitmap = GDI32.CreateCompatibleBitmap(hDC, size.cx, size.cy);

//    //As hBitmap is IntPtr we can not check it against null. For this purspose IntPtr.Zero is used.
//    if (hBitmap != IntPtr.Zero)
//    {
//        //Here we select the compatible bitmap in memeory device context and keeps the refrence to Old bitmap.
//        IntPtr hOld = (IntPtr)GDI32.SelectObject(hMemDC, hBitmap);
//        //We copy the Bitmap to the memory device context.
//        GDI32.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, 0, 0, (uint)(GDI32.SRCCOPY | GDI32.CAPTUREBLT));
//        //We select the old bitmap back to the memory device context.
//        GDI32.SelectObject(hMemDC, hOld);
//        //We delete the memory device context.
//        GDI32.DeleteDC(hMemDC);
//        //We release the screen device context.
//        GDI32.ReleaseDC(User32.GetDesktopWindow(), hDC);
//        //Image is created by Image bitmap handle and stored in local variable.
//        Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
//        //Release the memory to avoid memory leaks.
//        GDI32.DeleteObject(hBitmap);
//        //This statement runs the garbage collector manually.
//        GC.Collect();
//        //Return the bitmap 
//        return bmp;
//    }

//    //If hBitmap is null retunrn null.
//    return null;
//}
