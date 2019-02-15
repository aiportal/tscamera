using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace bfbd.Common
{
	static partial class Serialization
	{
		public static string ToXml(object obj, string rootElement = null)
		{
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			doc.LoadXml(string.Format(@"<{0}></{0}>", obj.GetType().Name));

			PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var p in props)
			{
				var node = doc.CreateElement(p.Name);
				node.InnerText = Convert.ToString(p.GetValue(obj, null));
				doc.DocumentElement.AppendChild(node);
			}
			return doc.OuterXml;
		}

		public static T FromXml<T>(string xml)
			where T : new()
		{
			T result = new T();
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			doc.LoadXml(xml);

			if (doc.HasChildNodes)
			{
				PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
				foreach (var p in props)
				{
					var node = doc.DocumentElement[p.Name];
					if (node != null)
						p.SetValue(result, Convert.ChangeType(node.InnerText, p.PropertyType), null);
				}
			}
			return result;
		}

		public static string ToJson(object obj)
		{
			//return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
			throw new NotImplementedException();
		}

		public static object FromJson(string json)
		{
			//return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
			throw new NotImplementedException();
		}

		public static byte[] ToBinary(object obj)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		public static object FromBinary(byte[] bs, bool verifyAssembly = false)
		{
			using (MemoryStream ms = new MemoryStream(bs))
			{
				BinaryFormatter bf = new BinaryFormatter();
				if (!verifyAssembly)
					bf.Binder = new LocalAssemblyBinder();
				return bf.Deserialize(ms);
			}
		}

		public static void ToBinaryFile(object obj, string path)
		{
			using (FileStream fs = File.OpenWrite(path))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(fs, obj);
			}
		}

		public static object FromBinaryFile(string path, bool verifyAssembly = false)
		{
			using (FileStream fs = File.OpenRead(path))
			{
				BinaryFormatter bf = new BinaryFormatter();
				if (!verifyAssembly)
					bf.Binder = new LocalAssemblyBinder();
				return bf.Deserialize(fs);
			}
		}
	}

	static partial class Serialization
	{
		public static Dictionary<string, object> ToDictionary(object obj)
		{
			System.Diagnostics.Debug.Assert(obj != null);
			Dictionary<string, object> dic = new Dictionary<string, object>();
			FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (FieldInfo f in fields)
				dic.Add(f.Name, f.GetValue(obj));
			PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (PropertyInfo p in props)
				dic.Add(p.Name, p.GetValue(obj, null));
			return dic;
		}

		public static T FromDictionary<T>(Dictionary<string, object> dic)
			where T : new()
		{
			object obj = new T();
			return (T)FromDictionary(dic, ref obj);
		}
		
		public static object FromDictionary(Dictionary<string, object> dic, ref object obj)
		{
			System.Diagnostics.Debug.Assert(obj != null);
			FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var f in fields)
			{
				if (dic.ContainsKey(f.Name))
				{
					var val = DataConverter.ChangeType(dic[f.Name], f.FieldType);
					f.SetValue(obj, val);
				}
			}
			PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var p in props)
			{
				if (dic.ContainsKey(p.Name))
				{
					var val = DataConverter.ChangeType(dic[p.Name], p.PropertyType);
					p.SetValue(obj, val, null);
				}
			}
			return obj;
		}
	}

	internal class LocalAssemblyBinder : System.Runtime.Serialization.SerializationBinder
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
