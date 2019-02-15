using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace bfbd.Common
{
	using bfbd.Common.Data;

	#region Base Functions

	partial class Database : DatabaseEngine
	{
		public Database() : base() { }
		public Database(string name) : base(name) { }

		public bool IsExist(string tableName, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { "count(*)" });
			object count = InvokeSingleQuery(sql, condition);
			return Convert.ToInt64(count) > 0;
		}

		public object SelectSingle(string tableName, string columnName, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { columnName });
			return InvokeSingleQuery(sql, condition);
		}

		public DataRow SelectRow(string tableName, object condition, params string[] columns)
		{
			string sql = MakeSelect(tableName, condition, columns);
			DataTable dt = InvokeTableQuery(sql, condition);
			return (dt.Rows.Count > 0) ? dt.Rows[0] : null;
		}

		public DataTable SelectTable(string tableName, object condition, params string[] columns)
		{
			string sql = MakeSelect(tableName, condition, columns);
			return InvokeTableQuery(sql, condition);
		}

		public DataTable SelectTable(string tableName, string condition, params string[] columns)
		{
			string sql = MakeSelect(tableName, condition, columns);
			return InvokeTableQuery(sql, condition);
		}

		public DataTable SelectTable(string tableName, object condition, object order, params string[] columns)
		{
			string sql = MakeSelect(tableName, condition, columns, 0, order);
			return InvokeTableQuery(sql, condition);
		}

		public DataTable SelectTable(string tableName, string condition, string order, int group, params string[] columns)
		{
			Debug.Assert(group < columns.Length);
			string sql = MakeSelect(tableName, condition, columns, group, order);
			return InvokeTableQuery(sql, condition);
		}

		public int Insert(string tableName, object prams)
		{
			string sql = MakeInsert(tableName, prams);
			return InvokeExecuteQuery(sql, prams);
		}

		public int Update(string tableName, object prams, object condition)
		{
			string sql = MakeUpdate(tableName, prams, condition);
			return InvokeExecuteQuery(sql, prams, condition);
		}

		public int Delete(string tableName, object condition)
		{
			string sql = MakeDelete(tableName, condition);
			return InvokeExecuteQuery(sql, condition);
		}

		public int InsertOrUpdate(string tableName, object prams, object condition)
		{
			string sql = IsExist(tableName, condition) ? MakeUpdate(tableName, prams, condition) : MakeInsert(tableName, prams);
			return InvokeExecuteQuery(sql, prams, condition);
		}

		[Obsolete]
		public int InsertDistinct(string tableName, object prams, object condition = null)
		{
			condition = condition == null? prams : condition;
			return IsExist(tableName, condition) ? 0 : Insert(tableName, prams);
		}
	}

	#endregion Base Functions

	partial class Database
	{
		public T SelectSingle<T>(string tableName, string columnName, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { columnName });
			var val = InvokeSingleQuery(sql, condition);
			return DataConverter.Convert<T>(val);
		}

		public T SelectObject<T>(string tableName, object condition, params string[] columns)
			where T : new()
		{
			string sql = MakeSelect(tableName, condition, columns);
			DataTable dt = InvokeTableQuery(sql, condition);
			T obj = new T();
			return (dt.Rows.Count > 0) ? DataConverter.Convert<T>(dt.Rows[0], ref obj) : default(T);
		}

		public T[] SelectArray<T>(string tableName, string columnName, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { columnName });
			DataTable dt = InvokeTableQuery(sql, condition);
			//return dt.Rows.Cast<DataRow>().Select(r => r[0]).Cast<T>().ToArray();
			List<T> array = new List<T>();
			foreach (DataRow r in dt.Rows)
				array.Add(DataConverter.Convert<T>(r[0]));
			return array.ToArray();
		}

		public Dictionary<K, V> SelectDictionary<K, V>(string tableName, string keyColumn, string valueColumn, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { keyColumn, valueColumn });
			DataTable dt = InvokeTableQuery(sql, condition);
			Dictionary<K, V> dic = new Dictionary<K, V>();
			foreach (DataRow r in dt.Rows)
			{
				K key = DataConverter.Convert<K>(r[0]);
				V val = DataConverter.Convert<V>(r[1]);
				dic[key] = val;
			}
			return dic;
		}

		public Dictionary<K, T> SelectDictionary<K, T>(string tableName, object condition, string keyColumn, params string[] valueColumns)
			where T : new()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, string condition, params string[] columns)
			where T : new()
		{
			string sql = MakeSelect(tableName, condition, columns);
			DataTable dt = InvokeTableQuery(sql, condition);
			T obj = new T();
			foreach (DataRow row in dt.Rows)
				yield return DataConverter.Convert<T>(row, ref obj);
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, object condition, params string[] columns)
			where T: new()
		{
			string sql = MakeSelect(tableName, condition, columns);
			DataTable dt = InvokeTableQuery(sql, condition);
			T obj = new T();
			foreach (DataRow row in dt.Rows)
				yield return DataConverter.Convert<T>(row, ref obj);
		}

		//public IEnumerable<T> SelectObjects<T>(string tableName, string condition, int order, params string[] columns)
		//    where T : new()
		//{
		//    Debug.Assert(order < columns.Length);
		//    string sql = MakeSelect(tableName, condition, columns, 0, string.Join(",", columns, 0, order));
		//    DataTable dt = InvokeTableQuery(sql, condition);
		//    foreach (DataRow row in dt.Rows)
		//        yield return DbConverter.Convert<T>(row);
		//}

		public IEnumerable<T> ReadObjects<T>(string tableName, object condition, string order, int group, params string[] columns)
			where T : new()
		{
			Debug.Assert(group < columns.Length);
			T obj = new T();
			string sql = MakeSelect(tableName, condition, columns, group, order);
			DbDataReader rdr = InvokeReaderQuery(sql, condition);
			while (rdr.Read())
				yield return DataConverter.Convert<T>(rdr, ref obj);
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, string condition, string order, int group, params string[] columns)
			where T : new()
		{
			Debug.Assert(group < columns.Length);
			string sql = MakeSelect(tableName, condition, columns, group, order);
			DataTable dt = InvokeTableQuery(sql, condition);
			T obj = new T();
			foreach (DataRow row in dt.Rows)
				yield return DataConverter.Convert<T>(row, ref obj);
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, object condition, string order, int group, params string[] columns)
			where T: new()
		{
			Debug.Assert(group < columns.Length);
			string sql = MakeSelect(tableName, condition, columns, group, order);
			DataTable dt = InvokeTableQuery(sql, condition);
			T obj = new T();
			foreach (DataRow row in dt.Rows)
				yield return DataConverter.Convert<T>(row, ref obj);
		}
	}
}