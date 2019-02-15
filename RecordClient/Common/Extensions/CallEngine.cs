using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.Common
{
	static partial class Call
	{
		public static void Invoke(Action action)
		{
			try { action(); }
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}
		public static R Invoke<R>(Func<R> func)
		{
			try { return func(); }
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public static void Execute(Action action)
		{
			try { action(); }
			catch (Exception ex) { TraceLog.WriteException(ex); }
		}
		public static R Execute<R>(Func<R> func, R defaultResult = default(R))
		{
			try { return func(); }
			catch (Exception ex)
			{
				TraceLog.WriteException(ex);
				return defaultResult;
			}
		}
	}
}
