using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace bfbd.UltraRecord
{
	partial class user32
	{
		[DllImport("user32.dll", EntryPoint = "GetForegroundWindow", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
		public static extern IntPtr GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

		//[DllImport("user32.dll", EntryPoint = "CreateWindowEx", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern IntPtr CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, int style, int x, int y, int width, int height, IntPtr hWndParent, IntPtr hMenu, IntPtr hInst, [MarshalAs(UnmanagedType.AsAny)]object pvParam);

		//[DllImport("user32.dll", EntryPoint = "RegisterClass", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern short RegisterClass(WNDCLASS wc);

		//[DllImport("user32.dll", EntryPoint = "TranslateMessage", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern bool TranslateMessage([In(), Out()]ref MSG msg);

		//[DllImport("user32.dll", EntryPoint = "DispatchMessage", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern int DispatchMessage([In()]ref MSG msg);

		//[DllImport("user32.dll", EntryPoint = "DefWindowProc", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		//[DllImport("user32.dll", EntryPoint = "PostQuitMessage", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern void PostQuitMessage(int nExitCode);

		//[DllImport("user32.dll", EntryPoint = "GetMessage", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern int GetMessage([In(), Out()]ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);

		//[DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true, CharSet = CharSet.Auto)]
		//public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, long lParam);

		[DllImport("user32.dll", EntryPoint = "MapVirtualKey", SetLastError = true)]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);
	}

	partial class util
	{
		public const uint MAPVK_VK_TO_CHAR = 2;
	}
}

