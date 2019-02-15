using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace bfbd.UltraRecord.Core
{
	using bfbd.Common.License;
	using bfbd.Common.Windows;

	partial class RawInputApp
	{
		private void UpdateConfiguration(object state)
		{
			TraceLogger.Instance.WriteLineVerbos("UpdateConfiguration ...");
			try
			{
				string fpath = System.IO.Path.Combine(Application.StartupPath, "record.config");
				if (File.Exists(fpath))
				{
					Global.Config.LoadConfiguration(fpath);
					if (IsLicenseValid())
					{
						IsRecording = Global.Config.RecordEnabled;
						//IsRecording &= IsUserRecording();
						UpdateNotify(APP_TITLE, Global.Config.IconVisible, false);
						return;
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			//UpdateNotify(@"License is not valid", true, true);
		}

		private void MakeShortSession(object state)
		{
			if (_session != null)
			{
				if (DateTime.Now.Subtract(_session.LastActiveTime).TotalMinutes > 15)
				{
					TraceLogger.Instance.WriteLineInfo("Record appplication will exit on idle: " + DateTime.Now.ToShortTimeString());
					_cacheManager.WriteSessionEnd(_session.SessionId);
					_tasks.Stop();
					Application.Exit();
				}
			}
		}

		private bool IsLicenseValid()
		{
			return true;

			///? 2015-4-29 发布免费版，不再做注册码验证
			//bool valid = false;
			//try
			//{
			//    // check license
			//    if (!string.IsNullOrEmpty(Global.Config.LicenseKey))
			//    {
			//        var sn = SerialNumber.DeSerialize(Global.Config.LicenseKey, Global.Config.InstallTime);
			//        return sn.IsValid();
			//    }
			//}
			//catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			//return valid;
		}

		[Conditional("Obsolete")]
		private void UpdateNotify(string msg, bool visible, bool tip)
		{
			if (_notifyIcon != null)
			{
				_notifyIcon.Text = msg;
				_notifyIcon.Visible = visible;
				if (tip)
					_notifyIcon.ShowBalloonTip(5 * 1000, APP_TITLE, msg, ToolTipIcon.Warning);
			}
		}

		private bool IsUserRecording()
		{
			var user = DomainUser.Current;
			if (Global.Config.ExcludeUsers != null && Global.Config.ExcludeUsers.Length > 0)
				return !Array.Exists(Global.Config.ExcludeUsers, s => user.Equals(s));
			else if (Global.Config.IncludeUsers != null)
				return Array.Exists(Global.Config.IncludeUsers, s => user.Equals(s));
			else
				return true;
		}
	}
}
