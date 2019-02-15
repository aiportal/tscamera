using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera.Web
{
	using bfbd.Common;

	[bfbd.MiniWeb.Core.HttpRemoting("s")]
	class StorageRemoting : bfbd.TSCamera.Core.IStorage
	{
		public Dictionary<string, object> GetConfigurations()
		{
			return DataConverter.ToDictionary(Global.Config, true);
		}

		public void WriteSessionInfo(Core.SessionInfo session)
		{
			
		}

		public void WriteSnapshot(Core.Snapshot sshot)
		{
			
		}

		public void WriteSessionEnd(string sessionId)
		{
			
		}
	}
}
