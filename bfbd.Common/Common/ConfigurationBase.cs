using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;

namespace bfbd.Common
{
	public abstract partial class ConfigurationBase
	{
		private const string ROOT_ELEMENT = "Configuration";
		private object _root = new object();

		public Dictionary<string, object> GetConfigurations()
		{
			BindingFlags flag = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
			lock (_root)
			{
				Dictionary<string, object> dic = new Dictionary<string, object>();
				FieldInfo[] fields = this.GetType().GetFields(flag);
				foreach (FieldInfo f in fields)
				{
					if (!f.IsPrivate)
						dic.Add(f.Name, f.GetValue(this));
				}
				PropertyInfo[] props = this.GetType().GetProperties(flag);
				foreach (PropertyInfo p in props)
					dic.Add(p.Name, p.GetValue(this, null));
				return dic;
			}
		}

		public void SetConfigurations(IDictionary<string, object> dic)
		{
			try
			{
				BindingFlags flag = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | 
					BindingFlags.SetField | BindingFlags.SetProperty;
				lock (_root)
				{
					FieldInfo[] fields = this.GetType().GetFields(flag);
					foreach (var f in fields)
					{
						if (f.IsPrivate || f.IsInitOnly)
							continue;
						if (dic.ContainsKey(f.Name))
						{
							var val = DataConverter.ChangeType(dic[f.Name], f.FieldType);
							f.SetValue(this, val);
						}
					}
					PropertyInfo[] props = this.GetType().GetProperties(flag);
					foreach (var p in props)
					{
						if (dic.ContainsKey(p.Name))
						{
							var val = DataConverter.ChangeType(dic[p.Name], p.PropertyType);
							p.SetValue(this, val, null);
						}
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void SaveConfiguration(string filename)
		{
			try
			{
				Dictionary<string, object> dic = this.GetConfigurations();
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(string.Format(@"<{0}></{0}>", ROOT_ELEMENT));
				foreach (string key in dic.Keys)
				{
					var node = doc.CreateElement(key);
					node.InnerText = DataConverter.Serialize(dic[key]);
					doc.DocumentElement.AppendChild(node);
				}

				string xml;
				using (StringWriter sw = new StringWriter())
				using (XmlTextWriter tw = new XmlTextWriter(sw))
				{
					doc.WriteTo(tw);
					xml = sw.GetStringBuilder().ToString();
				}

				byte[] bs = Convert.FromBase64String(Encryption.Encrypt(xml));
				File.WriteAllBytes(filename, bs);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}

		public void LoadConfiguration(string filename)
		{
			try
			{
				byte[] bs = File.ReadAllBytes(filename);
				string xml = Encryption.Decrypt(Convert.ToBase64String(bs));

				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);
				XmlNode root = doc.SelectSingleNode(ROOT_ELEMENT);

				Dictionary<string, object> dic = new Dictionary<string, object>();
				for (int i = 0; i < root.ChildNodes.Count; ++i)
					dic[root.ChildNodes[i].Name] = root.ChildNodes[i].InnerText;

				this.SetConfigurations(dic);
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
		}
	}
}
