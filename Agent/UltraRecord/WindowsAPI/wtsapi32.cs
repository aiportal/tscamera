using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace bfbd.UltraRecord
{
	partial class wtsapi32
	{
		[DllImport("wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
		public static extern bool WTSQueryUserToken(Int32 sessionId, out IntPtr Token);

		[DllImport("wtsapi32.dll", EntryPoint = "WTSEnumerateSessions", SetLastError = true)]
		public static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, uint Version, ref IntPtr ppSessionInfo, ref int pCount);

		[DllImport("wtsapi32", EntryPoint = "WTSFreeMemory", SetLastError = true)]
		public static extern void WTSFreeMemory(IntPtr pMemory);

		[DllImport("Wtsapi32.dll", EntryPoint = "WTSQuerySessionInformation", SetLastError = true)]
		public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);
		[DllImport("Wtsapi32.dll", EntryPoint = "WTSQuerySessionInformation", SetLastError = true)]
		public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out StringBuilder ppBuffer, out int pBytesReturned);

		[DllImport("kernel32.dll", EntryPoint = "ProcessIdToSessionId", SetLastError=true)]
		public static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);
	}

	[StructLayout(LayoutKind.Sequential)]
	struct WTS_SESSION_INFO
	{
		public Int32 SessionId;
		public String pWinStationName;
		public WTS_CONNECTSTATE_CLASS State;
	}

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

	/// <summary>
	/// The client network address is reported by the RDP client itself when it connects to the server. 
	/// This could be different than the address that actually connected to the server.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct WTS_CLIENT_ADDRESS
	{
		public uint AddressFamily;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
		public byte[] Address;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct WTS_CLIENT_DISPLAY
	{
		public Int32 HorizontalResolution;
		public Int32 VerticalResolution;
		public Int32 ColorDepth;
	}

	partial class util
	{
		public const int AF_INET = 2;
		public const int AF_INET6 = 23;
	}
}

//[StructLayout(LayoutKind.Sequential)]
//struct WTSINFO
//{
//    public WTS_CONNECTSTATE_CLASS State;
//    public Int32 SessionId;
//    public Int32 IncomingBytes;
//    public Int32 OutgoingBytes;
//    public Int32 IncomingFrames;
//    public Int32 OutgoingFrames;
//    public Int32 IncomingCompressedBytes;
//    public Int32 OutgoingCompressedBytes;
//    public string WinStationName;
//    public string Domain;
//    public string UserName;
//    public Int64 ConnectTime;
//    public Int64 DisconnectTime;
//    public Int64 LastInputTime;
//    public Int64 LogonTime;
//    public Int64 CurrentTime;
//}

//[StructLayout(LayoutKind.Sequential)]
//struct WTSCLIENT
//{
//    public string ClientName;
//    public string Domain;
//    public string UserName;
//    public string WorkDirectory;
//    public string InitialProgram;
//    public byte EncryptionLevel;
//    public Int64 ClientAddressFamily;
//    public Int16[] ClientAddress;
//    public Int16 HRes;
//    public Int16 VRes;
//    public Int16 ColorDepth;
//    public string ClientDirectory;
//    public Int64 ClientBuildNumber;
//    public Int64 ClientHardwareId;
//    public Int16 ClientProductId;
//    public Int16 OutBufCountHost;
//    public Int16 OutBufCountClient;
//    public Int16 OutBufLength;
//    public string DeviceId;
//}