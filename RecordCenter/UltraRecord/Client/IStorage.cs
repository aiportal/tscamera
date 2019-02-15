using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.UltraRecord.Client
{
	using bfbd.UltraRecord.Core;

	interface IStorage
	{
		Dictionary<string, object> GetConfigurations();
		void WriteSessionInfo(SessionInfo session);
		void WriteSnapshot(Snapshot sshot);
		void WriteSessionEnd(string sessionId);
	}
}
