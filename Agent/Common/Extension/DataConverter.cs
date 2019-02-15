using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace bfbd.Common
{
	static class DataConverter
	{
		public static object ChangeType(object val, Type type)
		{
			object result = null;
			if (val != null && val != DBNull.Value)
			{
				Type srcType = val.GetType();
				if (srcType == type)
				{
					result = val;
				}
				else if (val is string)
				{
					string str = val as string;
					if (type.IsEnum)
						result = Enum.Parse(type, str);
					else if (type == typeof(string[]))
						result = string.IsNullOrEmpty(str) ? null : str.Split(',');
					else if (type == typeof(Guid))
						result = new Guid(val as string);
					else
						result = System.Convert.ChangeType(str, type);
				}
				else
				{
					if (type.IsEnum)
						result = Enum.ToObject(type, val);
					else if (srcType == typeof(Guid) && type == typeof(string))
						result = val.ToString();
					else
						result = System.Convert.ChangeType(val, type);
				}
			}
			return result;
		}

		public static string Serilize(object val)
		{
			string result = null;
			if (val != null)
			{
				Type type = val.GetType();
				if (type == typeof(string[]))
				{
					result = string.Join(",", val as string[]);
				}
				else
				{
					var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
					if (fields.Length > 0)
					{
						//Array.Sort(fields);
						string[] vals = Array.ConvertAll(fields,
							f => System.Convert.ToString(f.GetValue(val))
						);
						result = string.Join(":", vals);
					}
					else
					{
						result = val.ToString();
					}
				}
			}
			return result;
		}

		public static T Convert<T>(object val)
		{
			return (T)ChangeType(val, typeof(T));
		}

		public static T Convert<T>(DataRow row)
			where T : new()
		{
			T result = new T();
			var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var f in fields)
			{
				var col = row.Table.Columns[f.Name];
				if (col != null)
					f.SetValue(result, ChangeType(row[col], f.FieldType));
			}
			var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var p in props)
			{
				var col = row.Table.Columns[p.Name];
				if (col != null)
					p.SetValue(result, ChangeType(row[col], p.PropertyType), null);
			}
			return result;
		}
	}
}

