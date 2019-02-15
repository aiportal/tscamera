using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.MiniWeb.Service
{
	using Newtonsoft.Json;

	#region TypeConverters

	abstract class TypeConverter<T> : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(T);
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null && value.GetType() == typeof(T))
				writer.WriteValue(JsonSerialize((T)value));
			else
				serializer.Serialize(writer, value);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
		public abstract string JsonSerialize(T obj);
	}

	class DateConverter : TypeConverter<DateTime> { public override string JsonSerialize(DateTime obj) { return obj.ToString("yyyy-MM-dd"); } }
	class DateTimeConverter : TypeConverter<DateTime> { public override string JsonSerialize(DateTime obj) { return obj.ToString("yyyy-MM-dd HH:mm"); } }
	class TimeConverter : TypeConverter<DateTime> { public override string JsonSerialize(DateTime obj) { return obj.ToString("HH:mm"); } }

	#endregion TypeConverters

	#region ArrayConverter

	abstract class ArrayConverter<T> : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(T[]);
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null && value.GetType() == typeof(T[]))
			{
				string array = string.Join(",", Array.ConvertAll<T, string>(value as T[], v=>v.ToString()));
				writer.WriteValue(array);
			}
			else
				serializer.Serialize(writer, value);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}

	#endregion ArrayConverter
}
