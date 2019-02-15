using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common.Windows;

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
				{
					record = InExcludeGroups(user) ? false : InIncludeGroups(user);
				}
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
				var u = DomainUser.Create(user);
				if (u.IsSystemUser)
				{
					string[] groups = new SystemUserAccess().GetUserGroups(u.UserName);
					contains = Array.Exists(Global.Config.ExcludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
				}
				else if (Global.Config.ADValid && ContainsADGroup(Global.Config.ExcludeGroups))
				{
					var cfg = Global.Config;
					var ada = new ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
					if (string.Equals(u.Domain, ada.DomainName, StringComparison.OrdinalIgnoreCase))
					{
						var groups = ada.GetUserGroups(u.UserName);
						contains = Array.Exists(Global.Config.ExcludeGroups, s => Array.Exists(groups, g => 
							string.Equals(s, ada.DomainName + "\\" + g, StringComparison.OrdinalIgnoreCase)));
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
				var u = DomainUser.Create(user);
				if (u.IsSystemUser)
				{
					string[] groups = new SystemUserAccess().GetUserGroups(u.UserName);
					return Array.Exists(Global.Config.IncludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
				}
				else if (Global.Config.ADValid && ContainsADGroup(Global.Config.IncludeGroups))
				{
					var cfg = Global.Config;
					var ada = new ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
					if (string.Equals(u.Domain, ada.DomainName, StringComparison.OrdinalIgnoreCase))
					{
						var groups = ada.GetUserGroups(u.UserName);
						contains = Array.Exists(Global.Config.IncludeGroups, s => Array.Exists(groups, g => 
							string.Equals(s, ada.DomainName + "\\" + g, StringComparison.OrdinalIgnoreCase)));
					}
				}
			}
			return contains;
		}

		/// <summary>
		/// 是否基于LDAP过滤
		/// </summary>
		private static bool ContainsADGroup(string[] groups)
		{
			bool contains = false;
			Debug.Assert(groups != null);
			foreach (var g in groups)
			{
				if (g.Contains(@"\"))
				{
					var domain = g.Substring(0, g.IndexOf('\\'));
					if (!string.Equals(domain, DomainUser.CurrentDomain, StringComparison.OrdinalIgnoreCase))
					{
						contains = true;
						break;
					}
				}
			}
			return contains;
		}
	}
}
