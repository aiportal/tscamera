using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Web;
using System.Drawing;

namespace bfbd.UltraRecord.Server
{
	using bfbd.Common;
	using bfbd.Common.Windows;
	using bfbd.UltraRecord.Core;
	using StorageEngine = bfbd.UltraRecord.Client.StorageEngine;

	public partial class DataQueryService
	{
		public object[] GetSessions(DateTime start, DateTime end, string user)
		{
			StringBuilder sbFilter = new StringBuilder();
			sbFilter.AppendFormat(" '{0:yyyy-MM-dd}'<=date(CreateTime) AND date(CreateTime)<='{1:yyyy-MM-dd}' ", start, end);
			sbFilter.Append(" AND SnapshotCount>0 ");
			if (!string.IsNullOrEmpty(user) && ValidUser(user))
			{
				var domainUser = DomainUser.Create(user);
				if (domainUser != null)
					sbFilter.AppendFormat(" AND Domain='{0}' AND UserName='{1}'", domainUser.Domain, domainUser.UserName);
			}

			var sessions = Database.Execute(db => db.SelectObjects<SessionObj>("SessionView", sbFilter.ToString(),
				"SessionId", "CreateTime", "LastActiveTime", "UserName", "Domain", "ClientName", "ClientAddress", "IsEnd", "SnapshotCount", "DataLength"));

			List<object> objs = new List<object>();
			foreach (var s in sessions)
				objs.Add(new
				{
					SID = s.SID,
					Date = s.Date,
					User = s.User,
					Time = s.TimeRange,
					Count = s.SnapshotCount,
					Length = s.DataLength,
					Active = s.IsActive,
					Client = s.ClientName,
					Address = s.ClientAddress,
				});

			return objs.ToArray();
		}

		// Snapshots for session detail.
		public object[] GetSnapshots(Guid sid)
		{
			var segments = Database.Execute(db => db.SelectObjects<SnapshotGroup>("SearchTitle", new { SessionId = sid.ToString("n") },
				"SessionId", "ProcessName", "WindowTitle", "StartTime", "EndTime", "SnapshotCount"));

			List<object> objs = new List<object>();
			foreach (var sg in segments)
				objs.Add(new {
					SID = sg.SID,
					Prog = sg.ProcessName,
					Title = sg.WindowTitle,
					Time = sg.TimeRange,
					Count = sg.SnapshotCount,
				});

			return objs.ToArray();
		}

		// Snapshots for movie
		public object[] SnapshotsByTitle(Guid sid, string prog, string title)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat("SessionId='{0}'", sid.ToString("n"));
			filter.Append(" AND ImageLen > 0 ");
			if (!string.IsNullOrEmpty(prog) && ValidProgram(prog))
				filter.AppendFormat(" AND ProcessName='{0}' ", prog);
			if (!string.IsNullOrEmpty(title) && ValidMd5(title))
				filter.AppendFormat(" AND md5(WindowTitle)='{0}' ", title.ToUpper());

			var snapshots = Database.Execute(db => db.SelectObjects<Snapshot>("SnapshotView", filter.ToString(),
				"SnapTime", 0,
				"SnapshotId", "BackgroundId", "ProcessName", "WindowTitle", "SnapTime", "MouseState as Mouse"));

			List<object> objs = new List<object>();
			foreach (var ss in snapshots)
				objs.Add(new
				{
					//SID = sid.ToString("n"),
					SSID = ss.SnapshotId.Replace("-",""),
					BGID = ss.BackgroundId,
					Prog = ss.ProcessName,
					Title = ss.WindowTitle,
					Time = string.Format("{0:HH:mm:ss}", ss.SnapTime),
					Mouse = ss.Mouse
				});

			return objs.ToArray();
		}
		
		// Snapshots for movie
		public object[] SnapshotsByUrl(Guid sid, string host, string url)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat("SessionId='{0}'", sid.ToString("n"));
			filter.Append(" AND ImageLen > 0 ");
			if (!string.IsNullOrEmpty(host))
				filter.AppendFormat(" AND UrlHost='{0}' ", host);
			if (!string.IsNullOrEmpty(url) && ValidMd5(url))
				filter.AppendFormat(" AND md5(WindowUrl)='{0}' ", url.ToUpper());

			var snapshots = Database.Execute(db => db.SelectObjects<Snapshot>("SnapshotView", filter.ToString(),
				"SnapTime", 0,
				"SnapshotId", "BackgroundId", "UrlHost", "WindowUrl", "SnapTime", "MouseState as Mouse"));

