using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.Windows;
	using bfbd.UltraRecord.Core;

	public partial class StatisticService
	{
		public object[] ComputerUsage(DateTime start, DateTime end, string user)
		{
			Debug.Assert(!string.IsNullOrEmpty(user));
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=SnapDate AND SnapDate<='{1:yyyy-MM-dd}' ", start, end);
			filter.AppendFormat(" AND Domain='{0}'", user.Contains(@"\") ? user.Split('\\')[0] : Environment.MachineName);
			filter.AppendFormat(" AND UserName='{0}'", user.Contains(@"\") ? user.Split('\\')[1] : user);

			var groups = Database.Execute(db=>db.ReadObjects<SnapGroup>("HoursView", filter.ToString(), "SnapHour", 4,
				"Domain", "UserName", "SnapDate AS SessionDate", "SnapHour", "Sum(SnapCount) AS SnapCount"));

			List<object> array = new List<object>();
			foreach (var g in groups)
				array.Add(new
				{
					User = g.User,
					Date = g.SessionDate,
					Hour = g.SnapHour,
					Count = g.SnapCount,
				});
			return array.ToArray();
		}

		public object[] ProgramUsage(DateTime start, DateTime end, string user)
		{
			Debug.Assert(!string.IsNullOrEmpty(user));
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=SessionDate AND SessionDate<='{1:yyyy-MM-dd}' ", start, end);
			filter.AppendFormat(" AND Domain='{0}'", user.Contains(@"\") ? user.Split('\\')[0] : Environment.MachineName);
			filter.AppendFormat(" AND UserName='{0}'", user.Contains(@"\") ? user.Split('\\')[1] : user);

			var groups = Database.Execute(db => db.ReadObjects<SnapGroup>("StatisticView", filter.ToString(), null, 4,
					"Domain", "UserName", "SessionDate", "ProcessName", "Sum(SnapCount) AS SnapCount"));

			List<object> array = new List<object>();
			foreach (var g in groups)
			{
				if (string.IsNullOrEmpty(g.ProcessName))
					continue;
				array.Add(new
				{
					User = g.User,
					Date = g.SessionDate,
					Prog = g.ProcessName,
					Count = g.SnapCount,
				});
			}
			return array.ToArray();
		}

		public object[] HostVisit(DateTime start, DateTime end, string user)
		{
			Debug.Assert(!string.IsNullOrEmpty(user));
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=SessionDate AND SessionDate<='{1:yyyy-MM-dd}' ", start, end);
			filter.AppendFormat(" AND Domain='{0}'", user.Contains(@"\") ? user.Split('\\')[0] : Environment.MachineName);
			filter.AppendFormat(" AND UserName='{0}'", user.Contains(@"\") ? user.Split('\\')[1] : user);
			filter.AppendFormat(" AND UrlHost NOT LIKE '_:'");

			var groups = Database.Execute(db => db.ReadObjects<SnapGroup>("StatisticView", filter.ToString(), null, 4,
					"Domain", "UserName", "SessionDate", "UrlHost", "Sum(SnapCount) AS SnapCount"));

			List<object> array = new List<object>();
			foreach (var g in groups)
			{
				if (string.IsNullOrEmpty(g.UrlHost))
					continue;
				array.Add(new
					{
						User = g.User,
						Date = g.SessionDate,
						Host = g.UrlHost,
						Count = g.SnapCount,
					});
			}
			return array.ToArray();
		}

		#region Custom Objects
		public class SnapGroup
		{
			public string SessionId;
			public string SessionDate;
			public string Domain;
			public string UserName;

			public string ProcessName;
			public string UrlHost;
			public string SnapHour;
			public int SnapCount;

			public string User
			{
				get
				{
					return string.Equals(Domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ?
						UserName : string.Format(@"{0}\{1}", Domain, UserName);
				}
			}
		}
		#endregion
	}
}
