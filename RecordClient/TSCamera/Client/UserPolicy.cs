using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace bfbd.TSCamera.Client
{
	using bfbd.Common.Windows;
	using bfbd.TSCamera.Windows;
	using bfbd.Common.Linq;

	class UserPolicy
	{
		public static bool IsUserRecording(string user)
		{
			bool record;

			if (HasUserPolicy() && HasGroupPolicy())
			{
				if (Global.Config.ExcludeUsers != null && InExcludeUsers(user))
					record = false;
				else if (Global.Config.IncludeUsers != null && InIncludeUsers(user))
					record = true;
				else
					record = InExcludeGroups(user) ? false : InIncludeGroups(user);
			}
			else if (HasUserPolicy() && !HasGroupPolicy())
			{
				record = InExcludeUsers(user) ? false : InIncludeUsers(user);
			}
			else if (!HasUserPolicy() && HasGroupPolicy())
			{
				record = InExcludeGroups(user) ? false : InIncludeGroups(user);
			}
			else
			{
				record = true;
			}
			return record;
		}

		private static bool HasUserPolicy()
		{
			return (Global.Config.ExcludeUsers != null || Global.Config.IncludeUsers != null); 
		}
		private static bool HasGroupPolicy()
		{
			return (Global.Config.ExcludeGroups != null || Global.Config.IncludeGroups != null);
		}

		private static bool InExcludeUsers(string user)
		{
			if (Global.Config.ExcludeUsers != null)
				return Array.Exists(Global.Config.ExcludeUsers, s => DomainUser.Equals(s, user));
			else
				return false;
		}
		private static bool InIncludeUsers(string user)
		{
			if (Global.Config.IncludeUsers != null)
				return Array.Exists(Global.Config.IncludeUsers, s => DomainUser.Equals(s, user));
			else
				return false;
		}

		private static bool InExcludeGroups(string user)
		{
			bool contains = false;
			if (Global.Config.ExcludeGroups != null)
			{
				var u = DomainUser.Parse(user);
				if (u.IsLocal)
				{
					string[] groups = UserAccess.GetLocalUserGroups(u.Name);
					contains = Global.Config.ExcludeGroups.Any(
						s => groups.Any(
							g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
				}
				else if (Global.Config.ADValid)
				{
					var exclude = Global.Config.ExcludeGroups
						.Select(s => DomainGroup.Parse(s))
						.Where(g => string.Equals(u.Domain, g.Domain, StringComparison.OrdinalIgnoreCase))
						.Select(g => g.Name);
					
					if (exclude.Any())
					{
						var ldap = new LdapAccess(Global.Config.ADPath, Global.Config.ADUser, Global.Config.ADPasswordValue, Global.Config.ADOrganization);
						var groups = ldap.GetUserGroups(u.Name);
						contains = exclude.Any(s => groups.Any(g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
					}
				}
			}
			return contains;
		}
		private static bool InIncludeGroups(string user)
		{
			bool contains = false;
			if (Global.Config.IncludeGroups != null)
			{
				var u = DomainUser.Parse(user);
				if (u.IsLocal)
				{
					string[] groups = UserAccess.GetLocalUserGroups(u.Name);
					contains = Global.Config.IncludeGroups.Any(
						s => groups.Any(
							g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
				}
				else if (Global.Config.ADValid)
				{
					var include = Global.Config.IncludeGroups
						.Select(s => DomainGroup.Parse(s))
						.Where(g => string.Equals(u.Domain, g.Domain, StringComparison.OrdinalIgnoreCase))
						.Select(g => g.Name);

					if (include.Any())
					{
						var ldap = new LdapAccess(Global.Config.ADPath, Global.Config.ADUser, Global.Config.ADPasswordValue, Global.Config.ADOrganization);
						var groups = ldap.GetUserGroups(u.Name);
						contains = include.Any(s => groups.Any(g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
					}
				}
			}
			return contains;
		}
	}
}
