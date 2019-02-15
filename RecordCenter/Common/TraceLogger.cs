using System;
using System.Diagnostics;

namespace bfbd
{
    class TraceLogger
    {
        private static bool _details;
        private static TraceLogger _instance;
        private static object _async = new object();
        private TraceSwitch _ts = new TraceSwitch("General", "Description for General");

        private TraceLogger()
        {
            _details = this._ts.TraceVerbose;
        }

        private string GetFunctionFullName(int aSkipFrames)
        {
            string str = string.Empty;
            try
            {
                StackFrame frame = new StackFrame(aSkipFrames);
                StackTrace trace = new StackTrace(frame);
                string name = frame.GetMethod().Name;
                string[] strArray = trace.ToString().Split(new char[] { '.' });
                string str3 = strArray[strArray.Length - 2];
                str = string.Format("{0}::{1}", name, str3);
            }
            catch (Exception)
            {
            }
            return str;
        }

        public void WriteEntracne()
        {
            this.WriteInfoLine(_details, FuncOpration.ENTRANCE);
        }

        public void WriteEntracne(bool aCondition)
        {
            if (aCondition)
            {
                this.WriteInfoLine(aCondition, FuncOpration.ENTRANCE);
            }
        }

        public void WriteException(Exception ex)
        {
			try
			{
				string functionFullName = this.GetFunctionFullName(2);
				this.WriteLineIf(true, string.Format("Exception at {0}, {1}, {2}\r\n{3}", functionFullName, ex.Message, ex.Source, ex.StackTrace));
			}
			catch (Exception) { }
        }

		public void WriteException(Exception ex, string functionName)
		{
			try
			{
				this.WriteLineIf(true, string.Format("Exception at {0}, {1}", functionName, ex.Message + ex.StackTrace));
			}
			catch (Exception) { }
		}

        public void WriteExit()
        {
            this.WriteInfoLine(_details, FuncOpration.EXIT);
        }

        public void WriteExit(bool aCondition)
        {
            if (aCondition)
            {
                this.WriteInfoLine(aCondition, FuncOpration.EXIT);
            }
        }

        private void WriteInfoLine(bool aCondition, FuncOpration funcOp)
        {
            try
            {
                string functionFullName = this.GetFunctionFullName(3);
                string str2 = string.Empty;
                switch (funcOp)
                {
                    case FuncOpration.ENTRANCE:
                        str2 = "Enter ";
                        break;

                    case FuncOpration.EXIT:
                        str2 = "Exit ";
                        break;
                }
                this.WriteLineIf(aCondition, str2 + functionFullName);
            }
            catch (Exception)
            {
            }
        }

        public void WriteLine(string aMsg)
        {
            try
            {
                Trace.WriteLineIf(true, aMsg);
            }
            catch (Exception)
            {
            }
        }

        public void WriteLineError(string aMsg)
        {
            this.WriteLineError(aMsg, this.GetFunctionFullName(2));
        }

        public void WriteLineError(string aMsg, string aFuncName)
        {
            string str = aMsg + " at " + aFuncName;
            this.WriteLineIf(this._ts.TraceError, str);
        }

        public void WriteLineIf(bool aCondition, string aMsg)
        {
            try
            {
                Trace.WriteLineIf(aCondition, aMsg);
            }
            catch (Exception)
            {
            }
        }

        public void WriteLineInfo(string aMsg)
        {
            string str = aMsg + " at " + this.GetFunctionFullName(2);
            this.WriteLineIf(this._ts.TraceInfo, str);
        }

        public void WriteLineInfo(string aMsg, string aFunctionName)
        {
            string str = string.Format("{0} ( {1} )", aMsg, aFunctionName);
            this.WriteLineIf(this._ts.TraceInfo, str);
        }

        public void WriteLineVerbos(string aMsg)
        {
            string str = aMsg + " at " + this.GetFunctionFullName(2);
            this.WriteLineIf(this._ts.TraceVerbose, str);
        }

        public void WriteLineWarning(string aMsg)
        {
            string str = aMsg + " at " + this.GetFunctionFullName(2);
            this.WriteLineIf(this._ts.TraceWarning, str);
        }

        public static TraceLogger Instance
        {
            get
            {
                lock (_async)
                {
                    if (_instance != null)
                    {
                        return _instance;
                    }
                    _instance = new TraceLogger();
                }
                return _instance;
            }
        }

        private enum FuncOpration
        {
            ENTRANCE = 1,
            EXIT = 2
        }
    }
}

