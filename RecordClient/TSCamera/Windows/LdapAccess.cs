using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;

namespace bfbd.TSCamera.Windows
{
	using bfbd.Common;

	class LdapAccess
	{
		private DirectoryEntry _root = null;
		
		public LdapAccess(string path, string user, string pwd, string organization = null)
		{
			try
			{
				this.Assert(!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pwd));

				path = path.StartsWith("LDAP://", StringComparison.OrdinalIgnoreCase) ? path : "LDAP://" + path;
				_root = new DirectoryEntry(path, user, pwd, AuthenticationTypes.Secure);
				if (!string.IsNullOrEmpty(organization))
					_root = _root.Children.Find("OU=" + organization);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public string[] GetUserGroups(string username)
		{
			List<string> groups = new List<string>();
			{
				DirectorySearcher directorySearch = new DirectorySearcher(_root);
				directorySearch.Filter = "(&(objectClass=user)(SAMAccountName=" + username + "))";
				SearchResult entry = directorySearch.FindOne();
				if (entry != null)
				{
					foreach (var g in entry.Properties["memberof"])
						groups.Add(g.ToString().Substring(3, g.ToString().IndexOf(',') - 3));
				}
			}
			return groups.ToArray();
		}
	}
}
