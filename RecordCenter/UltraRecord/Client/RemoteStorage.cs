using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.UltraRecord.Client
{
	class RemoteStorage : IStorage
	{
		public Dictionary<string, object> GetConfigurations()
		{
			throw new NotImplementedException();
		}

		public void WriteSessionInfo(Core.SessionInfo session)
		{
			throw new NotImplementedException();
		}

		public void WriteSnapshot(Core.Snapshot sshot)
		{
			throw new NotImplementedException();
		}

		public void WriteSessionEnd(string sessionId)
		{
			throw new NotImplementedException();
		}
	}
}
