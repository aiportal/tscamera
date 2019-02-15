using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	static partial class WTSEngine
	{
		public static RemoteSessionInfo GetRemoteSessionInfo()
		{
			RemoteSessionInfo rsi = new RemoteSessionInfo();
			try
			{
				int sessionId = Process.GetCurrentProcess().SessionId;
				StringBuilder sb;
				IntPtr ptr;
				int len;

				if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSUserName, out sb, out len))
					rsi.UserName = sb.ToString();
				
				if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSDomainName, out sb, out len))
					rsi.Domain = sb.ToString();
				
				if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSClientName, out sb, out len))
					rsi.ClientName = sb.ToString();

				if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSClientAddress, out ptr, out len))
				{
					WTS_CLIENT_ADDRESS addr = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure(ptr, typeof(WTS_CLIENT_ADDRESS));
					if (addr.AddressFamily == util.AF_INET)
						rsi.ClientAddress = string.Format("{0}.{1}.{2}.{3}", new object[] { addr.Address[2], addr.Address[3], addr.Address[4], addr.Address[5] });
				}
		
				//if (wtsapi32.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSClientDisplay, out ptr, out len))
				//{
				//    WTS_CLIENT_DISPLAY disp = (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure(ptr, typeof(WTS_CLIENT_DISPLAY));
				//    rsi.ClientHResolution = disp.HorizontalResolution;
				//    rsi.ClientVResolution = disp.VerticalResolution;
				//    rsi.ClientColorDepth = disp.ColorDepth;
				//}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return rsi;
		}
	}

	sealed class RemoteSessionInfo
	{
		public string UserName;
		public string Domain;

		public string ClientName;
		public string ClientAddress;

		//public int ClientHResolution;
		//public int ClientVResolution;
		//public int ClientColorDepth;
	}
}
