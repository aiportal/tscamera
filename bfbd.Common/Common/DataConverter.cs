using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace bfbd.Common
{
	using CV = System.Convert;

	public partial class DataConverter
	{
		private static readonly DataConverter _single = new DataConverter();

		public static object ChangeType(object obj, Type type)
		{
			if (obj == null || obj == DBNull.Value)
				return null;
			if (obj.GetType() == type)
				return obj;
			if (type == typeof(object))
				return obj;

			object result = null;
			try
			{
				if (obj is string)
				{
					if (_deserializeMethods.ContainsKey(type))
					{
						var method = _deserializeMethods[type];
						result = method.Invoke(_single, new object[] { obj as string });
					}
					else
					{
						if (type.IsEnum)
							result = Enum.Parse(type, obj as string);
						else
							result = System.Convert.ChangeType(obj, type);
					}
				}
				else if (type == typeof(string))
				{
					if (_serializeMethods.ContainsKey(obj.GetType()))
					{
						var method = _serializeMethods[obj.GetType()];
						result = method.Invoke(_single, new object[] { obj }) as string;
					}
					else
						result = obj.ToString();
				}
				else
				{
					if (type.IsEnum)
						result = Enum.ToObject(type, obj);
					else
						result = System.Convert.ChangeType(obj, type);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return result;
		}

		public static string Serialize(object obj)
		{
			return ChangeType(obj, typeof(string)) as string;
		}

		public static T Convert<T>(object val)
		{
			return (T)ChangeType(val, typeof(T));
		}

		public static T Convert<T>(DataRow row, ref T obj)
		{
			try
			{
				var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField);
				foreach (var f in fields)
				{
					var col = row.Table.Columns[f.Name];
					if (col != null)
						f.SetValue(obj, ChangeType(row[col], f.FieldType));
				}
				var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
				foreach (var p in props)
				{
					var col = row.Table.Columns[p.Name];
					if (col != null)
						p.SetValue(obj, ChangeType(row[col], p.PropertyType), null);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return obj;
		}

		public static T Convert<T>(System.Data.Common.DbDataReader reader, ref T obj)
		{
			try
			{
				var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
				foreach (var f in fields)
				{
					var col = reader.GetOrdinal(f.Name);
					if (col >= 0)
						f.SetValue(obj, ChangeType(reader.GetValue(col), f.FieldType));
				}
				var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
				foreach (var p in props)
				{
					var col = reader.GetOrdinal(p.Name);
					if (col >= 0)
						p.SetValue(obj, ChangeType(reader.GetValue(col), p.PropertyType), null);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return obj;
		}
	}

	partial class DataConverter
	{
		private static Dictionary<Type, MethodInfo> _serializeMethods = new Dictionary<Type, MethodInfo>();
		private static Dictionary<Type, MethodInfo> _deserializeMethods = new Dictionary<Type, MethodInfo>();
		static DataConverter()
		{
			var ms = typeof(DataConverter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
			var st = typeof(string);
			foreach (var m in ms)
			{
				var ps = m.GetParameters();
				if (ps.Length == 1)
				{
					var pt = ps[0].ParameterType;
					var rt = m.ReturnType;
					if (pt != st && rt == st)
						_serializeMethods[pt] = m;
					else if (pt == st && rt != st)
						_deserializeMethods[rt] = m;
				}
			}
		}

		private string Serialize(System.Drawing.Rectangle rect)
		{
			return string.Format("{0}:{1}:{2}:{3}", rect.X, rect.Y, rect.Width, rect.Height);
		}
		private System.Drawing.Rectangle Rectangle(string val)
		{
			var ss = val.Split(':');
			return new System.Drawing.Rectangle(CV.ToInt32(ss[0]), CV.ToInt32(ss[1]), CV.ToInt32(ss[2]), CV.ToInt32(ss[3]));
		}

		private string Serialize(string[] array)
		{
			return string.Join(",", array);
		}
		private string[] StringArray(string val)
		{
			return string.IsNullOrEmpty(val) ? null : val.Split(',');
		}

		private string Serialize(bfbd.UltraRecord.Core.MouseState ms)
		{
			return string.Format("{0}:{1}:{2}", (int)ms.ClickOption, ms.X, ms.Y);
		}
		private bfbd.UltraRecord.Core.MouseState MouseState(string val)
		{
			var ss = val.Split(':');
			return new bfbd.UltraRecord.Core.MouseState() { ClickOption = (bfbd.UltraRecord.Core.MouseClickOption)CV.ToInt32(ss[0]), X = CV.ToInt32(ss[1]), Y = CV.ToInt32(ss[2]) };
		}

		private string Serialize(Guid obj) { return obj.ToString("n"); }
		private Guid Guid(string val) { return new Guid(val); }
	}

	//delegate string SerializeHandler<T>(T obj);
	//delegate object DeserializeHandler(string val);
}

		//public static string _Serilize(object obj)
		//{
		//    string result = null;
		//    if (obj != null)
		//    {
		//        Type type = obj.GetType();
		//        if (type == typeof(string[]))
		//        {
		//            result = string.Join(",", obj as string[]);
		//        }
		//        else
		//        {
		//            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
		//            if (fields.Length > 0)
		//            {
		//                //Array.Sort(fields);
		//                string[] vals = Array.ConvertAll(fields,
		//                    f => System.Convert.ToString(f.GetValue(obj))
		//                );
		//                result = string.Join(":", vals);
		//            }
		//            else
		//            {
		//                result = obj.ToString();
		//            }
		//        }
		//    }
		//    return result;
		//}

		//public static object _ChangeType(object val, Type type)
		//{
		//    object result = null;
		//    if (val != null && val != DBNull.Value)
		//    {
		//        Type srcType = val.GetType();
		//        if (srcType == type)
		//        {
		//            result = val;
		//        }
		//        else if (val is string)
		//        {
		//            string str = val as string;
		//            if (type.IsEnum)
		//                result = Enum.Parse(type, str);
		//            else if (type == typeof(string[]))
		//                result = string.IsNullOrEmpty(str) ? null : str.Split(',');
		//            else if (type == typeof(Guid))
		//                result = new Guid(val as string);
		//            else
		//                result = System.Convert.ChangeType(str, type);
		//        }
		//        else
		//        {
		//            if (type.IsEnum)
		//                result = Enum.ToObject(type, val);
		//            else if (srcType == typeof(Guid) && type == typeof(string))
		//                result = val.ToString();
		//            else
		//                result = System.Convert.ChangeType(val, type);
		//        }
		//    }
		//    return result;
		//}
