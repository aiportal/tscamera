using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace bfbd.TSCamera
{
	using bfbd.Common;

	public static class Global
	{
		static Global() { }

		public static LocalConfig Config = new LocalConfig();
		internal static GlobalTask Tasks = new GlobalTask();
	}

	class GlobalTask
	{
		#region Tasks
		internal readonly string UpdateConfigurations = "Configuration";
		internal readonly string RecordingSessions = "Session";
		#endregion Tasks
	}

	[Serializable]
	public partial class LocalConfig : ConfigurationBase
	{
		public string AdminServerAddr = "127.0.0.1";
		public int AdminServerPort = 1081;
	}

	partial class LocalConfig
	{
		// Agent policy
		public bool RecordEnabled = true;
		public bool IconVisible = true;
		internal readonly bool RecordImage = true;
		internal readonly bool AgentGrayScale = false;
		public bool GrayScale = true;

		internal string RecordPolicy = null;
		internal bool DebugDumpOriginal = false;
		internal bool DebugDumpText = false;

		public string[] IncludeUsers;
		public string[] ExcludeUsers;
		public string[] IncludeGroups;
		public string[] ExcludeGroups;
		public string[] IncludeApps;
		public string[] ExcludeApps;
		///> public string[] IncludeHosts;
		///> public string[] ExcludeHosts;

		// License info
		internal string SerialKey = null;
		internal string LicenseKey = null;
		internal DateTime InstallTime = new DateTime(3000, 1, 1);

		// Admin config items.
		//internal int AdminWebPort = 1088;
		//internal string AdminAccount = "admin";
		//internal string AdminPassword = Encryption.Encrypt(@"admin");

		#region LDAP

		public string ADPath { get; set; }
		public string ADUser { get; set; }
		public string ADPassword
		{
			internal get { return Encryption.Encrypt(ADPasswordValue); }
			set { try { ADPasswordValue = Encryption.Decrypt(value); } catch (Exception) { ADPasswordValue = value; } }
		}
		public string ADOrganization { get; set; }
		internal string ADPasswordValue;
		internal bool ADValid { get { return !string.IsNullOrEmpty(ADPath) && !string.IsNullOrEmpty(ADUser) && !string.IsNullOrEmpty(ADPasswordValue); } }

		#endregion LDAP

		// Client config items
		//internal string StorageUrl = null;
		//internal string ClientId = null;
		//internal TimeSpan TimeDelay = TimeSpan.MinValue;	// server time subtract local time.
	}

	public enum CompressionType
	{
	    TextOnly = 0,
	    GrayScale = 1,
		IndexedColor = 2,
		ShortColor = 3, 
	    RawImage = 4
	}
}
