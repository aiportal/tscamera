using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;

namespace bfbd.Common
{
	public abstract partial class ConfigurationBase
	{
		private object _root = new object();

		public Dictionary<string, object> GetConfigurations(bool hasInternal = false)
		{
			lock (_root)
			{
				return DataConverter.ToDictionary(this, hasInternal);
			}
		}

		public void SetConfigurations(IDictionary<string, object> dic, bool hasInternal)
		{
			try
			{
				lock (_root)
				{
					DataConverter.Convert<ConfigurationBase>(dic, this, hasInternal);
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}
	}
}
