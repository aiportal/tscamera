using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Windows
{
	class OSInfo
	{
		public static bool IsVista
		{
			get { return Environment.OSVersion.Version.Major > 5; }
		}

		public static bool IsX64
		{
			get { return (IntPtr.Size == 8); }
		}

		public static string Name
		{
			get
			{
				string ver = string.Format("{0}.{1}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor);
				switch (ver)
				{
					case "5.0": return "Windows 2000";
					case "5.1": return "Windows XP";
					case "5.2": return "Windows 2003";
					case "6.0": return "Windows Vista";
					case "6.1": return "Windows7";
					case "6.2": return "Windows 2008";
					default: return "Unkown";
				}
			}
		}
	}
}
