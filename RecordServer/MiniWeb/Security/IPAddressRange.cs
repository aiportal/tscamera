using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	class IPAddressRange
	{
		public static readonly char Separator = '-';

		public IPAddress AddressMin { get; private set; }
		public IPAddress AddressMax { get; private set; }

		public bool Contains(IPAddress addr)
		{
			Debug.Assert(AddressMin.AddressFamily == AddressFamily.InterNetwork && AddressMax.AddressFamily == AddressFamily.InterNetwork);
			Debug.Assert(addr.AddressFamily == AddressFamily.InterNetwork);

			uint min = BitConverter.ToUInt32(AddressMin.GetAddressBytes(), 0);
			uint max = BitConverter.ToUInt32(AddressMax.GetAddressBytes(), 0);
			uint ip = BitConverter.ToUInt32(addr.GetAddressBytes(), 0);
			return (min <= ip && ip <= max);
		}

		public override string ToString()
		{
			return string.Format("{0}-{1}", AddressMin, AddressMax);
		}

		public static bool TryParse(string strRange, out IPAddressRange range)
		{
			range = null;
			if (string.IsNullOrEmpty(strRange))
				return false;

			string[] ss = strRange.Split('-');
			if (ss.Length == 2)
			{
				IPAddress ip1, ip2;
				if (IPAddress.TryParse(ss[0], out ip1) && IPAddress.TryParse(ss[1], out ip2))
				{
					Debug.Assert(ip1.AddressFamily == AddressFamily.InterNetwork && ip2.AddressFamily == AddressFamily.InterNetwork);
					bool order = BitConverter.ToUInt32(ip1.GetAddressBytes(), 0) < BitConverter.ToUInt32(ip2.GetAddressBytes(), 0);
					range = new IPAddressRange()
					{
						AddressMin = (order ? ip1 : ip2),
						AddressMax = (order ? ip2 : ip1)
					};
					return true;
				}
			}
			return false;
		}

		[Obsolete]
		public static bool Contains(string strRange, string strAddress)
		{
			IPAddressRange range;
			IPAddress address;
			if (TryParse(strRange, out range) && IPAddress.TryParse(strAddress, out address))
			{
				foreach (IPAddress addr in Dns.GetHostEntry(address).AddressList)
				{
					if (addr.AddressFamily == AddressFamily.InterNetwork)
					{
						uint min = BitConverter.ToUInt32(range.AddressMin.GetAddressBytes(), 0);
						uint max = BitConverter.ToUInt32(range.AddressMax.GetAddressBytes(), 0);
						uint ip = BitConverter.ToUInt32(addr.GetAddressBytes(), 0);
						if (min <= ip && ip <= max)
							return true;
					}
				}
			}
			return false;
		}
	}
}
