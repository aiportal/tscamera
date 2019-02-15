using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Windows
{
	[Serializable]
	public class DomainUser
	{
		public string UserName;
		public string FullName;
		public string Description;
		public string Domain;

		public static bool TryParse(string str, out DomainUser user)
		{
			if (string.IsNullOrEmpty(str))
			{
				user = null;
				return false;
			}
			else
			{
				if (str.Contains(@"\"))
				{
					user = new DomainUser()
					{
						Domain = str.Split('\\')[0],
						UserName = str.Split('\\')[1]
					};
					return (!string.IsNullOrEmpty(user.Domain) && !string.IsNullOrEmpty(user.UserName));
				}
				else
				{
					user = new DomainUser()
					{
						Domain = Environment.MachineName.ToLower(),
						UserName = str
					};
					return true;
				}
			}
		}

		public static bool Equals(string user1, string user2)
		{
			DomainUser u1, u2;
			if (TryParse(user1, out u1) && TryParse(user2, out u2))
				return Equals(u1, u2);
			return false;
		}
		
		public static bool Equals(DomainUser user1, DomainUser user2)
		{
			return string.Equals(user1.Domain, user2.Domain, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(user1.UserName, user2.UserName, StringComparison.OrdinalIgnoreCase);
		}
		
		public override bool Equals(object obj)
		{
			if (obj is string)
				return Equals(this.ToString(), obj as string);
			else if (obj is DomainUser)
				return Equals(this, obj as DomainUser);
			else
				return false;
		}
		
		public override string ToString()
		{
			if (string.Equals(Domain, Environment.MachineName, StringComparison.InvariantCultureIgnoreCase))
				return UserName;
			else
				return string.Format(@"{0}\{1}", Domain, UserName).ToLower();
		}
	}
}
