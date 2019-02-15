using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common
{
	public static class OSVersion
	{
		public static string GetName()
		{
			string name = "";
			Version ver = System.Environment.OSVersion.Version;
			//Major主版本号
			//Minor副版本号
			if (ver.Major == 5 && ver.Minor == 0)
			{
				name = "Windows 2000";
			}
			else if (ver.Major == 5 && ver.Minor == 1)
			{
				name = "Windows XP";
			}
			else if (ver.Major == 5 && ver.Minor == 2)
			{
				name = "Windows 2003";
			}
			else if (ver.Major == 6 && ver.Minor == 0)
			{
				name = "Windows Vista";
			}
			else if (ver.Major == 6 && ver.Minor == 1)
			{
				name = "Windows7";
			}
			else
			{
				name = "Unkown";
			}
			return name;
		}

		public static bool IsX64
		{
			get { return (IntPtr.Size == 8); }
		}

		public static bool IsVista
		{
			get { return Environment.OSVersion.Version.Major > 5; }
		}
	}
}
