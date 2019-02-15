using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.TSCamera.Core
{
	interface IStorage
	{
		Dictionary<string, object> GetConfigurations();
		void WriteSessionInfo(SessionInfo session);
		void WriteSnapshot(Snapshot sshot);
		void WriteSessionEnd(string sessionId);
	}
}
