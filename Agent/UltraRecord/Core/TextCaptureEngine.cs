using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace bfbd.UltraRecord.Core
{
	static partial class TextCaptureEngine
	{
		#region GetWindowTitle
		public static string GetWindowTitle(IntPtr hWnd)
		{
			StringBuilder sb = new StringBuilder(256);
			User32.GetWindowText(hWnd, sb, sb.Capacity);
			return sb.ToString();
		}
		#endregion

		#region GetWindowUrl

		public static string GetWindowUrl(IntPtr hWnd)
		{
			Debug.Assert(hWnd != IntPtr.Zero);
			string processName = null;
			try
			{
				int pid;
				User32.GetWindowThreadProcessId(hWnd, out pid);
				if (pid > 0)
				{
					var p = Process.GetProcessById(pid);
					if (p != null)
						processName = p.ProcessName.ToLower();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

			string url = null;
			switch (processName.ToLower())
			{
				case "chrome":
					url = GetChromeUrl(hWnd);
					break;
				case "opera":
					GetOperaUrl(hWnd);
					break;
				case "firefox":
					GetFirfoxUrl(hWnd);
					break;
				case "iexplore":
				case "explorer":
					url = GetIExploreUrl(hWnd);
					break;
				case "360se":
					url = Get360SeUrl(hWnd);
					break;
				case "maxthon":
					url = GetMaxthonUrl(hWnd);
					break;
				case "baidubrowser":
					url = GetBaiduUrl(hWnd);
					break;
				case "sogouexplorer":
					url = GetSogouUrl(hWnd);
					break;
				case "miniie":
					url = GetMiniIEUrl(hWnd);
					break;
				case "theworld":	// 世界之窗浏览器
					break;
				default:
					url = GetIExploreUrl(hWnd);		// try
					break;
			}
			return url;
		}

		private static string GetIExploreUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
				foreach (SHDocVw.InternetExplorer ie in shellWindows)
				{
					if (ie.HWND == hWnd.ToInt32())
					{
						url = ie.LocationURL;
						break;
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetChromeUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				IntPtr hAddressWnd = User32.FindWindowEx(hWnd, IntPtr.Zero, "Chrome_OmniboxView", "");
				if (hAddressWnd != IntPtr.Zero)
				{
					StringBuilder sb = new StringBuilder(2083);
					User32.SendMessage(hAddressWnd, User32.WM_GETTEXT, 2083, sb);
					url = sb.ToString();
				}
				else
				{
					var wnd = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NativeWindowHandleProperty, hWnd.ToInt32()));
					if (wnd != null)
					{
						var edit = wnd.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
						if (edit != null)
						{
							var val = edit.GetCurrentPropertyValue(ValuePattern.ValueProperty, false);
							if (val != null)
								url = val.ToString();
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetFirfoxUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				var dde = new NDde.Client.DdeClient("firefox", "WWW_GetWindowInfo");
				dde.Connect();
				url = dde.Request("URL", 3000);
				dde.Disconnect();
				if (!string.IsNullOrEmpty(url))
					url = url.Split(',')[0].Trim('\"');
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetOperaUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				var dde = new NDde.Client.DdeClient("opera", "WWW_GetWindowInfo");
				dde.Connect();
				url = dde.Request("URL", 3000);
				dde.Disconnect();
				if (!string.IsNullOrEmpty(url))
					url = url.Split(',')[0].Trim('\"');
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				// if not support dde, try window text.
				try
				{
					IntPtr hAddressWnd = User32.FindWindowEx(hWnd, IntPtr.Zero, "ViewsTextfieldEdit", "");
					if (hAddressWnd != IntPtr.Zero)
					{
						StringBuilder sb = new StringBuilder(2083);
						User32.SendMessage(hAddressWnd, User32.WM_GETTEXT, 2083, sb);
						url = sb.ToString();
					}
				}
				catch (Exception ex2) { TraceLogger.Instance.WriteException(ex2); }
			}
			return url;
		}

		private static string Get360SeUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				IntPtr hAddressWnd = User32.FindWindowEx(hWnd, IntPtr.Zero, "SmartUI.Win32.Edit", "");
				if (hAddressWnd != IntPtr.Zero)
				{
					StringBuilder sb = new StringBuilder(2083);
					User32.SendMessage(hAddressWnd, User32.WM_GETTEXT, 2083, sb);
					url = sb.ToString();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetMaxthonUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				var wnd = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NativeWindowHandleProperty, hWnd.ToInt32()));
				if (wnd != null)
				{
					var edit = wnd.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
					if (edit != null)
					{
						var val = edit.GetCurrentPropertyValue(ValuePattern.ValueProperty, false);
						if (val != null)
							url = val.ToString();
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetBaiduUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				IntPtr hAddressWnd = User32.FindWindowEx(hWnd, IntPtr.Zero, "ATL:5F22A750", "");
				if (hAddressWnd != IntPtr.Zero)
				{
					StringBuilder sb = new StringBuilder(2083);
					User32.SendMessage(hAddressWnd, User32.WM_GETTEXT, 2083, sb);
					url = sb.ToString();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetSogouUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				var wnd = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NativeWindowHandleProperty, hWnd.ToInt32()));
				if (wnd != null)
				{
					var pane = wnd.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.ClassNameProperty, "SE_TuotuoAddressBarEditCtrl"));
					if (pane != null)
					{
						var val = pane.GetCurrentPropertyValue(AutomationElement.NameProperty);
						if (val != null)
							url = val.ToString();
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}
		private static string GetMiniIEUrl(IntPtr hWnd)
		{
			string url = null;
			try
			{
				var wnd = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NativeWindowHandleProperty, hWnd.ToInt32()));
				if (wnd != null)
				{
					var pane = wnd.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.ClassNameProperty, "ThunderRT6ComboBox"));
					if (pane != null)
					{
						var val = pane.GetCurrentPropertyValue(AutomationElement.NameProperty);
						if (val != null)
							url = val.ToString();
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return url;
		}

		#endregion

		#region GetControlText

		public static string GetControlText(IntPtr hWnd)
		{
			Debug.Assert(hWnd != IntPtr.Zero);
			string processName = null;
			try
			{
				int pid;
				User32.GetWindowThreadProcessId(hWnd, out pid);
				if (pid > 0)
				{
					var p = Process.GetProcessById(pid);
					if (p != null)
						processName = p.ProcessName;
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

			string txt = null;
			switch (processName)
			{
				case "iexplore":
					txt = GetHtmlControlText(hWnd);
					break;
				default:
					txt = GetWindowControlText(hWnd);
					break;
			}
			return txt;
		}

		private static string GetWindowControlText(IntPtr hWnd)
		{
			string text = null;
			try
			{
				int cid = Kernel32.GetCurrentThreadId();
				int tid = User32.GetWindowThreadProcessId(hWnd, IntPtr.Zero);
				User32.AttachThreadInput(tid, cid, true);
				{
					IntPtr hFocus = User32.GetFocus();
					if (hFocus != IntPtr.Zero)
					{
						StringBuilder sb = new StringBuilder(4096);
						User32.SendMessage(hFocus, User32.WM_GETTEXT, 4096, sb);
						text = sb.ToString();
					}
				}
				User32.AttachThreadInput(tid, cid, false);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return text;
		}

		private static string GetHtmlControlText(IntPtr hWnd)
		{
			string txt = null;
			//try
			//{
			//    // get document.
			//    mshtml.HTMLDocument doc = null;
			//    SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
			//    foreach (SHDocVw.InternetExplorer ie in shellWindows)
			//    {
			//        if (ie.HWND == hWnd.ToInt32())
			//        {
			//            doc = ie.Document as mshtml.HTMLDocument;
			//            break;
			//        }
			//    }
			//    if (doc != null)
			//    {
			//        var elem = doc.activeElement;
			//        if (elem.isTextEdit)
			//        {
			//            if (elem is mshtml.IHTMLInputTextElement)
			//                txt = (elem as mshtml.IHTMLInputTextElement).value;
			//            else if (elem is mshtml.IHTMLTextAreaElement)
			//                txt = (elem as mshtml.IHTMLTextAreaElement).value;
			//            else
			//                txt = elem.getAttribute("value") as string;
			//        }
			//    }
			//}
			//catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return txt;
		}
		
		#endregion

		#region Windows API
		class User32
		{
			public const int WM_GETTEXT = 0x00D;

			[DllImport("user32.dll", EntryPoint = "GetWindowText", SetLastError = true)]
			public static extern void GetWindowText(IntPtr hWnd, StringBuilder sb, int nMaxCount);

			[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
			public static extern IntPtr GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

			[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
			public static extern int GetWindowThreadProcessId(IntPtr hwnd, IntPtr lpdwProcessId);

			[DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError=true)]
			public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

			[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError=true)]
			public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, StringBuilder lParam);

			[DllImport("user32.dll", EntryPoint = "AttachThreadInput", SetLastError=true)]
			public static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

			[DllImport("user32.dll", EntryPoint = "GetFocus", SetLastError=true)]
			public static extern IntPtr GetFocus();
		}

		class Kernel32
		{
			[DllImport("Kernel32.dll", EntryPoint = "GetCurrentThreadId", SetLastError=true)]
			public static extern int GetCurrentThreadId();
		}
		#endregion
	}
}

//private static string _GetIEDocument()
	//{
	//    SHDocVw.ShellWindows sws = new SHDocVw.ShellWindows();
	//    StringBuilder sb = new StringBuilder();

	//    foreach (SHDocVw.InternetExplorer iw in sws)
	//    {
	//        //获取窗口的URL
	//        sb.AppendLine(iw.LocationURL);  //ie进程的地址
	//        object a = iw.Document;
	//        Type type = a.GetType();
	//        mshtml.HTMLDocumentClass aa = a as mshtml.HTMLDocumentClass;
	//        if (aa != null)
	//        {
	//            string stringvalue = aa.all.length.ToString();

	//            aa.url = "http://www.baidu.com";
	//        }
	//    }
	//    return sb.ToString();
	//}


//[DllImport("User32.dll")]
//public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
//[DllImport("user32.dll")]
//public static extern int EnumWindows(EnumWindowsProc hWnd, IntPtr lParam);
//[DllImport("user32.dll", CharSet = CharSet.Auto)]
//public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpText, int nCount);
//[DllImport("user32.dll")]
//public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, int lParam);

//public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
//public delegate bool EnumChildProc(IntPtr hWnd, int lParam);
