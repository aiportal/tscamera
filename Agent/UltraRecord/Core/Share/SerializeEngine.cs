using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace bfbd.UltraRecord.Core
{
	using bfbd.UltraRecord.Core.New;

	static partial class SerializeEngine
	{
		public static void Serialize(object obj, string path)
		{
			using (FileStream fs = File.OpenWrite(path))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(fs, obj);
			}
		}

		public static object Deserialize(string path)
		{
			using (FileStream fs = File.OpenRead(path))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Binder = new LocalAssemblyBinder();
				return bf.Deserialize(fs);
			}
		}

		class LocalAssemblyBinder : System.Runtime.Serialization.SerializationBinder
		{
			public override Type BindToType(string assemblyName, string typeName)
			{
				Type type;
				try
				{
					string executingAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
					type = Type.GetType(String.Format("{0}, {1}", typeName, executingAssemblyName));
				}
				catch (Exception)
				{
					type = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
				}
				return type;
			}
		}
	}

	partial class SerializeEngine
	{
		public static byte[] SerializeEvents(IEnumerable<RawInputData> evts)
		{
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter bw = new BinaryWriter(ms))
			{
				KeyboardInputData kEvt;
				MouseInputData mEvt;
				foreach (RawInputData evt in evts)
				{
					kEvt = evt as KeyboardInputData;
					mEvt = evt as MouseInputData;
					if (kEvt != null)
					{
						bw.Write((long)kEvt.Time.ToBinary());
						bw.Write((int)RawInputEvent.KEvts[kEvt.Evt]);
						bw.Write((int)kEvt.Key);
					}
					if (mEvt != null)
					{
						bw.Write((long)mEvt.Time.ToBinary());
						bw.Write((int)RawInputEvent.MEvts[mEvt.Evt]);
						bw.Write((short)mEvt.X);
						bw.Write((short)mEvt.Y);
					}
				}
				return ms.ToArray();
			}
		}
	}
}
