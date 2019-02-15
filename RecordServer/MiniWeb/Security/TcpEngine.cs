using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;

namespace bfbd.MiniWeb.Security
{
	static partial class TcpEngine
	{
		public static int GetConnectedProcess(IPEndPoint remoteEndPoint)
		{
			Debug.Assert(IPAddress.IsLoopback(remoteEndPoint.Address));
			{
				byte[] bsPort = BitConverter.GetBytes(remoteEndPoint.Port);
				int dwPort = BitConverter.ToUInt16(new byte[] { bsPort[1], bsPort[0] }, 0);
				if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					foreach (var conn in EnumTcpConnectionsV4())
					{
						if (conn.dwLocalPort == dwPort)
							return conn.dwOwningPid;
					}
				}
				if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
				{
					foreach (var conn in EnumTcpConnectionsV6())
					{
						if (conn.dwLocalPort == dwPort)
							return conn.dwOwningPid;
					}
				}
				return 0;
			}
		}

		//public static int GetListeningProcess(int port)
		//{
		//    throw new NotImplementedException();
		//}
	}

	partial class TcpEngine
	{
		private static IEnumerable<MIB_TCPROW_OWNER_PID> EnumTcpConnectionsV4()
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

		private static IEnumerable<MIB_TCP6ROW_OWNER_PID> EnumTcpConnectionsV6()
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
	}

	partial class TcpEngine
	{
		#region iphlpapi

		class iphlpapi
		{
			[DllImport("iphlpapi.dll", EntryPoint = "GetExtendedTcpTable", SetLastError = true)]
			public static extern int GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);
		}

		[StructLayout(LayoutKind.Sequential)]
		class MIB_TCPROW_OWNER_PID
		{
			public int dwState;
			public int dwLocalAddr;
			public int dwLocalPort;
			public int dwRemoteAddr;
			public int dwRemotePort;
			public int dwOwningPid;
		}

		[StructLayout(LayoutKind.Sequential)]
		class MIB_TCP6ROW_OWNER_PID
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

		enum TCP_TABLE_CLASS
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

		enum TcpState
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
	}
}
