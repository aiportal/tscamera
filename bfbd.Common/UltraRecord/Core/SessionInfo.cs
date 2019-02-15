using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	[Serializable]
	public class SessionInfo
	{
		public string SessionId;
		public DateTime CreateTime = DateTime.Now;
		public string UserName;
		public string Domain;
		public string ClientName;
		public string ClientAddress;

		[NonSerialized]
		public DateTime LastActiveTime = DateTime.Now;
		[NonSerialized]
		public bool IsEnd = false;
		[NonSerialized]
		public int SnapshotCount;
		[NonSerialized]
		public long DataLength;

		public byte[] ToBinary()
		{
			Debug.Assert(Domain.Length < byte.MaxValue && UserName.Length < byte.MaxValue && ClientName.Length < byte.MaxValue && ClientAddress.Length < byte.MaxValue);
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
			{
				bw.Write(new byte[] { 1, 2, 6, 0 });
				bw.Write(new Guid(SessionId).ToByteArray());		// 16 bytes.
				bw.Write(CreateTime.ToBinary());					// 8 bytes.
				bw.Write(Domain);
				bw.Write(UserName);
				bw.Write(ClientName);
				bw.Write(ClientAddress);

				bw.Flush();
				return ms.ToArray();
			}
		}

		public static SessionInfo FromBinary(byte[] bs)
		{
			using (MemoryStream ms = new MemoryStream(bs))
			using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
			{
				byte[] version = br.ReadBytes(4);

				SessionInfo si = new SessionInfo();
				si.SessionId = new Guid(br.ReadBytes(16)).ToString("n");
				si.CreateTime = DateTime.FromBinary(br.ReadInt64());	// 8 bytes.
				si.Domain = br.ReadString();
				si.UserName = br.ReadString();
				si.ClientName = br.ReadString();
				si.ClientAddress = br.ReadString();
				return si;
			}
		}
	}
}
