using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera.Client
{
	using bfbd.TSCamera.Core;

	class RemotingStorage : IStorage, IDisposable
	{
		public void Dispose()
		{

		}

		public Dictionary<string, object> GetConfigurations()
		{
			return null;
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