			List<object> objs = new List<object>();
			foreach (var ss in snapshots)
				objs.Add(new
				{
					//SID = sid.ToString("n"),
					SSID = ss.SnapshotId.Replace("-", ""),
					BGID = ss.BackgroundId,
					Host = ss.UrlHost,
					Url = ss.WindowUrl,
					Time = string.Format("{0:HH:mm:ss}", ss.SnapTime),
					Mouse = ss.Mouse
				});

			return objs.ToArray();
		}

		public object[] SearchTitle(DateTime start, DateTime end, string user, string prog, string title)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=date(StartTime) AND date(StartTime)<='{1:yyyy-MM-dd}' ", start, end);
			if (!string.IsNullOrEmpty(user) && ValidUser(user))
			{
				var domainUser = DomainUser.Create(user);
				if (domainUser != null)
					filter.AppendFormat(" AND Domain='{0}' AND UserName='{1}'", domainUser.Domain, domainUser.UserName);
			}
			if (!string.IsNullOrEmpty(prog) && ValidProgram(prog))
				filter.AppendFormat(" AND ProcessName='{0}' ", prog);
			if (!string.IsNullOrEmpty(title) && ValidKey(title))
				filter.AppendFormat(" AND WindowTitle LIKE '%{0}%' ", title);

			var segments = Database.Execute(db => db.SelectObjects<SnapshotGroup>("SearchTitle", filter.ToString(),
				"SessionDate", "SessionId", "Domain", "UserName", "ProcessName", "WindowTitle", "StartTime", "EndTime", "SnapshotCount"));

			List<object> objs = new List<object>();
			foreach (var sg in segments)
				objs.Add(new
				{
					Date = sg.SessionDate,
					User = sg.User,
					Prog = sg.ProcessName,
					Title = sg.WindowTitle,
					SID = sg.SessionId,
					Time = sg.TimeRange,
					Count = sg.SnapshotCount,
				});
			return objs.ToArray();
		}

		public object[] SearchText(DateTime start, DateTime end, string user, string prog, string text)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=date(StartTime) AND date(StartTime)<='{1:yyyy-MM-dd}' ", start, end);
			filter.Append(" AND length(trim(InputText)) > 0 ");
			if (!string.IsNullOrEmpty(user) && ValidUser(user))
			{
				var domainUser = DomainUser.Create(user);
				if (domainUser != null)
					filter.AppendFormat(" AND Domain='{0}' AND UserName='{1}'", domainUser.Domain, domainUser.UserName);
			}
			if (!string.IsNullOrEmpty(prog) && ValidProgram(prog))
				filter.AppendFormat(" AND ProcessName='{0}' ", prog);
			if (!string.IsNullOrEmpty(text) && ValidKey(text))
				filter.AppendFormat(" AND InputText LIKE '%{0}%' ", text);

			var segments = Database.Execute(db => db.SelectObjects<SnapshotGroup>("SearchText", filter.ToString(),
				"SessionDate", "SessionId", "Domain", "UserName", "ProcessName", "WindowTitle", "StartTime", "EndTime", "InputText", "SnapshotCount"));

			List<object> objs = new List<object>();
			foreach (var sg in segments)
				objs.Add(new
				{
					Date = sg.SessionDate,
					User = sg.User,
					Prog = sg.ProcessName,
					Title = sg.WindowTitle,
					Text = sg.Text,
					SID = sg.SessionId,
					Time = sg.TimeRange,
					Count = sg.SnapshotCount,
				});
			return objs.ToArray();
		}

		public object[] SearchUrl(DateTime start, DateTime end, string user, string host, string url)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=date(StartTime) AND date(StartTime)<='{1:yyyy-MM-dd}' ", start, end);
			if (!string.IsNullOrEmpty(user) && ValidUser(user))
			{
				var domainUser = DomainUser.Create(user);
				if (domainUser != null)
					filter.AppendFormat(" AND Domain='{0}' AND UserName='{1}'", domainUser.Domain, domainUser.UserName);
			}
			if (!string.IsNullOrEmpty(host) && ValidHost(host))
				filter.AppendFormat(" AND UrlHost='{0}' ", host);
			else
				filter.Append(@" AND UrlHost NOT LIKE '_:'");
			if (!string.IsNullOrEmpty(url) && ValidKey(url))
				filter.AppendFormat(" AND WindowUrl LIKE '%{0}%' ", url);

			var segments = Database.Execute(db => db.SelectObjects<SnapshotGroup>("SearchUrl", filter.ToString(),
				"SessionDate", "SessionId", "Domain", "UserName", "StartTime", "EndTime", "UrlHost", "WindowUrl", "SnapshotCount"));

			List<object> objs = new List<object>();
			foreach (var sg in segments)
				objs.Add(new
				{
					SID = sg.SID,
					Date = sg.SessionDate,
					User = sg.User,
					Host = sg.UrlHost,
					Time = sg.TimeRange,
					Url = sg.WindowUrl,
					Count = sg.SnapshotCount,
				});

			return objs.ToArray();
		}

		public object[] SearchFile(DateTime start, DateTime end, string user, string drive, string file)
		{
			StringBuilder filter = new StringBuilder();
			filter.AppendFormat(" '{0:yyyy-MM-dd}'<=date(StartTime) AND date(StartTime)<='{1:yyyy-MM-dd}' ", start, end);
			if (!string.IsNullOrEmpty(user) && ValidUser(user))
			{
				var domainUser = DomainUser.Create(user);
				if (domainUser != null)
					filter.AppendFormat(" AND Domain='{0}' AND UserName='{1}'", domainUser.Domain, domainUser.UserName);
			}
			if (!string.IsNullOrEmpty(drive) && ValidDrive(drive))
				filter.AppendFormat(" AND UrlHost='{0}' ", drive);
			else
				filter.AppendFormat(" AND UrlHost LIKE '_:'");
			if (!string.IsNullOrEmpty(file) && ValidKey(file))
				filter.AppendFormat(" AND WindowUrl LIKE 'file:///%{0}%' ", file);

			var segments = Database.Execute(db => db.SelectObjects<SnapshotGroup>("SearchUrl", filter.ToString(),
				"SessionDate", "SessionId", "Domain", "UserName", "StartTime", "EndTime", "UrlHost", "WindowUrl", "SnapshotCount"));

			List<object> objs = new List<object>();
			foreach (var sg in segments)
				objs.Add(new
				{
					SID = sg.SID,
					Date = sg.SessionDate,
					User = sg.User,
					Host = sg.UrlHost,
					Time = sg.TimeRange,
					Url = sg.WindowUrl,
					Count = sg.SnapshotCount,
				});

			return objs.ToArray();
		}

		[bfbd.MiniWeb.RawResponse("Response")]
		public void GetImage(bfbd.MiniWeb.HttpResponse Response, Guid ssid)
		{
			byte[] bsImage = StorageEngine.LoadSnapshotImage(ssid.ToString("n"));
			Response.SendImage(bsImage, "png");
		}

		#region Custom objscts

		public class SessionObj
		{
			public string SessionId;
			public string Domain;
			public string UserName;
			public string ClientName;
			public string ClientAddress;

			public DateTime CreateTime;
			public DateTime LastActiveTime;
			public bool IsEnd = false;
			public int SnapshotCount;
			public long DataLength;

			public string SID { get { return SessionId == null ? null : SessionId.Replace("-", ""); } }
			public string Date { get { return string.Format("{0:yyyy-MM-dd}", CreateTime); } }
			public string User
			{
				get
				{
					return string.Equals(Domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ?
						UserName : string.Format(@"{0}\{1}", Domain, UserName);
				}
			}
			public string TimeRange { get { return string.Format("{0:HH:mm} - {1:HH:mm}", CreateTime, LastActiveTime); } }
			public bool IsActive { get { return !IsEnd; } }
		}

		public class SnapshotGroup
		{
			public string SessionDate = null;
			public string SessionId = null;
			public string Domain = null;
			public string UserName = null;
			public string ProcessName = null;
			public string WindowTitle = null;
			public string InputText = null;
			public string WindowUrl = null;
			public string UrlHost = null;

			public DateTime StartTime = DateTime.MaxValue;
			public DateTime EndTime = DateTime.MinValue;
			public int SnapshotCount;

			public string SID { get { return SessionId == null ? null : SessionId.Replace("-", ""); } }
			public string User
			{
				get
				{
					return string.Equals(Domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ?
						UserName : string.Format(@"{0}\{1}", Domain, UserName);
				}
			}
			public string TimeRange { get { return string.Format("{0:HH:mm} - {1:HH:mm}", StartTime, EndTime); } }
			public string Text { get { return InputText == null ? null : InputText.ToLower(); } }
		}
		#endregion
	}
}
