using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace bfbd.Common.Windows
{
	partial class DomainAccount
	{
		public string SID { get; set; }
		public string Name { get; set; }
		public string Domain { get; set; }
	}

	class DomainGroup : DomainAccount
	{
		public string GroupName { get { return Name; } set { Name = value; } }
		public string Comment { get; set; }
	}

	class DomainUser : DomainAccount
	{
		public string UserName { get { return Name; } set { Name = value; } }
		public string FullName { get; set; }
		public string Comment { get; set; }
	}

	class WindowsUser : DomainUser
	{
		public string Password { get; set; }
		public bool PwdExpired { get; set; }
		public DateTime? ExpireTime { get; set; }
	}

	class WindowsGroup : DomainGroup
	{
		public string[] Members { get; set; }
	}

	partial class DomainAccount : IEquatable<DomainAccount>
	{
		#region Account Functions

		public bool IsLocal
		{
			get { return IsLocalDomain(Domain); }
		}

		public bool Equals(DomainAccount a)
		{
			bool equal = false;
			if (a != null)
			{
				if ((a.IsLocal && this.IsLocal) || string.Equals(Domain, a.Domain, StringComparison.OrdinalIgnoreCase))
					equal = string.Equals(Name, a.Name, StringComparison.OrdinalIgnoreCase);
			}
			return equal;
		}

		public override string ToString()
		{
			return ToString(Domain, Name, false);
		}

		public string ToString(bool whole)
		{
			return ToString(Domain, Name, whole);
		}

		#endregion Account Functions

		#region Static Functions

		public static bool IsLocalDomain(string domain)
		{
			return string.IsNullOrEmpty(domain) || string.Equals(Environment.MachineName, domain, StringComparison.OrdinalIgnoreCase);
		}

		public static string ToString(string domain, string user, bool whole = false)
		{
			domain = IsLocalDomain(domain) ? Environment.MachineName : domain;
			return whole ? string.Format(@"{0}\{1}", domain, user).ToLower() : user;
		}

		public static DomainAccount Parse(string str) { return Parse<DomainAccount>(str); }
		
		public static T Parse<T>(string str)
			where T : DomainAccount, new()
		{
			T account = null;
			if (!string.IsNullOrEmpty(str))
			{
				var mc = Regex.Match(str, @"^(?<domain>.+)\\(?<name>.+)");
				if (mc.Success)
				{
					account = new T()
					{
						Domain = mc.Groups["domain"].Value,
						Name = mc.Groups["name"].Value
					};
				}
				else
				{
					account = new T()
					{
						Name = str
					};
				}
			}
			return account;
		}

		#endregion Static Functions
	}
}
