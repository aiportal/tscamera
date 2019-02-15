using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace bfbd.UltraRecord.Client
{
	partial class RecordService : ServiceBase
	{
		private RecordServiceCore _record = new RecordServiceCore();

		public RecordService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			_record.Start();
		}

		protected override void OnStop()
		{
			_record.Stop();
		}

		protected override void OnSessionChange(SessionChangeDescription desc)
		{
			base.OnSessionChange(desc);

			TraceLogger.Instance.WriteLineInfo(string.Format("OnSessionChange: Reason={0}, SessionId={1}", desc.Reason, desc.SessionId));
			switch (desc.Reason)
			{
				case SessionChangeReason.SessionLogon:
					_record.SessionLogon(desc.SessionId);
					break;
				case SessionChangeReason.SessionLogoff:
					_record.SessionLogoff(desc.SessionId);
					break;
			}
		}

		protected override void OnShutdown()
		{
			try
			{
				Array.ForEach(Process.GetProcessesByName("rcda"), p => p.Kill());
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}
	}
}
