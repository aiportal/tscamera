using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Diagnostics;

namespace bfbd.Common
{
	/// <summary>
	/// Core API
	/// </summary>
	partial class DataConverter
	{
		#region Base Interface

		public static T Convert<T>(object obj)
		{
			return (obj == null) ? default(T) : (T)ChangeType(obj, typeof(T));
		}

		public static string Convert(object obj)
		{
			return (obj == null) ? null : ChangeType(obj, typeof(string)) as string;
		}

		public static object ChangeType(object obj, Type type)
		{
			if (obj == null || obj == DBNull.Value)
				return null;
			if (type == typeof(object) || type == obj.GetType())
				return obj;
			if (obj is string && string.IsNullOrEmpty(obj as string))
				return null;

			object result = null;
			try
			{
				type = Nullable.GetUnderlyingType(type) ?? type;
				if (obj is string)
					result = ObjectFromString(obj as string, type);
				else if (type == typeof(string))
					result = ObjectToString(obj);
				else if (type.IsEnum)
					result = Enum.ToObject(type, obj);
				else
					result = System.Convert.ChangeType(obj, type);
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return result;
		}

		#endregion Base Interface

		#region Extend Interface

		public static T Convert<T>(DataRow row, object obj)
		{
			if (obj == null)
				obj = System.Activator.CreateInstance<T>();
			Debug.Assert(obj is T);
			return (T)StructFromValues(obj, (s) => row.Table.Columns[s] != null ? row[row.Table.Columns[s]] : null, true, false);
		}

		public static T Convert<T>(DbDataReader reader, object obj)
		{
			if (obj == null)
				obj = System.Activator.CreateInstance<T>();
			Debug.Assert(obj is T);
			return (T)StructFromValues(obj, (s) => reader.GetOrdinal(s) >= 0 ? reader.GetValue(reader.GetOrdinal(s)) : null, true, false);
		}

		public static T Convert<T>(NameValueCollection vals, object obj = null)
		{
			if (obj == null)
				obj = System.Activator.CreateInstance<T>();
			Debug.Assert(obj is T);
			return (T)StructFromValues(obj, (s) => vals[s], true, false);
		}

		public static T Convert<T>(IDictionary<string, object> vals, object obj, bool hasInternal)
		{
			if (obj == null)
				obj = System.Activator.CreateInstance<T>();
			Debug.Assert(obj is T);
			return (T)StructFromValues(obj, (k) => vals.ContainsKey(k) ? vals[k] : null, true, hasInternal);
		}

		public static Dictionary<string, object> ToDictionary(object obj, bool hasInternal)
		{
			var dic = new Dictionary<string, object>();
			StructToValues(obj, (k, v) => dic[k] = v, true, hasInternal);
			return dic;
		}

		#endregion Extend Interface
	}

	/// <summary>
	/// Implementation
	/// </summary>
	partial class DataConverter
	{
		#region Extend Implement

