using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace bfbd.UltraRecord
{
	static class Global
	{
		public static LocalConfig Config = new LocalConfig();
	}

	public class LocalConfig : bfbd.Common.ConfigurationBase
	{
		// Agent Parameters.
		public bool RecordEnabled = true;
		public bool IconVisible = true;
		public bool RecordImage = true;
		public bool AgentGrayScale = false;
		public string RecordPolicy = null;

		internal bool DebugDumpOriginal = false;
		internal bool DebugDumpText = false;
		//internal bool DebugDumpImage = false;

		// Agent Policy
		public string[] IncludeUsers;
		public string[] ExcludeUsers;

		//public string[] IncludeApps { get; set; }
		//public string[] ExcludeApps { get; set; }
		
		//public string[] IncludeHosts { get; set; }
		//public string[] ExcludeHosts { get; set; }

		// License info
		public string SerialKey = null;
		public string LicenseKey = null;
		public DateTime InstallTime = new DateTime(3000, 1, 1);
	}
}
