using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace bfbd.UltraRecord.Client
{
	using bfbd.Common;
	using bfbd.Common.License;
	using bfbd.Common.Windows;
	using bfbd.UltraRecord.Core;

	partial class RecordServiceCore
	{
		private IStorage _storage = new LocalStorage();

		private void UpdateLicenseInfo(object state)
		{
			try
			{
				Global.Config.SerialKey = null;
				Global.Config.LicenseKey = null;

				string path = Path.Combine(Path.GetDirectoryName(Application.StartupPath), "license.sn");
				if (File.Exists(path))
				{
					var config = _storage.GetConfigurations();
					bfbd.UltraRecord.Global.Config.SetConfigurations(config);

					TraceLogger.Instance.WriteLineVerbos("License file exists.");
					string lic = File.ReadAllText(path);
					var sn = SerialNumber.DeSerialize(lic.Substring(8), Global.Config.InstallTime);
					TraceLogger.Instance.WriteLineVerbos(string.Format("SN: {0}: {1}, {2}", sn.License, sn.CreateTime, sn.ExpireTime));
					if (sn.IsValid())
					{
						TraceLogger.Instance.WriteLineVerbos("License file valid: " + lic.Length);
						Global.Config.SerialKey = sn.MachineId;
						Global.Config.LicenseKey = lic.Substring(8);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		private void UpdateConfigurationFile(object state)
		{
			string path = System.IO.Path.Combine(Application.StartupPath, "record.config");
			var config = _storage.GetConfigurations();
			Global.Config.SetConfigurations(config);
			Global.Config.SaveConfiguration(path);
		}

		private void ScanWinSessionsToRecordOrEnd(object state)
		{
			int[] winSessions = WTSEngine.GetActiveSessions();
			var rcdProcesses = GetRcordingProcesses();

			// if not recording, record it.
			foreach (int sid in winSessions)
			{
				if (Array.Find(rcdProcesses, p => p.SessionId == sid) == null)
				{
					string user = WTSEngine.GetDomainUserBySessionId(sid);
					if (UserPolicy.IsUserRecording(user))
					{
						TraceLogger.Instance.WriteLineInfo("Start recording by configuration. user: " + user);
						this.SessionLogon(sid);
					}
				}
			}

			// if exclude recoding, kill it.
			//foreach (var proc in rcdProcesses)
			//{
			//    string user = WTSEngine.GetDomainUserBySessionId(proc.SessionId);
			//    if (!IsUserRecording(user))
			//    {
			//        TraceLogger.Instance.WriteLineInfo("Stop recording by configuration. user: " + user);
			//        try { proc.Kill(); }
			//        catch (Exception) { }
			//        this.SessionLogoff(proc.SessionId);
			//    }
			//}

			// if session not active, remove watcher.
			foreach (int sid in _watchers.Keys)
			{
				if (!Array.Exists(winSessions, s => s == sid))
					this.SessionLogoff(sid);
			}

			///? bug fix: double agent process in windows 7.
			List<int> sessions = new List<int>();
			foreach (var proc in rcdProcesses)
			{
				if (sessions.Contains(proc.SessionId))
				{
					TraceLogger.Instance.WriteLineInfo("Kill recording agent because double process. sessionId: " + proc.SessionId);
					try { proc.Kill(); }
					catch (Exception) { }
				}
				else
				{
					sessions.Add(proc.SessionId);
				}
			}

			// dispose
			Array.ForEach(rcdProcesses, p => p.Dispose());
		}

		private Process[] GetRcordingProcesses()
		{
			List<Process> rcdProcesses = new List<Process>();
			foreach (var p in Process.GetProcessesByName("rcda"))
			{
				try
				{
					if (Path.GetDirectoryName(p.MainModule.FileName) == Application.StartupPath)
						rcdProcesses.Add(p);
					else
						p.Dispose();
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			}
			return rcdProcesses.ToArray();
		}

		private void UpdateWebAccessPolicy(object state)
		{
			if (_adminWeb != null)
				_adminWeb.UpdateAccessPolicy();
		}

		//private bool IsUserRecording(string user)
		//{
		//    // if not in ExcludeUsers
		//    {
		//        // if not in ExcludeGroups
		//        // return false;
		//    }
		//    //if in IncludeUsers Or IncludeGroups
		//    //return true;

		//    bool record;
		//    // user policy
		//    {
		//        if (Global.Config.ExcludeUsers != null && Global.Config.ExcludeUsers.Length > 0)
		//        {
		//            record = !Array.Exists(Global.Config.ExcludeUsers, s => DomainUser.Equals(s, user));
		//        }
		//        else if (Global.Config.IncludeUsers != null && Global.Config.IncludeUsers.Length > 0)
		//        {
		//            record = Array.Exists(Global.Config.IncludeUsers, s => DomainUser.Equals(s, user));
		//        }
		//    }
		//    // group policy
		//    {
		//        var u = DomainUser.Create(user);
		//        if (u.IsSystemUser)
		//        {
		//            string[] groups = new bfbd.Common.Windows.SystemUserAccess().GetUserGroups(u.UserName);
		//            if (Global.Config.ExcludeGroups != null && Global.Config.ExcludeGroups.Length > 0)
		//            {
		//                record = !Array.Exists(Global.Config.ExcludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
		//            }
		//            else if (Global.Config.IncludeGroups != null && Global.Config.IncludeGroups.Length > 0)
		//            {
		//                record = Array.Exists(Global.Config.IncludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
		//            }
		//            else
		//            {
		//                record = true;
		//            }
		//        }
		//        else
		//        {
		//            record = true;
		//            if (Global.Config.ADValid)
		//            {
		//                try
		//                {
		//                    var cfg = Global.Config;
		//                    var ada = new ADUserAccess(cfg.ADPath, cfg.ADUser, cfg.ADPasswordValue, cfg.ADOrganization);
		//                    if (string.Equals(u.Domain, ada.DomainName, StringComparison.OrdinalIgnoreCase))
		//                    {
		//                        var groups = ada.GetUserGroups(u.UserName);
		//                        if (Global.Config.ExcludeGroups != null && Global.Config.ExcludeGroups.Length > 0)
		//                        {
		//                            record = !Array.Exists(Global.Config.ExcludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
		//                        }
		//                        else if (Global.Config.IncludeGroups != null && Global.Config.IncludeGroups.Length > 0)
		//                        {
		//                            record = Array.Exists(Global.Config.IncludeGroups, s => Array.Exists(groups, g => string.Equals(s, g, StringComparison.OrdinalIgnoreCase)));
		//                        }
		//                    }
		//                }
		//                catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		//            }
		//        }
		//    }
		//    return record;
		//}
	}
}

//private void _ScanWinSessionsToRecordOrEnd(object state)
//{
//    // enum active sessions, if not recording, record it.
//    int[] winSessions = WTSEngine.GetActiveSessions();
//    int[] rcdSessions = Array.ConvertAll(Process.GetProcessesByName("rcda"), p => p.SessionId);
//    foreach (int sid in winSessions)
//    {
//        if (!Array.Exists(rcdSessions, r => r == sid))
//        {
//            TraceLogger.Instance.WriteLineInfo("Session not recording: " + sid);
//            this.SessionLogon(sid);
//        }
//    }

//    // enum SessionWatchers, if session not active, close it.
//    foreach (int sid in _watchers.Keys)
//    {
//        if (!Array.Exists(winSessions, s => s == sid))
//            this.SessionLogoff(sid);
//    }

//    ///? bug fix: double agent process in windows 7.
//    try
//    {
//        List<int> winSessionsMonitored = new List<int>();
//        foreach (Process p in Process.GetProcessesByName("rcda"))
//        {
//            string fname = null;
//            try { fname = p.MainModule.FileName; }
//            catch (Exception) { }
//            if (fname != null && Path.GetDirectoryName(fname) == Application.StartupPath)
//            {
//                if (!winSessionsMonitored.Contains(p.SessionId))
//                {
//                    winSessionsMonitored.Add(p.SessionId);
//                }
//                else
//                {
//                    TraceLogger.Instance.WriteLineWarning("Double agents for session: " + p.SessionId);
//                    try { p.Kill(); }
//                    catch (Exception) { }
//                }
//            }
//        }
//    }
//    catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
//}

