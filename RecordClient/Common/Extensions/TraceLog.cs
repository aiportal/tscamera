using System;using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace bfbd.Common
{
	static partial class TraceLog
	{
		private static bool _details = false;
		private static TraceSwitch _ts = new TraceSwitch("General", "Description for General");

		static TraceLog() { _details = _ts.TraceVerbose; }

		public static void WriteEntracne() { WriteInfoLine(_details, FuncOpration.ENTRANCE); }
		public static void WriteEntracne(bool condition) { if (condition) { WriteInfoLine(condition, FuncOpration.ENTRANCE); } }
		public static void WriteEntracne(bool condition, string funcName) { if (condition) { WriteInfoLine(condition, FuncOpration.ENTRANCE, funcName); } }

		public static void WriteExit() { WriteInfoLine(_details, FuncOpration.EXIT); }
		public static void WriteExit(bool condition) { if (condition) { WriteInfoLine(condition, FuncOpration.EXIT); } }
		public static void WriteExit(bool condition, string funcName) { if (condition) { WriteInfoLine(condition, FuncOpration.EXIT, funcName); } }

		public static void WriteLineError(string msg) { WriteLineIf(_ts.TraceError, msg + " at " + GetFunctionFullName(2)); }
		public static void WriteLineError(string msg, string funcName) { WriteLineIf(_ts.TraceError, msg + " at " + funcName); }

		public static void WriteLineWarning(string msg) { WriteLineIf(_ts.TraceWarning, msg + " at " + GetFunctionFullName(2)); }
		public static void WriteLineWarning(string msg, string funcName) { WriteLineIf(_ts.TraceWarning, msg + " at " + funcName); }

		public static void WriteLineInfo(string msg) { WriteLineIf(_ts.TraceInfo, msg + " at " + GetFunctionFullName(2)); }
		public static void WriteLineInfo(string msg, string funcName) { WriteLineIf(_ts.TraceInfo, msg + " at " + funcName); }

		public static void WriteLineVerbos(string msg) { WriteLineIf(_ts.TraceVerbose, msg + " at " + GetFunctionFullName(2)); }
		public static void WriteLineVerbos(string msg, string funcName) { WriteLineIf(_ts.TraceVerbose, msg + " at " + funcName); }

		public static void WriteLine(string msg) { try { Trace.WriteLineIf(true, msg); } catch (Exception) { } }
		public static void WriteLineIf(bool condition, string msg) { try { Trace.WriteLineIf(condition, msg); } catch (Exception) { } }

		public static void WriteException(Exception ex)
		{
			try
			{
				string funcName = GetFunctionFullName(2);
				WriteLineIf(true, string.Format("Exception at {0}, {1}, {2}\r\n{3}", funcName, ex.Message, ex.Source, ex.StackTrace));
			}
			catch (Exception) { }
		}
		public static void WriteException(Exception ex, string funcName)
		{
			try
			{
				WriteLineIf(true, string.Format("Exception at {0}, {1}", funcName, ex.Message + ex.StackTrace));
			}
			catch (Exception) { }
		}
	}

	partial class TraceLog
	{
		#region Extension
		public static void TraceEntracne(this object obj) { WriteInfoLine(_details, FuncOpration.ENTRANCE); }
		public static void TraceEntracne(this object obj, bool condition) { WriteInfoLine(condition, FuncOpration.ENTRANCE); }
		public static void TraceEntracne(this object obj, bool condition, string funcName) { WriteInfoLine(condition, FuncOpration.ENTRANCE, funcName); }

		public static void TraceExit(this object obj) { WriteInfoLine(_details, FuncOpration.EXIT); }
		public static void TraceExit(this object obj, bool condition) { WriteInfoLine(condition, FuncOpration.EXIT); }
		public static void TraceExit(this object obj, bool condition, string funcName) { WriteInfoLine(condition, FuncOpration.EXIT, funcName); }

		public static void TraceException(this object obj, Exception ex)
		{
			try
			{
				string funcName = GetFunctionFullName(2);
				WriteLineIf(true, string.Format("Exception at {0}, {1}, {2}\r\n{3}", funcName, ex.Message, ex.Source, ex.StackTrace));
			}
			catch (Exception) { }
		}
		public static void TraceException(this object obj, Exception ex, string funcName)
		{
			try
			{
				WriteLineIf(true, string.Format("Exception at {0}, {1}", funcName, ex.Message + ex.StackTrace));
			}
			catch (Exception) { }
		}
		#endregion Extension
	}

	partial class TraceLog
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
