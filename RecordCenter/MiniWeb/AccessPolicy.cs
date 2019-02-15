using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Management;
using System.ComponentModel;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	using bfbd.Common.Windows;

	partial class AccessPolicy
	{
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		public bool ValidAccess(System.Net.IPEndPoint endPoint)
		{
			bool permit = true;
			if (IPAddress.IsLoopback(endPoint.Address))
			{
				if (!string.IsNullOrEmpty(PermitUser))
				{
					if (_cache["port_" + endPoint.Port.ToString()] != null)
					{
						permit = true;
					}
					else
					{
						try
						{
							var user = GetConnectedUser(endPoint.Port, endPoint.AddressFamily);
							permit = string.Equals(user, PermitUser, StringComparison.OrdinalIgnoreCase);
							if (permit)
								_cache["port_" + endPoint.Port.ToString()] = string.Empty;
						}
						catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
					}
				}
			}
			else if (PermitAddress != null)
			{
				permit = (endPoint.Address.ToString() == PermitAddress.ToString());
			}
			return permit;
		}

		public IPAddress PermitAddress { get; set; }
		public string PermitUser { get; set; }

		//public string AddressMin { get; set; }
		//public string AddressMax { get; set; }

		///? How to get connections info by .NET class.
	}

	partial class AccessPolicy
	{
		private string GetConnectedUser(int processPort, AddressFamily addressFamily)
		{
			int pid = 0;
			int dwProcessPort = BitConverter.ToUInt16(new byte[] { BitConverter.GetBytes(processPort)[1], BitConverter.GetBytes(processPort)[0] }, 0);
			if (addressFamily == AddressFamily.InterNetwork)
			{
				foreach (var conn in EnumTcpConnectionsV4())
				{
					if (conn.dwLocalPort == dwProcessPort)
					{
						pid = conn.dwOwningPid;
						break;
					}
				}
			}
			else if (addressFamily == AddressFamily.InterNetworkV6)
			{
				foreach (var conn in EnumTcpConnectionsV6())
				{
					if (conn.dwLocalPort == dwProcessPort)
					{
						pid = conn.dwOwningPid;
						break;
					}
				}
			}
			if (pid > 0)
			{
				using (Process proc = Process.GetProcessById(pid))
				{
					if (proc != null)
					{
						string user = GetDomainUserBySessionId(proc.SessionId);
						proc.Dispose();
						return DomainUser.Create(user).ToString();
					}
				}
			}
			return null;
		}

		private IEnumerable<MIB_TCPROW_OWNER_PID> EnumTcpConnectionsV4()
		{
			int dwSize = sizeof(int) + Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID)) * 100;
			IntPtr hTcpTable = IntPtr.Zero;
			{
				hTcpTable = Marshal.AllocHGlobal(dwSize);
				int ret = iphlpapi.GetExtendedTcpTable(hTcpTable, ref dwSize, true, util.AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_CONNECTIONS, 0);
				if (ret != util.NO_ERROR)
				{
					// retry for new dwSize.
					Marshal.FreeHGlobal(hTcpTable);
					hTcpTable = Marshal.AllocHGlobal(dwSize);
					ret = iphlpapi.GetExtendedTcpTable(hTcpTable, ref dwSize, true, util.AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_CONNECTIONS, 0);
					if (ret != util.NO_ERROR)
					{
						Marshal.FreeHGlobal(hTcpTable);
						throw new Exception("GetExtendedTcpTable return: " + ret);
					}
				}
			}
			{
				MIB_TCPROW_OWNER_PID item = new MIB_TCPROW_OWNER_PID();
				int dwNumEntries = Marshal.ReadInt32(hTcpTable);
				IntPtr pItem = new IntPtr(hTcpTable.ToInt32() + sizeof(int));
				for (int i = 0; i < dwNumEntries; ++i)
				{
					//var item = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(pItem, typeof(MIB_TCPROW_OWNER_PID));
					Marshal.PtrToStructure(pItem, item);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID)));
					yield return item;
				}
				Marshal.FreeHGlobal(hTcpTable);
			}
		}

		private IEnumerable<MIB_TCP6ROW_OWNER_PID> EnumTcpConnectionsV6()
		{
			int dwSize = sizeof(int) + Marshal.SizeOf(typeof(MIB_TCP6ROW_OWNER_PID)) * 100;
			IntPtr hTcpTable = IntPtr.Zero;
			{
				hTcpTable = Marshal.AllocHGlobal(dwSize);
				int ret = iphlpapi.GetExtendedTcpTable(hTcpTable, ref dwSize, true, util.AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_CONNECTIONS, 0);
				if (ret != util.NO_ERROR)
				{
					// retry for new dwSize.
					Marshal.FreeHGlobal(hTcpTable);
					hTcpTable = Marshal.AllocHGlobal(dwSize);
					ret = iphlpapi.GetExtendedTcpTable(hTcpTable, ref dwSize, true, util.AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_CONNECTIONS, 0);
					if (ret != util.NO_ERROR)
					{
						Marshal.FreeHGlobal(hTcpTable);
						throw new Exception("GetExtendedTcpTable return: " + ret);
					}
				}
			}
			{
				MIB_TCP6ROW_OWNER_PID item = new MIB_TCP6ROW_OWNER_PID();
				int dwNumEntries = Marshal.ReadInt32(hTcpTable);
				IntPtr pItem = new IntPtr(hTcpTable.ToInt32() + sizeof(int));
				for (int i = 0; i < dwNumEntries; ++i)
				{
					//var item = (MIB_TCP6ROW_OWNER_PID)Marshal.PtrToStructure(pItem, typeof(MIB_TCP6ROW_OWNER_PID));
					Marshal.PtrToStructure(pItem, item);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(MIB_TCP6ROW_OWNER_PID)));
					yield return item;
				}
				Marshal.FreeHGlobal(hTcpTable);
			}
		}
		
		private string GetDomainUserBySessionId(int winSessionId)
		{
			string domainUser = null;
			try
			{
				StringBuilder sbDomain = new StringBuilder();
				StringBuilder sbUsername = new StringBuilder();
				int bsCount;
				if (Wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, winSessionId, WTS_INFO_CLASS.WTSDomainName, out sbDomain, out bsCount))
				{
					if (Wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, winSessionId, WTS_INFO_CLASS.WTSUserName, out sbUsername, out bsCount))
					{
						return string.Format(@"{0}\{1}", sbDomain, sbUsername);
					}
				}
				throw new Win32Exception(Marshal.GetLastWin32Error()) { Source = "GetDomainUserBySessionId" };
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return domainUser;
		}
	}

	partial class AccessPolicy
	{
		#region iphlpapi

		public class iphlpapi
		{
			[DllImport("iphlpapi.dll", EntryPoint="GetExtendedTcpTable", SetLastError = true)]
			public static extern int GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MIB_TCPROW_OWNER_PID
		{
			public int dwState;
			public int dwLocalAddr;
			public int dwLocalPort;
			public int dwRemoteAddr;
			public int dwRemotePort;
			public int dwOwningPid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MIB_TCP6ROW_OWNER_PID
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ucLocalAddr;
			public int dwLocalScopeId;
			public int dwLocalPort;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ucRemoteAddr;
			public int dwRemoteScopeId;
			public int dwRemotePort;
			public int dwState;
			public int dwOwningPid;
		}

		public enum TCP_TABLE_CLASS
		{
			TCP_TABLE_BASIC_LISTENER,
			TCP_TABLE_BASIC_CONNECTIONS,
			TCP_TABLE_BASIC_ALL,
			TCP_TABLE_OWNER_PID_LISTENER,
			TCP_TABLE_OWNER_PID_CONNECTIONS,
			TCP_TABLE_OWNER_PID_ALL,
			TCP_TABLE_OWNER_MODULE_LISTENER,
			TCP_TABLE_OWNER_MODULE_CONNECTIONS,
			TCP_TABLE_OWNER_MODULE_ALL,
		}

		public enum TcpState
		{
			CLOSED = 1,
			LISTEN = 2,
			SYN_SENT = 3,
			SYN_RCVD = 4,
			ESTAB = 5,
			FIN_WAIT1 = 6,
			FIN_WAIT2 = 7,
			CLOSE_WAIT = 8,
			CLOSING = 9,
			LAST_ACK = 10,
			TIME_WAIT = 11,
			DELETE_TCB = 12,
		}

		class util
		{
			public const int NO_ERROR = 0;
			public const int AF_INET = 2;    // IP_v4
			public const int AF_INET6 = 23;
		}

		#endregion

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
