using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace bfbd.TSCamera.Windows
{
	using bfbd.Common.Windows;

	static partial class UserAccess
	{
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
	}

	partial class UserAccess
	{
		#region Windows API

		class netapi
		{
			[DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
			internal extern static int NetApiBufferFree(IntPtr Buffer);

			[DllImport("Netapi32.dll", EntryPoint = "NetUserGetLocalGroups", CharSet = CharSet.Unicode)]
			internal static extern int NetUserGetLocalGroups(string servername, string username, int level, int flags,
				out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries);

			[DllImport("Netapi32.dll", EntryPoint = "NetLocalGroupGetMembers", CharSet = CharSet.Unicode)]
			internal extern static int NetLocalGroupGetMembers(string servername, string groupname, int level, out IntPtr bufptr, int prefmaxlen,
				out int entriesread, out int totalentries, out int resumehandle);
		}

		#endregion Windows API

		#region Structs

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class LOCALGROUP_USERS_INFO_0
		{
			public string name;
		}

		// NetLocalGroupGetMembers
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class LOCALGROUP_MEMBERS_INFO_3
		{
			public string domainandname;
		}

		#endregion Structs
	}
}
