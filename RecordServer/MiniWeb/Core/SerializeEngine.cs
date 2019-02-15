using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace bfbd.MiniWeb.Core
{
	static partial class SerializeEngine
	{
		internal static void Serialize(object obj, Stream stream)
		{
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(stream, obj);
		}

		internal static object Deserialize(Stream stream)
		{
			BinaryFormatter bf = new BinaryFormatter();
			bf.Binder = new LocalAssemblyBinder();
			return bf.Deserialize(stream);
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
}
