using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace bfbd.TSCamera.Client
{
	using bfbd.Common;

	static partial class WTSEngine
	{
		internal static bool IsSessionActive(int sessionId)
		{
			return Array.Exists(GetActiveSessions(), s => s == sessionId);
		}

		internal static int[] GetActiveSessions()
		{
			List<int> sessions = new List<int>();
			try
			{
				IntPtr pSessions = IntPtr.Zero;
				int count = 0;
				if (wtsapi32.WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref pSessions, ref count))
				{
					WTS_SESSION_INFO si = new WTS_SESSION_INFO();
					IntPtr p = pSessions;
					for (int i = 0; i < count; ++i)
					{
						Marshal.PtrToStructure(p, si);
						if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
							sessions.Add(si.SessionId);
						p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(WTS_SESSION_INFO)));
					}
					wtsapi32.WTSFreeMemory(pSessions);
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return sessions.ToArray();
		}

		internal static string GetDomainUserBySessionId(int winSessionId)
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
			catch (Exception ex) { TraceLog.WriteException(ex); }
			return domainUser;
		}
	}

	partial class WTSEngine
	{
		#region Windows API

		class wtsapi32
		{
			[DllImport("wtsapi32.dll", EntryPoint = "WTSEnumerateSessions", SetLastError = true)]
			internal static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, uint Version, ref IntPtr ppSessionInfo, ref int pCount);

			[DllImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformation", SetLastError = true)]
			internal static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out StringBuilder ppBuffer, out int pBytesReturned);

			[DllImport("wtsapi32", EntryPoint = "WTSFreeMemory", SetLastError = true)]
			internal static extern void WTSFreeMemory(IntPtr pMemory);
		}

		#endregion Windows API

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		class WTS_SESSION_INFO
		{
			public Int32 SessionId;
			public String pWinStationName;
			public WTS_CONNECTSTATE_CLASS State;
		}

		#endregion Structs

		#region Enum

		enum WTS_CONNECTSTATE_CLASS
		{
			WTSActive,
			WTSConnected,
			WTSConnectQuery,
			WTSShadow,
			WTSDisconnected,
			WTSIdle,
			WTSListen,
			WTSReset,
			WTSDown,
			WTSInit
		}

		enum WTS_INFO_CLASS
		{
			WTSInitialProgram = 0,
			WTSApplicationName = 1,
			WTSWorkingDirectory = 2,
			WTSOEMId = 3,
			WTSSessionId = 4,
			WTSUserName = 5,
			WTSWinStationName = 6,
			WTSDomainName = 7,
			WTSConnectState = 8,
			WTSClientBuildNumber = 9,
			WTSClientName = 10,
			WTSClientDirectory = 11,
			WTSClientProductId = 12,
			WTSClientHardwareId = 13,
			WTSClientAddress = 14,
			WTSClientDisplay = 15,
			WTSClientProtocolType = 16,
			//WTSClientInfo = 23,
			//WTSSessionInfo = 24,
		}

		#endregion Enum
	}
}
