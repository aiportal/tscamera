using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bfbd.MiniWeb.Security
{
	partial class AccessPolicy
	{
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		public bool IsAccessValid(IPEndPoint remote)
		{
			if (IPAddress.IsLoopback(remote.Address))
			{
				if (CacheEnabled)
				{
					bool permit = true;
					if (_cache["port_" + remote.Port.ToString()] == null)
					{
						if (IsPermitedUser(remote))
							_cache["port_" + remote.Port.ToString()] = string.Empty;
						else
							return false;
					}
					return permit;
				}
				else
					return (IsPermitedUser(remote) && IsPermitedProcess(remote));
			}
			else
				return (IsPermitedAddress(remote) && IsPermitedAddressRange(remote) && IsPermitedMac(remote));
		}

		public bool CacheEnabled { get; set; }

		public string PermitUser { get; set; }
		public string PermitProcess { get; set; }

		public IPAddress PermitAddress { get; set; }
		public IPAddressRange PermitAddressRange { get; set; }
		public PhysicalAddress PermitMac { get; set; }
	}

	partial class AccessPolicy
	{
		private bool IsPermitedUser(IPEndPoint remote)
		{
			Debug.Assert(IPAddress.IsLoopback(remote.Address));
			if (string.IsNullOrEmpty(PermitUser))
				return true;

			int pid = TcpEngine.GetConnectedProcess(remote);
			if (pid > 0)
			{
				string user = WtsEngine.GetProcessOwner(pid);
				return string.Equals(user, PermitUser, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				return false;
			}
		}

		private bool IsPermitedProcess(IPEndPoint remote)
		{
			Debug.Assert(IPAddress.IsLoopback(remote.Address));
			if (string.IsNullOrEmpty(PermitProcess))
				return true;

			int pid = TcpEngine.GetConnectedProcess(remote);
			if (pid > 0)
			{
				string name = Process.GetProcessById(pid).ProcessName;
				return string.Equals(name, PermitProcess, StringComparison.OrdinalIgnoreCase);
			}
			else
				return false;
		}

		private bool IsPermitedAddress(IPEndPoint remote)
		{
			if (PermitAddress == null)
				return true;

			return (remote.Address.ToString() == PermitAddress.ToString());
		}

		private bool IsPermitedAddressRange(IPEndPoint remote)
		{
			if (PermitAddressRange == null)
				return true;

			return PermitAddressRange.Contains(remote.Address);
		}

		private bool IsPermitedMac(IPEndPoint remote)
		{
			if (PermitMac == null)
				return true;

			return PermitMac == GetMacAddress(remote.Address);
		}

		private PhysicalAddress GetMacAddress(IPAddress remoteAddr)
		{
			Debug.Assert(remoteAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

			int ip = BitConverter.ToInt32(remoteAddr.GetAddressBytes(), 0);
			int length = 6;
			byte[] bs = new byte[length];
			SendARP(ip, 0, bs, ref length);
			return new PhysicalAddress(bs);
		}

		[DllImport("Iphlpapi.dll")]
		static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);  
	}
}
