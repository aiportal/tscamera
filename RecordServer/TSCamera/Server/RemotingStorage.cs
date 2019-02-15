using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera.Server
{
	using bfbd.TSCamera.Core;

	class RemotingStorage : MarshalByRefObject, IStorage, IDisposable
	{
		public void Dispose()
		{
		}

		public Dictionary<string, object> GetConfigurations()
		{
			return Global.Config.GetConfigurations(true);
		}

		public void WriteSessionInfo(SessionInfo session)
		{
			throw new NotImplementedException();
		}

		public void WriteSnapshot(Snapshot sshot)
		{
			throw new NotImplementedException();
		}

		public void WriteSessionEnd(string sessionId)
		{
			throw new NotImplementedException();
		}
	}
}
