using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace bfbd.Common.License
{
	sealed class LicenseInfo
	{
		public string SerialKey { get; set; }
		public string Version { get; set; }
		public string MachineId { get; set; }

		public string Email { get; set; }
		public string CompanyName { get; set; }
		public string ContactName { get; set; }
		public string Phone { get; set; }
		public string Address { get; set; }
	
		public DateTime CreateTime { get; set; }
		public DateTime ExpireTime { get; set; }
		public string LicenseKey { get; set; }

		internal bool IsVerified { get; set; }
	}
}
