using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.License
{
	using bfbd.Common.License;

	public partial class SerialNumber
	{
		public LicenseType License;
		public DateTime CreateTime;
		public DateTime ExpireTime;
		public string MachineId;

		public SerialNumber()
		{
			MachineId = new MachineInfo().GetMachineId(HardwareType.Driver);
		}

		//public string Serialize(string keyName)
		//{
		//    var str = string.Format("{0},{1:X},{2},{3}",
		//        (int)License, CreateTime.ToBinary(), ExpireTime.Subtract(CreateTime).Days, MachineId);
		//    return RSA.Encrypt(str, keyName);
		//}

		public static SerialNumber DeSerialize(string str, DateTime installTime)
		{
			SerialNumber sn = null;
			if (!string.IsNullOrEmpty(str))
			{
				string[] ss = RSA.Decrypt(str, "Monkey").Split(',');
				if (ss.Length > 3)
				{
					var stm = DateTime.FromBinary(Convert.ToInt64(ss[1], 16));
					sn = new SerialNumber()
					{
						License = (LicenseType)Convert.ToInt32(ss[0]),
						CreateTime = stm,
						ExpireTime = stm.AddDays(Convert.ToInt32(ss[2])),
						MachineId = ss[3]
					};
					var span = sn.ExpireTime.Subtract(sn.CreateTime);
					sn.CreateTime = installTime;
					sn.ExpireTime = installTime.Add(span);
				}
			}
			return sn;
		}

		public bool IsValid()
		{
			switch (License)
			{
				case LicenseType.Demo:
					{
						return (CreateTime < DateTime.Now && DateTime.Now < ExpireTime);
					}
				case LicenseType.Professional:
					{
						// check MachineId to local driver.
						string mid = new MachineInfo().GetMachineId(HardwareType.Driver);
						if (MachineId == mid)
							return (CreateTime < DateTime.Now && DateTime.Now < ExpireTime);
						else
							return false;
					}
				case LicenseType.Enterprise:
					{
						// check MachineId to server mac.
						//if (string.IsNullOrEmpty(serverHost))
							//serverHost = System.Net.Dns.GetHostName();

						//var entry = System.Net.Dns.GetHostEntry(serverHost);
						//]Iphlpapi.dll]
						//DWORD SendARP(
						//  _In_     IPAddr DestIP,
						//  _In_     IPAddr SrcIP,
						//  _Out_    PULONG pMacAddr,
						//  _Inout_  PULONG PhyAddrLen
						//);
						throw new NotImplementedException();
					}
				default:
					return false;
			}
		}
	}

	public enum LicenseType
	{
		None = 0,
		Demo = 1,
		Professional = 2,
		Enterprise = 3,
	}
}
