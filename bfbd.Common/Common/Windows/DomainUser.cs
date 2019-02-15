using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace bfbd.Common.Windows
{
	public class DomainUser
	{
		public string Domain;
		public string UserName;
		public string FullName;
		public string Description;

		public static DomainUser Current
		{
			get { return new DomainUser() { Domain = Environment.MachineName, UserName = Environment.UserName }; }
		}

		public static string CurrentDomain
		{
			get { return Environment.MachineName; }
		}

		public bool IsSystemUser
		{
			get { return string.Equals(Environment.MachineName, Domain, StringComparison.OrdinalIgnoreCase); }
		}

		public override bool Equals(object obj)
		{
			DomainUser user;
			if (obj is string)
				user = Create(obj as string);
			else
				user = obj as DomainUser;

			if (user != null)
			{
				return string.Equals(Domain, user.Domain, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(UserName, user.UserName, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				return false;
			}
		}

		//public override int GetHashCode()
		//{
		//    return base.GetHashCode();
		//}

		public override string ToString()
		{
			if (string.Equals(Domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
				return UserName;
			else
				return string.Format(@"{0}\{1}", Domain, UserName).ToLower();
		}

		public static bool Equals(string user1, string user2)
		{
			var user = DomainUser.Create(user1);
			return user == null ? false : user.Equals(user2);
		}

		public static DomainUser Create(string str)
		{
			DomainUser user = null;
			if (!string.IsNullOrEmpty(str))
			{
				var mc = Regex.Match(str, @"^(?<domain>.+)\\(?<user>.+)");
				if (mc.Success)
				{
					user = new DomainUser()
					{
						Domain = mc.Groups["domain"].Value,
						UserName = mc.Groups["user"].Value
					};
				}
				else
				{
					user = new DomainUser()
					{
						Domain = Environment.MachineName.ToLower(),
						UserName = str
					};
				}
			}
			return user;
		}
	}
}
