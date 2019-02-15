using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.Common;

namespace bfbd.Common.Data
{
	partial class DatabaseEngine
	{
		private static Hashtable _settings = Hashtable.Synchronized(new Hashtable(1));

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

		public DatabaseEngine()
		{
			var _settingsEnumerator = _settings.GetEnumerator();
			if (_settingsEnumerator.MoveNext())
			{
				var setting = (ConnectionStringSettings)_settingsEnumerator.Value;
				_connStr = setting.ConnectionString.Replace("{0}", _defaultPassword);
				if (setting.ProviderName == "System.Data.SQLite")
					_dbFactory = new System.Data.SQLite.SQLiteFactory();
				else
					_dbFactory = DbProviderFactories.GetFactory(setting.ProviderName);
			}
			else
			{
				throw new ConfigurationErrorsException("Can't find ConnectionStrings setting.");
			}
		}

		public DatabaseEngine(string connectionName)
		{
			ConnectionStringSettings setting = (ConnectionStringSettings)_settings[connectionName];
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

		protected DbDataReader InvokeReaderQuery(string sql, params object[] prams)
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

					foreach (System.Reflection.PropertyInfo prop in prams.GetType().GetProperties())
					{
						DbParameter p = cmd.CreateParameter();
						p.ParameterName = prop.Name;
						p.Value = prop.GetValue(prams, null);
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
						//props.ToList().ForEach(p => sb.AppendFormat("{0} = {1},", p.Name, p.GetValue(prams, null)));
						foreach (var p in props)
							sb.AppendFormat("{0} = {1},", p.Name, p.GetValue(prams, null));
					}
				}
			}
			TraceLogger.Instance.WriteLineError(string.Format(
@"Exception at DatabaseEngine.
sql : {0}
parameters : {1}
exception : {2}", sql, sb.ToString(), ex.Message));
			TraceLogger.Instance.WriteException(ex);
		}
	}

	partial class DatabaseEngine
	{
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

		protected static string MakeInsert(string tableName, object prams)
		{
			string sql;
			List<string> cols = new List<string>();
			List<string> vals = new List<string>();
			{
				System.Reflection.PropertyInfo[] props = prams.GetType().GetProperties();
				foreach(var p in props)
				{
					if (p.GetValue(prams, null) != null)
					{
						cols.Add(p.Name);
						vals.Add("@" + p.Name);
					}
				};
			}
			sql = string.Format("INSERT INTO [{0}] ({1}) VALUES({2})", tableName, string.Join(",", cols.ToArray()), string.Join(",", vals.ToArray()));
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
			List<string> vals = new List<string>();
			{
				System.Reflection.PropertyInfo[] props = prams.GetType().GetProperties();
				//props.Where(p => p.GetValue(prams, null) != null).ToList().ForEach(p => vals.Add(p.Name + "=@" + p.Name));
				foreach (var p in props)
				{
					if (p.GetValue(prams, null) != null)
						vals.Add(p.Name + "=@" + p.Name);
				}
			}
			sql = string.Format("UPDATE [{0}] SET {1} {2}", tableName, string.Join(",", vals.ToArray()), filter);
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

		private static string MakeFilter(object condition)
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
					List<string> vals = new List<string>();
					System.Reflection.PropertyInfo[] props = condition.GetType().GetProperties();
					//props.ToList().ForEach(p =>
					foreach(var p in props)
					{
						vals.Add((p.GetValue(condition, null) != null) ? (p.Name + "=@" + p.Name) : (p.Name + " IS NULL"));
					};
					if (vals.Count > 0)
						filter = " WHERE " + string.Join(" AND ", vals.ToArray());
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
					List<string> vals = new List<string>();
					System.Reflection.PropertyInfo[] props = order.GetType().GetProperties();
					foreach(var p in props)
					{
						string val = p.GetValue(order, null) as string;
						if (string.Equals(val, "DESC", StringComparison.OrdinalIgnoreCase))
							vals.Add(p.Name + " DESC");
						else
							vals.Add(p.Name);
					};
					if (vals.Count > 0)
					    sql = " ORDER BY " +string.Join(",", vals.ToArray());
				}
			}
			return sql;
		}

		private static string MakeGroup(string[] columns, int group)
		{
			string sql = string.Empty;
			if (group > 0 && group <= columns.Length)
			{
				List<string> cols = new List<string>();
				for (int i = 0; i < group; ++i)
				{
					var c = columns[i];
					if (c.ToLower().Contains(" as "))
						cols.Add(c.Substring(c.ToLower().LastIndexOf(" as ") + 4));
					else if (c.Contains(" "))
						cols.Add(c.Substring(c.LastIndexOf(" ") + 1));
					else
						cols.Add(c);
				}
				sql = "GROUP BY " + string.Join(",", cols.ToArray());
			}
			return sql;
		}
	}
}