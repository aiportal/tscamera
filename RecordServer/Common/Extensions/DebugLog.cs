using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.Common
{
	static partial class DebugLog
	{
		private static bool _details = false;
		private static TraceSwitch _ts = new TraceSwitch("General", "Description for General");

		static DebugLog() { _details = _ts.TraceVerbose; }

		[Conditional("DEBUG")]
		public static void Assert(bool condition) { Debug.Assert(condition); }
		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message) { Debug.Assert(condition, message); }

		[Conditional("DEBUG")]
		public static void Confirm(Func<bool> func) { Debug.Assert(func()); }
		[Conditional("DEBUG")]
		public static void Confirm(string message, Func<bool> func) { Debug.Assert(func(), message); }

		[Conditional("DEBUG")]
		public static void WriteEntracne() { WriteInfoLine(_details, FuncOpration.ENTRANCE); }
		[Conditional("DEBUG")]
		public static void WriteEntracne(bool condition) { if (condition) { WriteInfoLine(condition, FuncOpration.ENTRANCE); } }
		[Conditional("DEBUG")]
		public static void WriteEntracne(bool condition, string funcName) { if (condition) { WriteInfoLine(condition, FuncOpration.ENTRANCE, funcName); } }

		[Conditional("DEBUG")]
		public static void WriteExit() { WriteInfoLine(_details, FuncOpration.EXIT); }
		[Conditional("DEBUG")]
		public static void WriteExit(bool condition) { if (condition) { WriteInfoLine(condition, FuncOpration.EXIT); } }
		[Conditional("DEBUG")]
		public static void WriteExit(bool condition, string funcName) { if (condition) { WriteInfoLine(condition, FuncOpration.EXIT, funcName); } }

		[Conditional("DEBUG")]
		public static void WriteLineError(string msg) { WriteLineIf(_ts.TraceError, msg + " at " + GetFunctionFullName(2)); }
		[Conditional("DEBUG")]
		public static void WriteLineError(string msg, string funcName) { WriteLineIf(_ts.TraceError, msg + " at " + funcName); }

		[Conditional("DEBUG")]
		public static void WriteLineWarning(string msg) { WriteLineIf(_ts.TraceWarning, msg + " at " + GetFunctionFullName(2)); }
		[Conditional("DEBUG")]
		public static void WriteLineWarning(string msg, string funcName) { WriteLineIf(_ts.TraceWarning, msg + " at " + funcName); }

		[Conditional("DEBUG")]
		public static void WriteLineInfo(string msg) { WriteLineIf(_ts.TraceInfo, msg + " at " + GetFunctionFullName(2)); }
		[Conditional("DEBUG")]
		public static void WriteLineInfo(string msg, string funcName) { WriteLineIf(_ts.TraceInfo, msg + " at " + funcName); }

		[Conditional("DEBUG")]
		public static void WriteLineVerbos(string msg) { WriteLineIf(_ts.TraceVerbose, msg + " at " + GetFunctionFullName(2)); }
		[Conditional("DEBUG")]
		public static void WriteLineVerbos(string msg, string funcName) { WriteLineIf(_ts.TraceVerbose, msg + " at " + funcName); }

		[Conditional("DEBUG")]
		public static void WriteLine(string msg) { try { Trace.WriteLineIf(true, msg); } catch (Exception) { } }
		[Conditional("DEBUG")]
		public static void WriteLineIf(bool condition, string msg) { try { Trace.WriteLineIf(condition, msg); } catch (Exception) { } }

		[Conditional("DEBUG")]
		public static void WriteException(Exception ex)
		{
			try
			{
				string funcName = GetFunctionFullName(2);
				WriteLineIf(true, string.Format("Exception at {0}, {1}, {2}\r\n{3}", funcName, ex.Message, ex.Source, ex.StackTrace));
			}
			catch (Exception) { }
		}
		[Conditional("DEBUG")]
		public static void WriteException(Exception ex, string funcName)
		{
			try
			{
				WriteLineIf(true, string.Format("Exception at {0}, {1}", funcName, ex.Message + ex.StackTrace));
			}
			catch (Exception) { }
		}
	}

	partial class DebugLog
	{
		#region Extension

		[Conditional("DEBUG")]
		public static void Assert<T>(this T obj, bool condition) { Debug.Assert(condition, typeof(T).FullName); }
		[Conditional("DEBUG")]
		public static void Assert<T>(this T obj, bool condition, string message) { Debug.Assert(condition, typeof(T).FullName, message); }

		[Conditional("DEBUG")]
		public static void Confirm<T>(this T obj, Func<T, bool> func) { Debug.Assert(func(obj)); }
		[Conditional("DEBUG")]
		public static void Confirm<T>(this T obj, string message, Func<T, bool> func) { Debug.Assert(func(obj), message); }

		#endregion Extension
	}

	partial class DebugLog
	{
		#region Inner Implement

		private static string GetFunctionFullName(int skipFrames)
		{
			string str = string.Empty;
			try
			{
				StackFrame frame = new StackFrame(skipFrames);
				var method = frame.GetMethod();
				str = string.Format("{0}::{1}", method.Name, method.DeclaringType.Name);
			}
			catch (Exception) { }
			return str;
		}

		private static void WriteInfoLine(bool condition, FuncOpration funcOp, string funcName = null)
		{
			if (!condition)
				return;
			try
			{
				if (string.IsNullOrEmpty(funcName))
					funcName = GetFunctionFullName(3);
				string oper = string.Empty;
				switch (funcOp)
				{
					case FuncOpration.ENTRANCE:
						oper = "Enter ";
						break;

					case FuncOpration.EXIT:
						oper = "Exit ";
						break;
				}
				Trace.WriteLineIf(condition, oper + funcName);
			}
			catch (Exception) { }
		}

		enum FuncOpration
		{
			ENTRANCE = 1,
			EXIT = 2
		}

		#endregion Inner Implement
	}
}
