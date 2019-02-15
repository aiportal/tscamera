using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Diagnostics;

namespace bfbd.Common.Windows
{
	class ADUserAccess
	{
		private DirectoryEntry _root = null;

		public string DomainName { get { return _root.Name.Substring(3); } }

		public ADUserAccess(string path, string user, string pwd, string organization = null)
		{
			try
			{
				Debug.Assert(!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pwd));
				path = path.StartsWith("LDAP://", StringComparison.OrdinalIgnoreCase) ? path : "LDAP://" + path;
				_root = new DirectoryEntry(path, user, pwd, AuthenticationTypes.Secure);
				if (!string.IsNullOrEmpty(organization))
					_root = _root.Children.Find("OU=" + organization);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
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

		public IEnumerable<ADItem> EnumADItems(ADSchema filter)
		{
			Queue<DirectoryEntry> queue = new Queue<DirectoryEntry>();
			queue.Enqueue(_root);
			while (queue.Count > 0)
			{
				var entry = queue.Dequeue();
				foreach (DirectoryEntry child in entry.Children)
				{
					if (child.SchemaClassName == "user")
					{
						if ((filter & ADSchema.User) == ADSchema.User) 
							yield return CreateADItem(child, ADSchema.User);
					}
					else if (child.SchemaClassName == "group")
					{
						if ((filter & ADSchema.Group) == ADSchema.Group)
							yield return CreateADItem(child, ADSchema.Group);
					}
					else if (child.SchemaClassName == "organizationalUnit")
					{
						queue.Enqueue(child);
						if ((filter & ADSchema.OrganizationalUnit) == ADSchema.OrganizationalUnit)
							yield return CreateADItem(child, ADSchema.OrganizationalUnit);
					}
				}					
			}
		}

		private ADItem CreateADItem(DirectoryEntry entry, ADSchema schema)
		{
			ADItem item = new ADItem()
			{
				Name = entry.Properties["SAMAccountName"].Value.ToString(),
				DisplayName = entry.Properties.Contains("displayName") ? entry.Properties["displayName"].Value.ToString() : null,
				Description = entry.Properties.Contains("description") ? entry.Properties["description"].Value.ToString() : null,

				Schema = schema,
				Id = entry.Guid.ToString(),
				ParentId = entry.Parent.Guid.ToString()
			};
			return item;
		}
	}

	class ADItem
	{
		public ADSchema Schema;
		public string Name;
		public string DisplayName;
		public string Description;

		public string Id;
		public string ParentId;
	}

	[Flags]
	enum ADSchema
	{
		User = 1,
		Group = 2,
		OrganizationalUnit = 4,
		All = User | Group | OrganizationalUnit
	}
}
