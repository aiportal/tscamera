using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace bfbd.WindowsAPI.WTS
{
	public partial class advapi32
	{
		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CreateProcessAsUser(
			IntPtr hToken, 
			string lpApplicationName, 
			string lpCommandLine,
			ref SECURITY_ATTRIBUTES lpProcessAttributes, 
			ref SECURITY_ATTRIBUTES lpThreadAttributes, 
			bool bInheritHandles, 
			uint dwCreationFlags,
			IntPtr lpEnvironment, 
			string lpCurrentDirectory, 
			ref STARTUPINFO lpStartupInfo, 
			out PROCESS_INFORMATION lpProcessInformation);
	}

	partial class kernel32
	{
		[DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
		public static extern bool CloseHandle(IntPtr handle);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SECURITY_ATTRIBUTES
	{
		public int nLength;
		public IntPtr lpSecurityDescriptor;
		public bool bInheritHandle;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct STARTUPINFO
	{
		public Int32 cb;
		public string lpReserved;
		public string lpDesktop;
		public string lpTitle;
		public Int32 dwX;
		public Int32 dwY;
		public Int32 dwXSize;
		public Int32 dwYSize;
		public Int32 dwXCountChars;
		public Int32 dwYCountChars;
		public Int32 dwFillAttribute;
		public Int32 dwFlags;
		public Int16 wShowWindow;
		public Int16 cbReserved2;
		IntPtr lpReserved2;
		IntPtr hStdInput;
		IntPtr hStdOutput;
		IntPtr hStdError;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION
	{
		public IntPtr hProcess;
		public IntPtr hThread;
		public Int32 dwProcessID;
		public Int32 dwThreadID;
	}
}
