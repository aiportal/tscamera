using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Security.Principal;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Security.AccessControl;

namespace bfbd.Common.Windows
{
	class ServiceInstaller
	{
		public void Install(string[] cmdArgs)
		{
			if (!IsAdministrator())
			{
				RunAsAdministrator(string.Join(" ", cmdArgs));
			}
			else
			{
				try
				{
					TransactedInstaller transactedInstaller = new TransactedInstaller();
					AssemblyInstaller assemblyInstaller = new AssemblyInstaller(Application.ExecutablePath, new string[] { });
					transactedInstaller.Installers.Add(assemblyInstaller);
					transactedInstaller.Install(new System.Collections.Hashtable());
				}
				catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			}
		}

		public void Uninstall(string[] cmdArgs)
		{
			if (!IsAdministrator())
			{
				RunAsAdministrator(string.Join(" ", cmdArgs));
			}
			else
			{
				try
				{
					TransactedInstaller transactedInstaller = new TransactedInstaller();
					AssemblyInstaller assemblyInstaller = new AssemblyInstaller(Application.ExecutablePath, new string[] { });
					transactedInstaller.Installers.Add(assemblyInstaller);
					transactedInstaller.Uninstall(null);
				}
				catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			}
		}

		private bool IsAdministrator()
		{
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		private void RunAsAdministrator(string arguments)
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = Application.ExecutablePath,
				Arguments = arguments,
				Verb = "runas"
			};
			try
			{
				Process.Start(psi);
			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
		}
	}
}
