using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera
{
	using bfbd.Common;

	static class Global
	{
		public static LocalConfig Config = new LocalConfig();
	}

	public partial class LocalConfig : bfbd.Common.ConfigurationBase
	{
		internal int AdminServerPort = 1081;

		internal int AdminWebPort = 1080;
		internal string AdminAccount = "admin";
		internal string AdminPassword = Encryption.Encrypt(@"admin");

		internal string LicenseKey = null;
		internal DateTime InstallTime = new DateTime(3000, 1, 1);
	}

	partial class LocalConfig
	{
		// Agent policy
		public bool RecordEnabled = true;
		public bool IconVisible = true;
		//internal readonly bool RecordImage = true;
		//internal readonly bool AgentGrayScale = false;
		public bool GrayScale = true;

		internal string RecordPolicy = null;
		internal bool DebugDumpOriginal = false;
		internal bool DebugDumpText = false;

		public string[] IncludeUsers;
		public string[] ExcludeUsers;
		public string[] IncludeGroups;
		public string[] ExcludeGroups;
		public string[] IncludeApps;
		public string[] ExcludeApps;
		//public string[] IncludeHosts;
		//public string[] ExcludeHosts;

		// License info

		// Storage policy
		public int DiskQuota { get; set; }	// GB
		public int DaysLimit { get; set; }	// Days

		// Admin config items.
		public bool ForbidUsers = true;
		public string PermitAddress
		{
			get { return PermitAddressValue == null ? null : PermitAddressValue.ToString(); }
			set
			{
				if (string.IsNullOrEmpty(value))
					PermitAddressValue = null;
				else
				{
					System.Net.IPAddress addr;
					if (System.Net.IPAddress.TryParse(value, out addr))
						PermitAddressValue = addr;
				}
			}
		}
		internal System.Net.IPAddress PermitAddressValue = null;
		internal string PermitUser = null;

		#region LDAP

		public string ADPath { get; set; }
		public string ADUser { get; set; }
		public string ADPassword
		{
			internal get { return Encryption.Encrypt(ADPasswordValue); }
			set { try { ADPasswordValue = Encryption.Decrypt(value); } catch (Exception) { ADPasswordValue = value; } }
		}
		public string ADOrganization { get; set; }
		internal string ADPasswordValue;
		internal bool ADValid { get { return !string.IsNullOrEmpty(ADPath) && !string.IsNullOrEmpty(ADUser) && !string.IsNullOrEmpty(ADPasswordValue); } }

		#endregion LDAP
	}
}
