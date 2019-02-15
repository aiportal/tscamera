using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.License
{
	using bfbd.Common.License;

	partial class SerialNumber
	{
		public LicenseType LicenseType;
		public DateTime CreateTime;
		public DateTime ExpireTime;
		public string MachineId;
		public int MaxActivity;

		public SerialNumber()
		{
			MachineId = new MachineInfo().GetMachineId(HardwareType.Driver);
		}

		public static SerialNumber DeSerialize(string licContent, string keyName, DateTime installTime)
		{
			SerialNumber sn = null;
			if (!string.IsNullOrEmpty(licContent))
			{
				string[] ss = RSA.Decrypt(licContent, keyName).Split(',');
				if (ss.Length > 4)
				{
					var stm = DateTime.FromBinary(Convert.ToInt64(ss[1], 16));
					sn = new SerialNumber()
					{
						LicenseType = (LicenseType)Convert.ToInt32(ss[0]),
						CreateTime = stm,
						ExpireTime = stm.AddDays(Convert.ToInt32(ss[2])),
						MachineId = ss[3],
						MaxActivity = Convert.ToInt32(ss[4])
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
			return true;

			///? 2015-4-29 发布免费版，不再做注册码验证
			//switch (LicenseType)
			//{
			//    case LicenseType.Free:
			//    case LicenseType.Trial:
			//        {
			//            // only check expire time.
			//            return (CreateTime < DateTime.Now && DateTime.Now < ExpireTime);
			//        }
			//    case LicenseType.Professional:
			//        {
			//            // check MachineId for local driver.
			//            string mid = new MachineInfo().GetMachineId(HardwareType.Driver);
			//            if (MachineId == mid)
			//                return (CreateTime < DateTime.Now && DateTime.Now < ExpireTime);
			//            else
			//                return false;
			//        }
			//    case LicenseType.Enterprise:
			//        {
			//            throw new NotImplementedException();
			//        }
			//    default:
			//        return false;
			//}
		}
	}

	[Serializable]
	public enum LicenseType
	{
		Free = 0,
		Trial = 1,
		Professional = 2,
		Enterprise = 3,
	}
}