using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace bfbd.UltraRecord.Core
{
	[Serializable]
	public class SessionInfo
	{
		public string SessionId;
		public DateTime CreateTime;
		public string UserName;
		public string Domain;
		public string ClientName;
		public string ClientAddress;

		[NonSerialized]
		public DateTime LastActiveTime;
		[NonSerialized]
		public bool IsEnd = false;
		[NonSerialized]
		public int SnapshotCount;
		[NonSerialized]
		public long DataLength;
	}
}
