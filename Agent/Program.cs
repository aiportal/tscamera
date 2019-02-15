using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace bfbd.UltraRecord
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//DebugFunc();

			bool single = false;
			using (var mutex = new System.Threading.Mutex(true, "RecordAgent", out single))
			{
				if (!single)
					return;
			}
			
			Application.ThreadException += (o, ev) => { bfbd.TraceLogger.Instance.WriteException(ev.Exception); };
			AppDomain.CurrentDomain.UnhandledException += (o, ev) => { bfbd.TraceLogger.Instance.WriteException(ev.ExceptionObject as Exception); };

			string sessionId = args.Length > 0 ? args[0] : null;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new bfbd.UltraRecord.Core.RawInputApp() { SessionId = sessionId });
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void DebugFunc()
		{
			System.Threading.Thread.Sleep(5 * 1000);
			IntPtr hWnd = user32.GetForegroundWindow();
			string url = bfbd.UltraRecord.Core.TextCaptureEngine.GetWindowUrl(hWnd);
			
			//System.Threading.Thread.Sleep(5000);
			//System.Drawing.Image img = bfbd.UltraRecord.Core.WindowCaptureEngine.CaptureScreen();
			//img.Save(@"D:\desktop.png", System.Drawing.Imaging.ImageFormat.Png);
			//bfbd.UltraRecord.Core.TextCaptureEngine.GetUrls();
		}
	}
}
