using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace bfbd.Common.Data
{
	partial class Database : DatabaseEngine
	{
		public Database() : base() { }
		public Database(string dbName) : base(dbName) { }

		#region Base Functions

		public bool IsExist(string tableName, object condition)
		{
			string sql = MakeSelect(tableName, condition, new string[] { "count(*) > 0" });
			object exist = InvokeSingleQuery(sql, condition);
			return Convert.ToBoolean(exist);
		}

		#region Select

		public object SelectSingle(string tableName, object condition, string columnName)
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

		#endregion Select

		#region Operate

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

		public int InsertDistinct(string tableName, object prams, object condition = null)
		{
			condition = condition == null? prams : condition;
			return IsExist(tableName, condition) ? 0 : Insert(tableName, prams);
		}

		#endregion Operate

		#endregion Base Functions
	}

	partial class Database
	{
		#region Extend Selectors

		public T SelectSingle<T>(string tableName, object condition, string columnName)
		{
			string sql = MakeSelect(tableName, condition, new string[] { columnName });
			var val = InvokeSingleQuery(sql, condition);
			return DataConverter.Convert<T>(val);
		}

		public T SelectObject<T>(string tableName, object condition, params string[] columns)
		{
			string sql = MakeSelect(tableName, condition, columns);
			using (DataTable dt = InvokeTableQuery(sql, condition))
			{
				return (dt.Rows.Count > 0) ? DataConverter.Convert<T>(dt.Rows[0], null) : default(T);
			}
		}

		public IEnumerable<T> SelectValues<T>(string tableName, object condition, string columnName)
		{
			string sql = MakeSelect(tableName, condition, new string[] { columnName });
			using (DbDataReader dr = InvokeReadQuery(sql, condition))
			{
				while (dr.Read())
					yield return DataConverter.Convert<T>(dr[0]);
			}
		}

		public Dictionary<K, V> SelectDictionary<K, V>(string tableName, object condition, string keyColumn, string valueColumn)
		{
			var dic = new Dictionary<K, V>();
			string sql = MakeSelect(tableName, condition, new string[] { keyColumn, valueColumn });
			using (DbDataReader dr = InvokeReadQuery(sql, condition))
			{
				while (dr.Read())
					dic[DataConverter.Convert<K>(dr[0])] = DataConverter.Convert<V>(dr[1]);
			}
			return dic;
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, string condition, params string[] columns)
		{
			return SelectObjects<T>(tableName, condition as object, columns);
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, object condition, params string[] columns)
		{
			Debug.Assert(columns.Length > 0);

			object obj = System.Activator.CreateInstance<T>();
			string sql = MakeSelect(tableName, condition, columns);
			using (DbDataReader dr = InvokeReadQuery(sql, condition, condition is string ? null : condition))
			{
				while (dr.Read())
					yield return DataConverter.Convert<T>(dr, obj);
			}
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, string condition, string order, int group, params string[] columns)
		{
			return SelectObjects<T>(tableName, condition as object, order, group, columns);
		}

		public IEnumerable<T> SelectObjects<T>(string tableName, object condition, string order, int group, params string[] columns)
		{
			Debug.Assert(group < columns.Length);
			
			object obj = System.Activator.CreateInstance<T>();
			string sql = MakeSelect(tableName, condition, columns, group, order);
			using (DbDataReader dr = InvokeReadQuery(sql, condition is string ? null : condition))
			{
				while (dr.Read())
					yield return DataConverter.Convert<T>(dr, obj);
			}
		}

		#endregion Extend Selectors
	}
}