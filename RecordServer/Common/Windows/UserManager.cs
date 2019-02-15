using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace bfbd.Common.Windows
{
	static partial class UserManager
	{
		#region Enum

		public static IEnumerable<DomainUser> EnumLocalUsers(bool sid = false)
		{
			int read;
			int total;
			int resume;
			IntPtr buf;

			int ret = netapi.NetUserEnum(null, 10, util.FILTER_NORMAL_ACCOUNT, out buf, util.MAX_PREFERRED_LENGTH, out read, out total, out resume);
			if (ret != 0)
				throw new Win32Exception(ret);
			if (read > 0)
			{
				DomainUser user = new DomainUser();
				var u = new USER_INFO_10();
				IntPtr pItem = buf;
				for (int i = 0; i < read; ++i)
				{
					Marshal.PtrToStructure(pItem, u);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(USER_INFO_10)));

					if (sid)
						user.SID = GetAccountSID(u.name);
					user.Name = u.name;
					user.FullName = u.full_name;
					user.Comment = u.comment;
					yield return user;
				}
				netapi.NetApiBufferFree(buf);
			}
		}

		public static IEnumerable<DomainGroup> EnumLocalGroups(bool sid = false)
		{
			int read;
			int total;
			int resume;
			IntPtr buf;

			int ret = netapi.NetLocalGroupEnum(null, 1, out buf, util.MAX_PREFERRED_LENGTH, out read, out total, out resume);
			if (ret != 0)
				throw new Win32Exception(ret);
			if (read > 0)
			{
				DomainGroup group = new DomainGroup();
				var g = new LOCALGROUP_INFO_1();
				IntPtr pItem = buf;
				for (int i = 0; i < read; i++)
				{
					Marshal.PtrToStructure(pItem, g);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_INFO_1)));

					if (sid)
						group.SID = GetAccountSID(g.name);
					group.Name = g.name;
					group.Comment = g.comment;
					yield return group;
				}
				netapi.NetApiBufferFree(buf);
			}
		}

		public static string GetAccountSID(string account)
		{
			var a = new System.Security.Principal.NTAccount(null, account);
			return a.Translate(typeof(System.Security.Principal.SecurityIdentifier)).Value;
		}

		public static string AccountFromSID(string sid)
		{
			var identifier = new System.Security.Principal.SecurityIdentifier(sid);
			var account = identifier.Translate(typeof(System.Security.Principal.NTAccount));
			return DomainAccount.Parse(account.Value).ToString();
		}

		public static bool IsBuildInAccount(string account)
		{
			var sid = GetAccountSID(account);
			return int.Parse(Regex.Match(sid, @"\d+$").Value) < 1000;
		}

		#endregion Enum

		#region User

		public static void AddLocalUser(WindowsUser user, params string[] groups)
		{
			Debug.Assert(user.IsLocal && !string.IsNullOrEmpty(user.Name));
			Debug.Assert(!string.IsNullOrEmpty(user.Password));

			int flag = user.PwdExpired ? 0 : util.UF_DONT_EXPIRE_PASSWD;
			USER_INFO_1 u = new USER_INFO_1()
			{
				name = user.Name,
				password = user.Password,
				priv = 1,
				home_dir = null,
				comment = user.Comment,
				flags = flag,
			};
			int ret = netapi.NetUserAdd(null, 1, u, 0);
			if (ret != 0)
				throw new Win32Exception(ret);

			UpdateLocalUser(user.Name, user, groups);
		}

		public static void UpdateLocalUser(string username, WindowsUser user, params string[] groups)
		{
			Debug.Assert(user.IsLocal && !string.IsNullOrEmpty(user.Name));
			int ret = 0;

			// rename
			if (!string.Equals(username, user.Name, StringComparison.OrdinalIgnoreCase))
			{
				ret = netapi.NetUserSetInfo(null, username, 0, ref user.Name, 0);
				if (ret != 0)
					throw new Win32Exception(ret);
			}

			// attributes
			USER_INFO_4 u = new USER_INFO_4();
			{
				IntPtr pu = new IntPtr();
				ret = netapi.NetUserGetInfo(null, user.Name, 4, out pu);
				if (ret != 0)
					throw new Win32Exception(ret);
				Marshal.PtrToStructure(pu, u);
				netapi.NetApiBufferFree(pu);
			}
			if (!string.IsNullOrEmpty(user.Password))
			{
				u.password = user.Password;
				u.password_expired = user.PwdExpired;
			}
			u.full_name = user.FullName;
			u.comment = user.Comment;
			u.acct_expires = user.ExpireTime.HasValue ? (int)user.ExpireTime.Value.Subtract(util.TIME_MIN).TotalSeconds : util.TIMEQ_FOREVER;
			ret = netapi.NetUserSetInfo(null, user.Name, 4, u, 0);
			if (ret != 0)
				throw new Win32Exception(ret);

			// groups
			if (groups != null)
				Array.ForEach<string>(groups, g => netapi.NetLocalGroupAddMembers(null, g, 3, ref user.Name, 1));
		}

		public static void DeleteLocalUser(string username)
		{
			int ret = netapi.NetUserDel(null, username);
			if (ret != 0)
				throw new Win32Exception(ret);
		}

		public static string[] GetLocalUserGroups(string username)
		{
			int read;
			int total;
			IntPtr pbuf;

			int ret = netapi.NetUserGetLocalGroups(null, username, 0, 0, out pbuf, -1, out read, out total);
			if (ret != 0)
				throw new Win32Exception(ret);

			List<string> groups = new List<string>();
			if (read > 0)
			{
				var g = new LOCALGROUP_USERS_INFO_0();
				IntPtr pItem = pbuf;
				for (int i = 0; i < read; ++i)
				{
					Marshal.PtrToStructure(pItem, g);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_USERS_INFO_0)));
					groups.Add(g.name);
				}
			}
			netapi.NetApiBufferFree(pbuf);
			return groups.ToArray();
		}

		#endregion User

		#region Group

		public static void AddLocalGroup(DomainGroup group, string[] members = null)
		{
			int ret = 0;
			var g = new LOCALGROUP_INFO_1() { name = group.Name, comment = group.Comment };
			ret = netapi.NetLocalGroupAdd(null, 1, g, 0);
			if (ret != 0)
				throw new Win32Exception(ret);

			if (members != null)
			{
				ret = netapi.NetLocalGroupSetMembers(null, group.Name, 3, members, members.Length);
				if (ret != 0)
					throw new Win32Exception(ret);
			}
		}

		public static void UpdateLocalGroup(string groupname, DomainGroup group, string[] members = null)
		{
			int ret = 0;

			// rename
			if (!string.Equals(groupname, group.Name, StringComparison.OrdinalIgnoreCase))
			{
				ret = netapi.NetLocalGroupSetInfo(null, groupname, 0, ref group.Name, 0);
				if (ret != 0)
					throw new Win32Exception(ret);
			}

			// attributes
			var g = new LOCALGROUP_INFO_1() { name = group.Name, comment = group.Comment };
			ret = netapi.NetLocalGroupSetInfo(null, g.name, 1, g, 0);
			if (ret != 0)
				throw new Win32Exception(ret);

			// members
			if (members != null)
			{
				ret = netapi.NetLocalGroupSetMembers(null, group.Name, 3, members, members.Length);
				if (ret != 0)
					throw new Win32Exception(ret);
			}
			else
			{
				ret = netapi.NetLocalGroupSetMembers(null, group.Name, 3, new string[0], 0);
			}
		}

		public static void DeleteLocalGroup(string groupname)
		{
			int ret = netapi.NetLocalGroupDel(null, groupname);
			if (ret != 0)
				throw new Win32Exception(ret);
		}

		public static string[] GetLocalGroupMembers(string groupname)
		{
			int read;
			int total;
			int resume;
			IntPtr pbuf;

			int ret = netapi.NetLocalGroupGetMembers(null, groupname, 3, out pbuf, -1, out read, out total, out resume);
			if (ret != 0)
				throw new Win32Exception(ret);

			List<string> members = new List<string>();
			if (read > 0)
			{
				var m = new LOCALGROUP_MEMBERS_INFO_3();
				IntPtr pItem = pbuf;
				for (int i = 0; i < read; ++i)
				{
					Marshal.PtrToStructure(pItem, m);
					pItem = new IntPtr(pItem.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_MEMBERS_INFO_3)));
					members.Add(DomainUser.Parse(m.domainandname).ToString());
				}
			}
			netapi.NetApiBufferFree(pbuf);
			return members.ToArray();
		}

		public static void SetLocalGroupMembers(string groupname, string[] members)
		{
			members = members ?? new string[0];
			if (members != null)
			{
				int ret = netapi.NetLocalGroupSetMembers(null, groupname, 3, members, members.Length);
				if (ret != 0)
					throw new Win32Exception(ret);
			}
		}

		#endregion Group

		#region Others

		public static WindowsUser TryGetLocalUser(string username)
		{
			USER_INFO_4 u = new USER_INFO_4();
			{
				IntPtr pu = new IntPtr();
				int ret = netapi.NetUserGetInfo(null, username, 4, out pu);
				if (ret != 0)
					return null;
				Marshal.PtrToStructure(pu, u);
				netapi.NetApiBufferFree(pu);
			}
			return new WindowsUser()
			{
				Name = u.name,
				FullName = u.full_name,
				Comment = u.comment,
				PwdExpired = u.password_expired,
				ExpireTime = (u.acct_expires == util.TIMEQ_FOREVER) ? (DateTime?)null : util.TIME_MIN.AddSeconds(u.acct_expires)
			};
		}

		public static WindowsGroup TryGetLocalGroup(string groupname)
		{
			LOCALGROUP_INFO_1 g = new LOCALGROUP_INFO_1();
			{
				IntPtr buf = new IntPtr();
				int ret = netapi.NetLocalGroupGetInfo(null, groupname, 1, out buf);
				if (ret != 0)
					return null;
				Marshal.PtrToStructure(buf, g);
				netapi.NetApiBufferFree(buf);
			}
			return new WindowsGroup()
			{
				Name = g.name,
				Comment = g.comment,
				Members = GetLocalGroupMembers(groupname)
			};
		}

		public static void ChangeUserPassword(string username, string password)
		{
			if (!string.Equals(username, username, StringComparison.OrdinalIgnoreCase))
			{
				int ret = netapi.NetUserSetInfo(null, username, 1003, ref password, 0);
				if (ret != 0)
					throw new Win32Exception(ret);
			}
		}

		public static void SetUserLogonHours(string username, byte[] bsHours)
		{
			Debug.Assert(bsHours.Length == 21);

			int ret = 0;
			USER_INFO_4 u = new USER_INFO_4();
			{
				IntPtr pu = new IntPtr();
				ret = netapi.NetUserGetInfo(null, username, 4, out pu);
				if (ret != 0)
					throw new Win32Exception(ret);
				Marshal.PtrToStructure(pu, u);
				netapi.NetApiBufferFree(pu);
			}
			Marshal.Copy(bsHours, 0, u.logon_hours, 21);
			ret = netapi.NetUserSetInfo(null, username, 4, u, 0);
			if (ret != 0)
				throw new Win32Exception(ret);
		}

		#endregion Others
	}

	static partial class UserManager
	{
		public static readonly string USER_Administrator = "Administrator";
		public static readonly string GROUP_RDP = "Remote Desktop Users";
		public static readonly string GROUP_Administrators = "Administrators";
	}

	partial class UserManager
	{
		class netapi
		{
			#region Enum

			[DllImport("Netapi32.dll", EntryPoint = "NetUserEnum", CharSet = CharSet.Unicode)]
			public extern static int NetUserEnum(
				string servername,
				int level,
				int filter,
				out IntPtr bufptr,
				int prefmaxlen,
				out int entriesread,
				out int totalentries,
				out int resume_handle);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupEnum", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupEnum(
				string sName,
				int Level,
				out IntPtr bufPtr,
				int prefmaxlen,
				out int entriesread,
				out int totalentries,
				out int resume_handle);

			[DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
			public extern static int NetApiBufferFree(IntPtr Buffer);

			#endregion Enum

			#region User

			[DllImport("Netapi32.dll", EntryPoint = "NetUserAdd", CharSet = CharSet.Unicode)]
			public static extern int NetUserAdd(string servername, int level, USER_INFO_1 buf, int parm_err);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserDel", CharSet = CharSet.Unicode)]
			public static extern int NetUserDel(string servername, string username);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserGetInfo", CharSet = CharSet.Unicode)]
			public static extern int NetUserGetInfo(string servername, string username, int level, out IntPtr bufptr);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserSetInfo", CharSet = CharSet.Unicode)]
			public static extern int NetUserSetInfo(string servername, string username, int level, ref string value, int error);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserSetInfo", CharSet = CharSet.Unicode)]
			public static extern int NetUserSetInfo(string servername, string username, int level, ref int value, int error);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserSetInfo", CharSet = CharSet.Unicode)]
			public static extern int NetUserSetInfo(string servername, string username, int level, USER_INFO_4 user_info_4, int error);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserGetLocalGroups", CharSet = CharSet.Unicode)]
			public static extern int NetUserGetLocalGroups(string servername, string username, int level, int flags, 
				out IntPtr bufptr,  int prefmaxlen, out int entriesread, out int totalentries);
			
			#endregion User

			#region Group

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupAdd", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupAdd(string servername, int level, LOCALGROUP_INFO_1 buf, int parm_err);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupGetInfo", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupGetInfo(string servername, string groupname, int Level, out IntPtr buf);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupSetInfo", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupSetInfo(string servername, string groupname, int level, ref string value, int parm_err);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupSetInfo", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupSetInfo(string servername, string groupname, int level, LOCALGROUP_INFO_1 buf, int parm_err);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupDel", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupDel(string servername, string groupname);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupAddMembers", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupAddMembers(string servername, string groupname, int level, ref string domainandname, int totalentries);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupGetMembers", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupGetMembers(string servername, string groupname, int level, out IntPtr bufptr, int prefmaxlen,
				out int entriesread, out int totalentries, out int resumehandle);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupSetMembers", CharSet = CharSet.Unicode)]
			public extern static int NetLocalGroupSetMembers(string servername, string groupname, int level, string[] users, int totalentries);

			#endregion Group
		}

		#region Struct

		#region User

		// NetUserEnum
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class USER_INFO_10
		{
			public string name;
			public string comment;
			public string usr_comment;
			public string full_name;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class USER_INFO_3
		{
			public string name;
			public string password;
			public int password_age;
			public int priv;
			public string home_dir;
			public string comment;
			public int flags;
			public string script_path;
			public int auth_flags;
			public string full_name;
			public string usr_comment;
			public string parms;
			public string workstations;
			public int last_logon;
			public int last_logoff;
			public int acct_expires;
			public int max_storage;
			public int units_per_week;
			public IntPtr logon_hours;
			public int bad_pw_count;
			public int num_logons;
			public string logon_server;
			public int country_code;
			public int code_page;
			public int user_sid;
			public int primary_group_id;
			public string profile;
			public string home_dir_drive;
			public bool password_expired;
		}

		// NetUserAdd
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class USER_INFO_1
		{
			public string name;
			public string password;
			public int password_age;
			public int priv;
			public string home_dir;
			public string comment;
			public int flags;
			public string script_path;
		}

		// NetUserSetInfo
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class USER_INFO_4
		{
			public string name;
			public string password;
			public int password_age;
			public int priv;
			public string home_dir;
			public string comment;
			public int flags;
			public string script_path;
			public int auth_flags;
			public string full_name;
			public string usr_comment;
			public string parms;
			public string workstations;
			public int last_logon;
			public int last_logoff;
			public int acct_expires;
			public int max_storage;
			public int units_per_week;
			public IntPtr logon_hours;
			public int bad_pw_count;
			public int num_logons;
			public string logon_server;
			public int country_code;
			public int code_page;
			public IntPtr user_sid;
			public int primary_group_id;
			public string profile;
			public string home_dir_drive;
			public bool password_expired;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class LOCALGROUP_USERS_INFO_0
		{
			public string name;
		}

		#endregion User

		#region Group

		// Enum
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class LOCALGROUP_INFO_1
		{
			public string name;
			public string comment;
		}

		// NetLocalGroupGetMembers
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class LOCALGROUP_MEMBERS_INFO_3
		{
			public string domainandname;
		}

		#endregion Group

		#endregion Struct

		#region Util

		class util
		{
			// NetUserEnum,NetLocalGroupEnum
			public const int FILTER_NORMAL_ACCOUNT = 2;
			public const int MAX_PREFERRED_LENGTH = -1;

			// NetUserAdd
			public const int USER_PRIV_USER = 1;

			#region UserFlags
			
			public const int UF_SCRIPT = 0x000001;
			public const int UF_ACCOUNTDISABLE = 0x000002;
			public const int UF_HOMEDIR_REQUIRED = 0x000008;
			public const int UF_LOCKOUT = 0x000010;
			public const int UF_PASSWD_NOTREQD = 0x000020;
			public const int UF_PASSWD_CANT_CHANGE = 0x000040;
			public const int UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED = 0x000080;
			public const int UF_TEMP_DUPLICATE_ACCOUNT = 0x000100;
			public const int UF_NORMAL_ACCOUNT = 0x000200;
			public const int UF_INTERDOMAIN_TRUST_ACCOUNT = 0x000800;
			public const int UF_WORKSTATION_TRUST_ACCOUNT = 0x001000;
			public const int UF_SERVER_TRUST_ACCOUNT = 0x002000;
			public const int UF_DONT_EXPIRE_PASSWD = 0x010000;
			public const int UF_MNS_LOGON_ACCOUNT = 0x020000;
			public const int UF_SMARTCARD_REQUIRED = 0x040000;
			public const int UF_TRUSTED_FOR_DELEGATION = 0x080000;
			public const int UF_NOT_DELEGATED = 0x100000;
			public const int UF_USE_DES_KEY_ONLY = 0x200000;
			public const int UF_DONT_REQUIRE_PREAUTH = 0x400000;
			public const int UF_PASSWORD_EXPIRED = 0x800000;
			
			#endregion UserFlags

			// NetUserSetInfo
			public const int TIMEQ_FOREVER = -1;
			public static readonly DateTime TIME_MIN = new DateTime(1970, 1, 1);
		}

		#endregion Util
	}
}
