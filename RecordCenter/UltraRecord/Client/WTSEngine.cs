using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

namespace bfbd.UltraRecord.Client
{
	using bfbd.WindowsAPI.WTS;

	static partial class WTSEngine
	{
		public static bool IsSessionActive(int sessionId)
		{
			bool isActive = false;
			try
			{
				IntPtr pSessions = IntPtr.Zero;
				int count = 0;
				if (wtsapi32.WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref pSessions, ref count))
				{
					WTS_SESSION_INFO si = new WTS_SESSION_INFO();
					IntPtr pSession = pSessions;
					for (int i = 0; i < count; ++i)
					{
						//WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(pSession, typeof(WTS_SESSION_INFO));
						Marshal.PtrToStructure(pSession, si);
						pSession = new IntPtr(pSession.ToInt64() + Marshal.SizeOf(typeof(WTS_SESSION_INFO)));
						if (si.SessionId == sessionId)
						{
							isActive = (si.State == WTS_CONNECTSTATE_CLASS.WTSActive);
							break;
						}
					}
					wtsapi32.WTSFreeMemory(pSessions);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return isActive;
		}

		public static bool SetProcessPrivileges(int processId, params string[] privilegesName)
		{
			bool succeed = false;
			if (processId == 0)
				processId = Process.GetCurrentProcess().Id;

			IntPtr hProcess = OpenProcess(MAXIMUM_ALLOWED, false, (uint)processId);
			if (hProcess != IntPtr.Zero)
			{
				IntPtr hToken;
				if (advapi32.OpenProcessToken(hProcess, TokenAccess.TOKEN_ALL_ACCESS, out hToken))
				{
					succeed = true;
					foreach (string privilege in privilegesName)
						succeed &= SetTokenPrivilege(hToken, privilege);
					
					CloseHandle(hToken);
				}
				CloseHandle(hProcess);
			}
			return succeed;
		}

		private static bool SetTokenPrivilege(IntPtr hToken, string privilegeName)
		{
			LUID luid = new LUID();
			if (advapi32.LookupPrivilegeValue(null, privilegeName, ref luid))
			{
				TOKEN_PRIVILEGES priv = new TOKEN_PRIVILEGES();
				priv.PrivilegeCount = 1;
				priv.Luid = luid;
				priv.Attributes = util.SE_PRIVILEGE_ENABLED;

				if (advapi32.AdjustTokenPrivileges(hToken, false, ref priv, 0, IntPtr.Zero, IntPtr.Zero))
					return true;
			}
			return false;
		}
	}

	partial class WTSEngine
	{
		public static int[] GetActiveSessions()
		{
			List<int> sessions = new List<int>();
			try
			{
				IntPtr pSessions = IntPtr.Zero;
				int count = 0;
				if (wtsapi32.WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref pSessions, ref count))
				{
					IntPtr pSession = pSessions;
					for (int i = 0; i < count; ++i)
					{
						WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(pSession, typeof(WTS_SESSION_INFO));
						pSession = new IntPtr(pSession.ToInt64() + Marshal.SizeOf(typeof(WTS_SESSION_INFO)));
						if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
							sessions.Add(si.SessionId);
					}
					wtsapi32.WTSFreeMemory(pSessions);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return sessions.ToArray();
		}

		public static string GetDomainUserBySessionId(int winSessionId)
		{
			string domainUser = null;
			try
			{
				StringBuilder sbDomain = new StringBuilder();
				StringBuilder sbUsername = new StringBuilder();
				int bsCount;
				if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, winSessionId, WTS_INFO_CLASS.WTSDomainName, out sbDomain, out bsCount))
				{
					if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, winSessionId, WTS_INFO_CLASS.WTSUserName, out sbUsername, out bsCount))
					{
						return string.Format(@"{0}\{1}", sbDomain, sbUsername);
					}
				}
				throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "GetDomainUserBySessionId" };
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			return domainUser;
		}
	}

	partial class WTSEngine
	{
		#region Constants

		public const int TOKEN_DUPLICATE = 0x0002;
		public const uint MAXIMUM_ALLOWED = 0x2000000;

		#endregion

		#region Win32 API Imports

		[DllImport("Wtsapi32.dll", EntryPoint = "WTSQuerySessionInformation", SetLastError = true)]
		public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out StringBuilder ppBuffer, out int pBytesReturned);

		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
		static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
		private static extern bool CloseHandle(IntPtr hSnapshot);

		#endregion
	}
}

//[Obsolete]
//public static int CreateProcessAsUser(int sessionId, string filename, string arguments)
//{
//    int processId = 0;
//    IntPtr hToken = IntPtr.Zero;
//    try
//    {
//        if (wtsapi32.WTSQueryUserToken(sessionId, out hToken))
//        {
//            // test for rdp
//            {
//                bool ret = WTSEngine.SetTokenPrivilege(hToken, NtPrivileges.SE_DEBUG_NAME);
//                TraceLogger.Instance.WriteLineInfo("Set process token: " + ret);
//            }

//            string cmdLine = string.Format("{0} {1}", filename, arguments);
//            SECURITY_ATTRIBUTES sap = new SECURITY_ATTRIBUTES();
//            SECURITY_ATTRIBUTES sat = new SECURITY_ATTRIBUTES();
//            STARTUPINFO si = new STARTUPINFO();
//            sap.nLength = Marshal.SizeOf(sap);
//            sat.nLength = Marshal.SizeOf(sat);
//            si.cb = Marshal.SizeOf(si);
//            si.lpDesktop = @"WinSta0\Default";

//            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
//            if (advapi32.CreateProcessAsUser(hToken, null, cmdLine, ref sap, ref sat, false, 0, IntPtr.Zero, null, ref si, out pi))
//            {
//                if (pi.hProcess != null)
//                    kernel32.CloseHandle(pi.hProcess);
//                if (pi.hThread != null)
//                    kernel32.CloseHandle(pi.hThread);
//                processId = pi.dwProcessID;
//            }
//            else { throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "CreateProcessAsUser" }; }
//        }
//        else { throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "WTSQueryUserToken" }; }
//    }
//    catch (Win32Exception ex) {
//        TraceLogger.Instance.WriteLineError(string.Format(">>>Win32Exception: ErrorCode={0}, Message={1}, Source={2}, time={3:o}", 
//            ex.NativeErrorCode, ex.Message, ex.Source, DateTime.Now));
//        throw; 
//    }
//    catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
//    finally
//    {
//        if (hToken != IntPtr.Zero)
//            kernel32.CloseHandle(hToken);
//    }
//    return processId;
//}

//[Obsolete]
//public static bool SetCurrentPrivileges(params string[] privilegesName)
//{
//    bool succeed = false;
//    IntPtr hProcess = System.Diagnostics.Process.GetCurrentProcess().Handle;
//    IntPtr hToken;
//    if (advapi32.OpenProcessToken(hProcess, TokenAccess.TOKEN_ALL_ACCESS, out hToken))
//    {
//        succeed = true;
//        foreach(string privilege in privilegesName)
//            succeed &= SetTokenPrivilege(hToken, privilege);
//    }
//    return succeed;
//}

