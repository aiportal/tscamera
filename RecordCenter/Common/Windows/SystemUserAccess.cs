using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.DirectoryServices;
using System.Management;
using System.Runtime.InteropServices;

namespace bfbd.Common.Windows
{
	partial class SystemUserAccess
	{
		public DomainUser[] GetUsers()
		{
			List<DomainUser> users = new List<DomainUser>();
			using (DirectoryEntry entry = new DirectoryEntry("WinNT://" + Environment.MachineName))
			{
				foreach (DirectoryEntry child in entry.Children)
				{
					if (child.SchemaClassName == "User")
					{
						int flag = Convert.ToInt32(child.Properties["UserFlags"].Value);
						if ((flag & util.UF_ACCOUNTDISABLE) == 0)
						{
							users.Add(new DomainUser()
							{
								UserName = child.Name,
								FullName = Convert.ToString(child.Properties["FullName"].Value),
								Description = Convert.ToString(child.Properties["Description"].Value.ToString()),
								Domain = Environment.MachineName
							});
						}
						//foreach (var pn in child.Properties.PropertyNames)
						//{
						//    System.Diagnostics.Debug.WriteLine(pn.ToString());
						//}
					}
				}
			}
			return users.ToArray();
		}

		public LocalGroup[] GetGroups()
		{
			List<LocalGroup> groups = new List<LocalGroup>();
			using (DirectoryEntry entry = new DirectoryEntry("WinNT://" + Environment.MachineName))
			{
				foreach (DirectoryEntry child in entry.Children)
				{
					if (child.SchemaClassName == "Group")
					{
						groups.Add(new LocalGroup()
						{
							Name = child.Name,
							Description = Convert.ToString(child.Properties["Description"].Value)
						});
					}
				}
			}
			return groups.ToArray();
		}

		public string[] GetUserGroups(string username)
		{
			List<string> groups = new List<string>();
			try
			{
				int entriesread;
				int totalentries;
				IntPtr pBuf;
				netapi32.NetUserGetLocalGroups(null, username, 0, 1,out pBuf, -1, out entriesread, out totalentries);
				if (entriesread > 0)
				{
					IntPtr pItem = pBuf;
					for (int i = 0; i < entriesread; ++i)
					{
						var groupinfo = (LOCALGROUP_USERS_INFO_0)Marshal.PtrToStructure(pItem, typeof(LOCALGROUP_USERS_INFO_0));
						pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_USERS_INFO_0)));
						groups.Add(groupinfo.lgrui0_name);
					}
				}
				netapi32.NetApiBufferFree(pBuf);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return groups.ToArray();
		}

		[Obsolete]
		public string[] GetUserGroupsByWMI(string userName)
		{
			List<string> groups = new List<string>();
			try
			{
				Regex regGroupName = new Regex(",Name=\"(?<group>.+)\"");
				string wql = string.Format("SELECT * FROM Win32_GroupUser WHERE PartComponent=\"Win32_UserAccount.Domain='{0}',Name='{1}'\"", Environment.UserDomainName, userName);
				using (var searcher = new ManagementObjectSearcher("root\\CIMV2", wql))
				{
					foreach (var mo in searcher.Get())
					{
						string val = Convert.ToString(mo["GroupComponent"]);
						var mc = regGroupName.Match(val);
						if (mc.Success)
							groups.Add(mc.Groups["group"].Value);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return groups.ToArray();
		}
	}

	[Serializable]
	public class LocalGroup
	{
		public string Name;
		public string Description;
	}

	partial class netapi32
	{
		//[DllImport("Netapi32.dll", EntryPoint = "NetUserGetGroups", CharSet = CharSet.Unicode)]
		//public extern static int NetUserGetGroups(string servername, string username, int level, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries);

		[DllImport("Netapi32.dll", EntryPoint = "NetUserGetLocalGroups", CharSet = CharSet.Unicode)]
		public extern static int NetUserGetLocalGroups(string servername, string username, int level, int flags, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries);

		[DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
		public extern static int NetApiBufferFree(IntPtr Buffer);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LOCALGROUP_USERS_INFO_0
	{
		[MarshalAs(UnmanagedType.LPWStr)]
		public string lgrui0_name;
	}

	//public struct GROUP_USERS_INFO_0
	//{
	//    public string grui0_name;
	//}

	partial class util
	{
		// UserFlags
		public const int UF_SCRIPT = 0x0001;
		public const int UF_ACCOUNTDISABLE = 0x0002;
		public const int UF_HOMEDIR_REQUIRED = 0x0008;
		public const int UF_LOCKOUT = 0x0010;
		public const int UF_PASSWD_NOTREQD = 0x0020;
		public const int UF_PASSWD_CANT_CHANGE = 0x0040;
		public const int UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED = 0x0080;
		public const int UF_DONT_EXPIRE_PASSWD = 0x10000;
		public const int UF_MNS_LOGON_ACCOUNT = 0x20000;
		public const int UF_SMARTCARD_REQUIRED = 0x40000;
		public const int UF_TRUSTED_FOR_DELEGATION = 0x80000;
		public const int UF_NOT_DELEGATED = 0x100000;
		public const int UF_USE_DES_KEY_ONLY = 0x200000;
		public const int UF_DONT_REQUIRE_PREAUTH = 0x400000;

		public const int UF_TEMP_DUPLICATE_ACCOUNT = 0x0100; //local account
		public const int UF_NORMAL_ACCOUNT = 0x0200; //global account
		public const int UF_INTERDOMAIN_TRUST_ACCOUNT = 0x0800; //incoming trust
		public const int UF_WORKSTATION_TRUST_ACCOUNT = 0x1000; //ws or ms comp
		public const int UF_SERVER_TRUST_ACCOUNT = 0x2000; //dc computer
	}
}
