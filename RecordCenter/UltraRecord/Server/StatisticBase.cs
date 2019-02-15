using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.Windows;

	partial class StatisticService
	{
		//private static Hashtable _users = Hashtable.Synchronized(new Hashtable());

		public object[] GetUsers()
		{
			var users = Database.Execute(db => db.SelectObjects<DomainUser>("SessionInfo", null, "DISTINCT UserName", "Domain"));
			List<object> objs = new List<object>();
			//_users.Clear();
			foreach (var user in users)
			{
				//_users[user] = null;
				objs.Add(new { Name = user.ToString() });
			}
			return objs.ToArray();
		}

		//private bool ValidUser(string user)
		//{
		//    return _users.ContainsKey(user);
		//}
	}
}
