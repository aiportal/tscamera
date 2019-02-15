using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace bfbd.Common.Data
{
	abstract partial class DatabaseEngine
	{
		private static Hashtable _settings = Hashtable.Synchronized(new Hashtable());

		static DatabaseEngine()
		{
			foreach (ConnectionStringSettings setting in ConfigurationManager.ConnectionStrings)
			{
				if (!setting.ElementInformation.IsPresent)
					continue;
				if (System.IO.Path.GetFileName(setting.ElementInformation.Source) == "machine.config")
					continue;
				_settings[setting.Name] = setting;
			}
		}

		public static void AddConnectionSettings(string name, string connectionString, string providerName)
		{
			_settings[name] = new ConnectionStringSettings(name, connectionString, providerName);
		}
	}

	partial class DatabaseEngine : IDisposable
	{
		private const string _defaultPassword = @"12345678";
		private string _connStr;
		private DbProviderFactory _dbFactory;
		private DbConnection _conn;

		public DatabaseEngine(string connName = null)
		{
			if (_settings.Count < 1)
				throw new ConfigurationErrorsException("Can't find ConnectionStrings setting.");

			ConnectionStringSettings setting = null;
			if (!string.IsNullOrEmpty(connName))
				setting = _settings[connName] as ConnectionStringSettings;
			else
			{
				var enumerator = _settings.Values.GetEnumerator();
				if (enumerator.MoveNext())
					setting = enumerator.Current as ConnectionStringSettings;
			}

			_connStr = setting.ConnectionString.Replace("{0}", _defaultPassword);
			if (setting.ProviderName == "System.Data.SQLite")
				_dbFactory = new System.Data.SQLite.SQLiteFactory();
			else
				_dbFactory = DbProviderFactories.GetFactory(setting.ProviderName);
		}

		protected DbConnection Connection
		{
			get
			{
				if (_conn == null)
				{
					_conn = _dbFactory.CreateConnection();
					_conn.ConnectionString = _connStr;
					_conn.Open();
				}
				return _conn;
			}
		}

		public void Dispose()
		{
			if (_conn != null)
			{
				_conn.Close();
				_conn.Dispose();
				_conn = null;
			}
		}

		#region Invoke Implement

		protected object InvokeSingleQuery(string sql, params object[] prams)
		{
			try
			{
				object result = null;
				using (DbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandType = CommandType.Text;
					this.BindParameters(cmd, prams);
					result = cmd.ExecuteScalar();
					if (result == DBNull.Value)
						result = null;
				}
				return result;
			}
			catch (Exception ex)
			{
				this.DumpException(ex, sql, prams);
				throw;
			}
		}

		protected DataTable InvokeTableQuery(string sql, params object[] prams)
		{
			try
			{
				DataTable dt = new DataTable();
				using (DbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandType = CommandType.Text;
					this.BindParameters(cmd, prams);
					using (DbDataAdapter adp = _dbFactory.CreateDataAdapter())
					{
						adp.SelectCommand = cmd;
						adp.Fill(dt);
					}
				}
				return dt;
			}
			catch (Exception ex)
			{
				this.DumpException(ex, sql, prams);
				throw;
			}
		}

		protected DbDataReader InvokeReadQuery(string sql, params object[] prams)
		{
			try
			{
				DataTable dt = new DataTable();
				using (DbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandType = CommandType.Text;
					this.BindParameters(cmd, prams);
					return cmd.ExecuteReader();
				}
			}
			catch (Exception ex)
			{
				this.DumpException(ex, sql, prams);
				throw;
			}
		}

		protected int InvokeExecuteQuery(string sql, params object[] prams)
		{
			try
			{
				int count = 0;
				using (DbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandType = CommandType.Text;
					this.BindParameters(cmd, prams);
					count = cmd.ExecuteNonQuery();
				}
				return count;
			}
			catch (Exception ex)
			{
				this.DumpException(ex, sql, prams);
				throw;
			}
		}

		private void BindParameters(DbCommand cmd, params object[] pramsArray)
		{
			if (pramsArray != null)
			{
				foreach (object prams in pramsArray)
				{
					if (prams == null || prams is string)
						continue;

					foreach (var prop in prams.GetType().GetProperties())
					{
						DbParameter p = cmd.CreateParameter();
						p.ParameterName = prop.Name;
						p.Value = prop.GetValue(prams, null);
						if (prop.PropertyType.IsArray)
							p.Value = DataConverter.Convert(p.Value);
						if (p.Value != null)
							cmd.Parameters.Add(p);
					}
				}
			}
		}

		private void DumpException(Exception ex, string sql, params object[] pramsArray)
		{
			StringBuilder sb = new StringBuilder();
			if (pramsArray != null)
			{
				foreach (object prams in pramsArray)
				{
					if (prams != null)
					{
						System.Reflection.PropertyInfo[] props = prams.GetType().GetProperties();
						foreach (var p in props)
							sb.AppendFormat("{0} = {1},", p.Name, p.GetValue(prams, null));
					}
				}
			}
			TraceLog.WriteLineError(string.Format(
@"Exception at DatabaseEngine.
sql : {0}
parameters : {1}
exception : {2}", sql, sb.ToString(), ex.Message));
			TraceLog.WriteException(ex);
		}

		#endregion Invoke Implement

		#region SQL Generator

		protected static string MakeSelect(string tableName, string condition, string[] columns, int group = 0, string order = null)
		{
			return MakeSelect(tableName, condition as object, columns, group, order as object);
		}

		protected static string MakeSelect(string tableName, object condition, string[] columns, int group = 0, object order = null)
		{
			string sql;
			string filter = MakeFilter(condition);
			string group_by = MakeGroup(columns, group);
			string order_by = MakeOrder(order);
			sql = string.Format("SELECT {0} FROM [{1}] {2} {3} {4}", string.Join(",", columns), tableName, filter, group_by, order_by);
			return sql;
		}

		protected static string MakeSelect(string tableName, object condition, string[] columns, int limit, int offset)
		{
			string sql;
			string filter = MakeFilter(condition);
			string row_count = limit > 0 ? string.Format("LIMIT {0} OFFSET {1}", limit, offset) : null;
			sql = string.Format("SELECT {0} FROM [{1}] {2} {3}", string.Join(",", columns), tableName, filter, row_count);
			return sql;
		}

		protected static string MakeInsert(string tableName, object prams)
		{
			string sql;
			StringBuilder cols = new StringBuilder();
			StringBuilder vals = new StringBuilder();
			{
				var props = prams.GetType().GetProperties();
				foreach (var p in props)
				{
					if (Attribute.IsDefined(p, typeof(InsertIgnore)))
						continue;

					if (p.CanRead && p.GetValue(prams, null) != null)
					{
						cols.Append(cols.Length > 0 ? "," : null).Append(p.Name);
						vals.Append(vals.Length > 0 ? "," : null).Append("@").Append(p.Name);
					}
				};
			}
			sql = string.Format("INSERT INTO [{0}] ({1}) VALUES({2})", tableName, cols, vals);
			return sql;
		}

		protected static string MakeUpdate(string tableName, object prams, string condition)
		{
			return MakeUpdate(tableName, prams, condition as object);
		}

		protected static string MakeUpdate(string tableName, object prams, object condition)
		{
			string sql;
			string filter = MakeFilter(condition);
			StringBuilder vals = new StringBuilder();
			{
				var props = prams.GetType().GetProperties();
				foreach (var p in props)
				{
					if (Attribute.IsDefined(p, typeof(UpdateIgnore)))
						continue;

					if (p.CanRead)
					{
						vals.Append(vals.Length > 0 ? "," : null);
						if (p.GetValue(prams, null) != null)
							vals.Append(p.Name).Append("=@").Append(p.Name);
						else
							vals.Append(p.Name).Append("=NULL");
					}
				}
			}
			sql = string.Format("UPDATE [{0}] SET {1} {2}", tableName, vals, filter);
			return sql;
		}

		protected static string MakeDelete(string tableName, string condition)
		{
			return MakeDelete(tableName, condition as object);
		}

		protected static string MakeDelete(string tableName, object condition)
		{
			string sql;
			string filter = MakeFilter(condition);
			sql = string.Format("DELETE FROM [{0}] {1}", tableName, filter);
			return sql;
		}

		protected static string MakeFilter(object condition)
		{
			string filter = string.Empty;
			if (condition != null)
			{
				if (condition is string)
				{
					filter = string.IsNullOrEmpty(condition as string) ? "" : " WHERE " + condition;
				}
				else
				{
					StringBuilder vals = new StringBuilder();
					var props = condition.GetType().GetProperties();
					foreach (var p in props)
					{
						if (p.CanRead)
						{
							vals.Append(vals.Length > 0 ? " AND " : null);
							if (p.GetValue(condition, null) != null)
								vals.Append(p.Name).Append("=@").Append(p.Name);
							else
								vals.Append(p.Name).Append(" IS NULL");
						}
					};
					if (vals.Length > 0)
						filter = vals.Insert(0, " WHERE ").Append(" ").ToString();
				}
			}
			return filter;
		}

		private static string MakeOrder(object order)
		{
			string sql = string.Empty;
			if (order != null)
			{
				if (order is string)
				{
					sql = string.IsNullOrEmpty(order as string) ? "" : "ORDER BY " + order;
				}
				else
				{
					StringBuilder cols = new StringBuilder();
					var props = order.GetType().GetProperties();
					foreach (var p in props)
					{
						string val = p.GetValue(order, null) as string;
						if (string.Equals(val, "DESC", StringComparison.OrdinalIgnoreCase))
							cols.Append(p.Name).Append(" DESC");
						else
							cols.Append(p.Name);
					};
					if (cols.Length > 0)
						sql = cols.Insert(0, " ORDER BY ").Append(" ").ToString();
				}
			}
			return sql;
		}

		private static string MakeGroup(string[] columns, int group)
		{
			string sql = string.Empty;
			if (group > 0 && group <= columns.Length)
			{
				StringBuilder cols = new StringBuilder();
				for (int i = 0; i < group; ++i)
				{
					cols.Append(cols.Length > 0? "," : null);
					var c = columns[i];
					if (c.ToLower().Contains(" as "))
						cols.Append(c.Substring(c.ToLower().LastIndexOf(" as ") + 4));
					else if (c.Contains(" "))
						cols.Append(c.Substring(c.LastIndexOf(" ") + 1));
					else
						cols.Append(c);
				}
				sql = cols.Insert(0, " GROUP BY ").Append(" ").ToString();
			}
			return sql;
		}

		#endregion SQL Generator
	}

	[AttributeUsage(AttributeTargets.Property)]
	class InsertIgnore : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	class UpdateIgnore : Attribute { }

	//[AttributeUsage(AttributeTargets.Property)]
	//class DbColumnAttribute : Attribute
	//{
	//    public string Name { get; private set; }
	//    public DbColumnAttribute(string name) { Name = name; }
	//}

	//[AttributeUsage(AttributeTargets.Property)]
	//class DbTypeAttribute : Attribute
	//{
	//    public Type Type { get; private set; }
	//    public DbTypeAttribute(Type type) { Type = type; }
	//}
}