		private static object StructFromValues(object obj, GetValueCallback getValue, bool hasField, bool hasInternal)
		{
			Debug.Assert(obj != null);
			try
			{
				// properties
				{
					var flags = BindingFlags.Instance | BindingFlags.Public | (hasInternal ? BindingFlags.NonPublic : 0);
					var props = obj.GetType().GetProperties(flags);
					foreach (var p in props)
					{
						if (hasInternal)
						{
							var m = p.GetSetMethod(true);
							if (m.IsPrivate || m.IsFamily)
								continue;
						}
						if (p.CanWrite)
						{
							var v = getValue(p.Name);
							if (v != null)
								p.SetValue(obj, ChangeType(v, p.PropertyType), null);
						}
					}
				}
				// fields
				if (hasField)
				{
					var flags = BindingFlags.Instance | BindingFlags.Public | (hasInternal ? BindingFlags.NonPublic : 0);
					var flds = obj.GetType().GetFields(flags);
					foreach (var f in flds)
					{
						if (f.IsPrivate || f.IsFamily || f.IsInitOnly)
							continue;
		
						var v = getValue(f.Name);
						if (v != null)
							f.SetValue(obj, ChangeType(v, f.FieldType));
					}
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return obj;
		}

		private static void StructToValues(object obj, SetValueCallback setValue, bool hasField, bool hasInternal)
		{
			Debug.Assert(obj != null);
			try
			{
				// properties
				{
					var flags = BindingFlags.Instance | BindingFlags.Public | (hasInternal ? BindingFlags.NonPublic : 0);
					var props = obj.GetType().GetProperties(flags);
					foreach (var p in props)
					{
						if (hasInternal)
						{
							var m = p.GetSetMethod(true);
							if (m.IsPrivate || m.IsFamily)
								continue;
						}
						if (p.CanRead)
						{
							setValue(p.Name, p.GetValue(obj, null));
						}
					}
				}
				// fields
				if (hasField)
				{
					var flags = BindingFlags.Instance | BindingFlags.Public | (hasInternal ? BindingFlags.NonPublic : 0);
					var flds = obj.GetType().GetFields(flags);
					foreach (var f in flds)
					{
						if (f.IsPrivate || f.IsFamily || f.IsInitOnly)
							continue;
						setValue(f.Name, f.GetValue(obj));
					}
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		delegate object GetValueCallback(string name);
		delegate void SetValueCallback(string name, object value);

		#endregion Extend Implement
		
		#region Base Implement

		private static object ObjectFromString(string str, Type type)
		{
			object result = null;
			if (string.IsNullOrEmpty(str))
				result = type.IsValueType ? Activator.CreateInstance(type) : null;
			else if (type == typeof(Guid))
				result = new Guid(str);
			else if (type == typeof(DateTime))
				result = DateTime.Parse(str);
			else if (type == typeof(Decimal))
				result = Decimal.Parse(str);
			else if (type.IsEnum)
				result = Enum.Parse(type, str);
			else if (type.IsArray)
				result = ArrayFromString(str, type);
			else
			{
				var parse = type.GetParseMethod();
				if (parse != null)
					result = parse.Invoke(null, new object[] { str });
				else
				{
					if (type.IsValueType && !type.IsPrimitive)
						result = StructFromString(str, type);
					else
						result = System.Convert.ChangeType(str, type);
				}
			}
			return result;
		}

		private static string ObjectToString(object obj)
		{
			if (obj == null || obj == DBNull.Value)
				return null;
			if (obj is string) 
				return obj as string;

			Type type = obj.GetType();
			type = Nullable.GetUnderlyingType(type) ?? type;

			string result = null;
			if (type == typeof(Guid))
				result= ((Guid)obj).ToString("n");
			else if (type == typeof(DateTime))
				result=((DateTime)obj).ToString();
			else if (type == typeof(Decimal))
				result=((Decimal)obj).ToString();
			else if (type.IsEnum)
				result= obj.ToString();
			else if (type.IsArray)
				result= ArrayToString(obj);
			else
			{
				var parse = type.GetParseMethod();
				if (parse != null)
					result =obj.ToString();
				else
				{
					if (type.IsValueType && !type.IsPrimitive)
						result = StructToString(obj);
					else
						result= obj.ToString();
				}
			}
			return result;
		}

		private static object StructFromString(string str, Type type)
		{
			Debug.Assert(!string.IsNullOrEmpty(str));
			Debug.Assert(type.IsValueType);

			var flds = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (flds.Length > 0)
			{
				object result = System.Activator.CreateInstance(type);
				string[] vals = str.Split(FieldSeparator);
				for (int i = 0; i < flds.Length && i < vals.Length; ++i)
				{
					object v = ObjectFromString(vals[i], flds[i].FieldType);
					flds[i].SetValue(result, v);
				}
				return result;
			}
			else
			{
				return System.Convert.ChangeType(str, type);
			}
		}

		private static string StructToString(object obj)
		{
			Debug.Assert(obj != null);
			Debug.Assert(obj.GetType().IsValueType);

			var flds = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (flds.Length > 0)
			{
				StringBuilder sb = new StringBuilder();
				foreach (var fld in flds)
				{
					sb.Append(ObjectToString(fld.GetValue(obj)));
					sb.Append(FieldSeparator);
				}
				return sb.ToString().TrimEnd(FieldSeparator);
			}
			else
			{
				return obj.ToString();
			}
		}

		private static object ArrayFromString(string str, Type type)
		{
			Debug.Assert(!string.IsNullOrEmpty(str));
			Debug.Assert(type.IsArray);

			string[] vals = str.Split(new char[] { ArraySeparator });
			var list = new System.Collections.ArrayList(vals.Length);
			var t = type.GetElementType();
			for (int i = 0; i < vals.Length; ++i)
				list.Add(ObjectFromString(vals[i], t));
			return list.ToArray(t);
		}

		private static string ArrayToString(object objs)
		{
			Debug.Assert(objs != null);
			Debug.Assert(objs.GetType().IsArray);

			StringBuilder sb = new StringBuilder();
			foreach (var obj in (Array)objs)
			{
				sb.Append(ObjectToString(obj));
				sb.Append(ArraySeparator);
			}
			return sb.ToString().TrimEnd(ArraySeparator);
		}

		#endregion Base Implement

		public static readonly char FieldSeparator = ':';
		public static readonly char ArraySeparator = ',';
	}

	static class TypeExtension
	{
		public static MethodInfo GetParseMethod(this Type type)
		{
			var ms = type.GetMember("Parse", MemberTypes.Method, BindingFlags.Static| BindingFlags.Public);
			foreach (MethodInfo m in ms)
			{
				var ps = m.GetParameters();
				if (m.ReturnType == type && ps.Length == 1 && ps[0].ParameterType == typeof(string))
					return m;
			}
			return null;
		}
	}
}
