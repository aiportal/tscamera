using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common
{
	#region Static Functions

	partial class Database
	{
		/// <summary>
		/// throw Exception when error.
		/// </summary>
		public static T Invoke<T>(DatabaseFunc<Database, T> func)
		{
			T result;
			try
			{
				using (Database db = new Database())
				{
					result = func(db);
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				throw;
			}
			return result;
		}

		/// <summary>
		/// throw Exception when error.
		/// </summary>
		public static T Invoke<T>(string name, DatabaseFunc<Database, T> func)
		{
			T result;
			try
			{
				using (Database db = new Database(name))
				{
					result = func(db);
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				throw;
			}
			return result;
		}

		/// <summary>
		/// Silence when error occur.
		/// </summary>
		public static T Execute<T>(DatabaseFunc<Database, T> func)
		{
			T result = default(T);
			try
			{
				using (Database db = new Database())
				{
					result = func(db);
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				//throw;
			}
			return result;
		}

		/// <summary>
		/// Silence when error occur.
		/// </summary>
		public static T Execute<T>(string name, DatabaseFunc<Database, T> func)
		{
			T result = default(T);
			try
			{
				using (Database db = new Database(name))
				{
					result = func(db);
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				//throw;
			}
			return result;
		}
	}

	delegate R DatabaseFunc<T, R>(T p);

	#endregion Static Functions
}
