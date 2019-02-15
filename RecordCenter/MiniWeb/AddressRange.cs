using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace bfbd.MiniWeb
{
	class AddressRange
	{
		public IPAddress MinAddress { get; private set; }
		public IPAddress MaxAddress { get; private set; }

		public override string ToString()
		{
			return string.Format("{0}-{1}", MinAddress, MaxAddress);
		}

		public static bool TryParse(string strRange, out AddressRange range)
		{
			range = null;
			if (!string.IsNullOrEmpty(strRange) && strRange.Contains("-"))
			{
				string[] ss = strRange.Split('-');
				if (ss.Length == 2)
				{
					IPAddress ip1, ip2;
					if (IPAddress.TryParse(ss[0], out ip1) && IPAddress.TryParse(ss[1], out ip2))
					{
						if (BitConverter.ToUInt32(ip1.GetAddressBytes(), 0) < BitConverter.ToUInt32(ip2.GetAddressBytes(), 0))
						{
							range = new AddressRange()
							{
								MinAddress = ip1,
								MaxAddress = ip2
							};
						}
						else
						{
							range = new AddressRange()
							{
								MinAddress = ip2,
								MaxAddress = ip1
							};
						}
						return true;
					}
				}
			}
			return false;
		}

		public static bool Contains(string strRange, string strAddress)
		{
			AddressRange range;
			IPAddress address;
			if (TryParse(strRange, out range) && IPAddress.TryParse(strAddress, out address))
			{
				foreach (IPAddress addr in Dns.GetHostEntry(address).AddressList)
				{
					if (addr.AddressFamily == AddressFamily.InterNetwork)
					{
						uint min = BitConverter.ToUInt32(range.MinAddress.GetAddressBytes(), 0);
						uint max = BitConverter.ToUInt32(range.MaxAddress.GetAddressBytes(), 0);
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
