using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security;
using System.ComponentModel;

namespace bfbd.TSCamera.Client
{
	using bfbd.Common;

	static partial class ProcessEngine
	{
		public static int CreateProcessAsUser(int sessionId, string filename, string arguments)
		{
			int processId = 0;
			IntPtr hToken = IntPtr.Zero;
			try
			{
				if (WTSQueryUserToken(sessionId, out hToken))
				{
					string cmdLine = string.Format("{0} {1}", filename, arguments);
					SECURITY_ATTRIBUTES sap = new SECURITY_ATTRIBUTES();
					SECURITY_ATTRIBUTES sat = new SECURITY_ATTRIBUTES();
					STARTUPINFO si = new STARTUPINFO();
					sap.Length = Marshal.SizeOf(sap);
					sat.Length = Marshal.SizeOf(sat);
					si.cb = Marshal.SizeOf(si);
					si.lpDesktop = @"WinSta0\Default";

					PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
					if (CreateProcessAsUser(hToken, null, cmdLine, ref sap, ref sat, false, 0, IntPtr.Zero, null, ref si, out pi))
					{
						if (pi.hProcess != null)
							CloseHandle(pi.hProcess);
						if (pi.hThread != null)
							CloseHandle(pi.hThread);
						processId = (int)pi.dwProcessId;
					}
					else { throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "CreateProcessAsUser" }; }
				}
				else { throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "WTSQueryUserToken" }; }
			}
			catch (Win32Exception ex)
			{
				TraceLog.WriteLineError(string.Format(">>>Win32Exception: ErrorCode={0}, Message={1}, Source={2}, time={3:o}",
					ex.NativeErrorCode, ex.Message, ex.Source, DateTime.Now));
				throw;
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			finally
			{
				if (hToken != IntPtr.Zero)
					CloseHandle(hToken);
			}
			return processId;
		}

		public static int CreateProcessAsAdmin(int sessionId, string filename, string arugments)
		{
			IntPtr hToken = IntPtr.Zero;
			try
			{
				Process process = Array.Find(Process.GetProcessesByName("winlogon"), p => p.SessionId == sessionId);
				if (process != null)
				{
					IntPtr hProcess = OpenProcess(MAXIMUM_ALLOWED, false, (uint)process.Id);
					process.Close();
					if (hProcess != IntPtr.Zero)
					{
						//Debug.Assert(hProcess == process.Handle);
						if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hToken))
							throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "OpenProcessToken" };

						CloseHandle(hProcess);
					}
					else
						throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "OpenProcess" };
				}
				else
					TraceLog.WriteLineWarning("Process winlogon not find at sessionId: " + sessionId);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); }

			int processId = 0;
			try
			{
				if (hToken != IntPtr.Zero)
				{
					IntPtr hUserTokenDup = IntPtr.Zero;
					SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
					sa.Length = Marshal.SizeOf(sa);

					if (DuplicateTokenEx(hToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
					{
						STARTUPINFO si = new STARTUPINFO();
						si.cb = (int)Marshal.SizeOf(si);
						si.lpDesktop = @"winsta0\default";

						PROCESS_INFORMATION pi;
						string cmdLine = string.Format("{0} {1}", filename, arugments);
						bool ret = CreateProcessAsUser(hUserTokenDup,			// client's access token
														null,                   // file to execute
														cmdLine,				// command line
														ref sa,                 // pointer to process SECURITY_ATTRIBUTES
														ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
														false,                  // handles are not inheritable
														0,						// creation flags
														IntPtr.Zero,            // pointer to new environment block 
														null,                   // name of current directory 
														ref si,                 // pointer to STARTUPINFO structure
														out pi					// receives information about new process
														);
						CloseHandle(hUserTokenDup);
						if (ret)
							processId = (int)pi.dwProcessId;
						else
							throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "CreateProcessAsUser" };
					}
					else
						throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "DuplicateTokenEx" };

					CloseHandle(hToken);
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); }
			return processId;
		}
	}

	partial class ProcessEngine
	{
		#region Structures

		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES
		{
			public int Length;
			public IntPtr lpSecurityDescriptor;
			public bool bInheritHandle;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct STARTUPINFO
		{
			public int cb;
			public String lpReserved;
			public String lpDesktop;
			public String lpTitle;
			public uint dwX;
			public uint dwY;
			public uint dwXSize;
			public uint dwYSize;
			public uint dwXCountChars;
			public uint dwYCountChars;
			public uint dwFillAttribute;
			public uint dwFlags;
			public short wShowWindow;
			public short cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public uint dwProcessId;
			public uint dwThreadId;
		}

		#endregion

		#region Enumerations

		enum TOKEN_TYPE : int
		{
			TokenPrimary = 1,
			TokenImpersonation = 2
		}

		enum SECURITY_IMPERSONATION_LEVEL : int
		{
			SecurityAnonymous = 0,
			SecurityIdentification = 1,
			SecurityImpersonation = 2,
			SecurityDelegation = 3,
		}

		#endregion

		#region Constants

		const int TOKEN_DUPLICATE = 0x0002;
		const uint MAXIMUM_ALLOWED = 0x2000000;
		const int CREATE_NEW_CONSOLE = 0x00000010;

		const int IDLE_PRIORITY_CLASS = 0x40;
		const int NORMAL_PRIORITY_CLASS = 0x20;
		const int HIGH_PRIORITY_CLASS = 0x80;
		const int REALTIME_PRIORITY_CLASS = 0x100;

		#endregion

		#region Windows API

		[DllImport("wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
		public static extern bool WTSQueryUserToken(Int32 sessionId, out IntPtr Token);

		[DllImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
		static extern uint WTSGetActiveConsoleSessionId();

		[DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		public extern static bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
			ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
			String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32.dll", EntryPoint = "ProcessIdToSessionId")]
		static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

		[DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
		public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType, int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError=true)]
		static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("advapi32", EntryPoint="OpenProcessToken", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
		public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

		[DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hSnapshot);

		#endregion
	}
}
