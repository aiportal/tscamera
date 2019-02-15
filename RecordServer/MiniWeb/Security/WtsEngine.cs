using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bfbd.MiniWeb.Security
{
	using bfbd.Common;

	static partial class WtsEngine
	{
		public static string GetProcessOwner(int processId)
		{
			Debug.Assert(processId > 0);
			using (Process process = Process.GetProcessById(processId))
			{
				if (process != null)
					return GetSessionUser(process.SessionId);
				else
					return null;
			}
		}

		private static string GetSessionUser(int sessionId)
		{
			string domainUser = null;
			try
			{
				StringBuilder sbDomain = new StringBuilder(50);
				StringBuilder sbUsername = new StringBuilder(50);
				int bsCount;
				if (Wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSDomainName, out sbDomain, out bsCount))
				{
					if (Wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSUserName, out sbUsername, out bsCount))
					{
						if (!string.Equals(Environment.MachineName, sbDomain.ToString(), StringComparison.OrdinalIgnoreCase))
							domainUser = string.Format(@"{0}\{1}", sbDomain, sbUsername);
						else
							domainUser = sbUsername.ToString();
					}
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return domainUser;
		}
	}

	partial class WtsEngine
	{
		#region Wtsapi32

		class Wtsapi32
		{
			[DllImport("Wtsapi32.dll", EntryPoint = "WTSQuerySessionInformation", SetLastError = true)]
			public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out StringBuilder ppBuffer, out int pBytesReturned);
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

		#endregion
	}
}
