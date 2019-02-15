using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace bfbd.Common.Windows
{
	using NetFwTypeLib;

	static class FireWallEngine
	{
		public static void AuthorizeApplication(string name, string path)
		{
			try
			{
				var app = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication")) as INetFwAuthorizedApplication;
				app.Name = name;
				app.ProcessImageFileName = path;
				app.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
				app.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
				app.Enabled = true;

				var mgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr")) as INetFwMgr;
				mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public static void OpenFirewallPort(int port, string name, string comment = null)
		{
			try
			{
				var rule = (NetFwTypeLib.INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule"));
				rule.Name = name;
				rule.Description = comment;
				rule.Enabled = true;
				rule.Protocol = 6; // TCP
				rule.LocalPorts = port.ToString();

				var policy = (NetFwTypeLib.INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
				policy.Rules.Add(rule);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public static void RemoveFirewallRule(string name)
		{
			try
			{
				var policy = (NetFwTypeLib.INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
				policy.Rules.Remove(name);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public static bool EnableFirewallRule(string name, bool enable = true)
		{
			bool succeed = false;
			try
			{
				var policy = (NetFwTypeLib.INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
				var rule = policy.Rules.Item(name);
				if (rule != null)
				{
					rule.Enabled = enable;
					succeed = true;
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return succeed;
		}
	}
}
