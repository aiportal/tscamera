using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Core
{
	using bfbd.WindowsAPI;
	using bfbd.UltraRecord.Core;

	static partial class SnapshotEngine
	{
		public static WindowInfo GetActiveProcessWindow()
		{
			WindowInfo wi = new WindowInfo();
			try
			{
				IntPtr hWnd = GetForegroundWindow();
				if (hWnd != IntPtr.Zero)
				{
					Process process = GetWindowProcess(hWnd);
					if (process != null)
					{
						try
						{
							wi.ProcessId = process.Id;
							wi.ProcessName = process.ProcessName;
							wi.FileName = GetProcessFile(process.Id);
						}
						catch (Exception ex) { TraceLogger.Instance.WriteLineInfo("Open process fail:" + wi.ProcessName); TraceLogger.Instance.WriteException(ex); }
						process.Dispose();
					}

					wi.ScreenWidth = Screen.PrimaryScreen.Bounds.Width;
					wi.ScreenHeight = Screen.PrimaryScreen.Bounds.Height;

					wi.WindowHandle = hWnd.ToInt32();
					wi.WindowRect = WindowCaptureEngine.GetWindowRect(hWnd);
					wi.WindowTitle = TextCaptureEngine.GetWindowTitle(hWnd);
					wi.WindowUrl = TextCaptureEngine.GetWindowUrl(hWnd);

					///? GetControlText cause double click invalid.
					//wi.ControlText = TextCaptureEngine.GetControlText(hWnd);	
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return wi;
		}

		private static IntPtr GetForegroundWindow()
		{
			IntPtr hWnd = IntPtr.Zero;
			try
			{
				hWnd = user32.GetForegroundWindow();
				for (int i = 0; i < 20; ++i)
				{
					System.Threading.Thread.Sleep(10);
					hWnd = user32.GetForegroundWindow();
					if (hWnd != IntPtr.Zero)
						break;
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return hWnd;
		}

		private static Process GetWindowProcess(IntPtr hWnd)
		{
			Process p = null;
			try
			{
				int pid;
				user32.GetWindowThreadProcessId(hWnd, out pid);
				if (pid > 0)
					p = Process.GetProcessById(pid);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return p;
		}

		private static string GetProcessFile(int processId)
		{
			string filename = null;
			IntPtr hProcess = IntPtr.Zero;
			try
			{
				hProcess = kernel32.OpenProcess(util.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
				if (hProcess != IntPtr.Zero)
				{
					var buffer = new StringBuilder(1024);
					int size = buffer.Capacity;
					if (kernel32.QueryFullProcessImageName(hProcess, 0, buffer, out size))
						filename = buffer.ToString();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			finally
			{
				if (hProcess != IntPtr.Zero)
					kernel32.CloseHandle(hProcess);
			}
			return filename;
		}
	}

	class WindowInfo
	{
		public int ProcessId;
		public string ProcessName;
		public string FileName;

		public int ScreenWidth;
		public int ScreenHeight;

		public int WindowHandle;
		public Rectangle WindowRect;
		public string WindowTitle;
		public string WindowUrl;

		//public int ControlHandle;
		//public string ControlText;
		//public Point CaretPosition;
	}
}

//[Obsolete]
//public static Image CaptureDesktop()
//{
//    Image img = null;
//    try
//    {
//        int width = Screen.PrimaryScreen.Bounds.Width;
//        int height = Screen.PrimaryScreen.Bounds.Height;
//        img = new Bitmap(width, height);
//        using (Graphics g = Graphics.FromImage(img))
//        {
//            g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
//        }
//    }
//    catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
//    return img;
//}
//public static MouseState CaptureMouseState(MouseEventType evt)
//{
//    MouseState mouse = null;
//    try
//    {
//        if (!Cursor.Position.IsEmpty)
//        {
//            mouse = new MouseState()
//            {
//                ClickOption = MouseClickOption.None,
//                X = Cursor.Position.X,
//                Y = Cursor.Position.Y,
//            };
//            if (evt == MouseEventType.LeftButtonDown)
//                mouse.ClickOption = MouseClickOption.LeftButtonDown;
//            else if (evt == MouseEventType.RightButtonDown)
//                mouse.ClickOption = MouseClickOption.RightButtonDown;
//        }
//    }
//    catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
//    return mouse;
//}

