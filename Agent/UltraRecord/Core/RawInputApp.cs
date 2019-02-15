using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	using SerialNumber = bfbd.Common.License.SerialNumber;

	partial class RawInputApp : System.Windows.Forms.ApplicationContext
	{
		const string APP_TITLE = "TSCamera";
		const string APP_SITE = "http://www.ultragis.com";

		private Queue _events = Queue.Synchronized(new Queue());

		private IRecordPolicy _policy;
		private SessionInfo _session;
		private BackgroundWorker _recordWorker;
		private bfbd.Common.Task.PeriodTask _tasks;
		private NotifyIcon _notifyIcon;

		public string SessionId { get; set; }

		public RawInputApp()
		{
			// load config
			try
			{
				TraceLogger.Instance.WriteLineInfo("Configuration is loading...");
				string fpath = System.IO.Path.Combine(Application.StartupPath, "record.config");
				Global.Config.LoadConfiguration(fpath);
				TraceLogger.Instance.WriteLineInfo("Configuration has loaded.");
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

			// init app
			TraceLogger.Instance.WriteLineInfo("Application is initing...");
			this.InitNotifyIcon();
			Application.ApplicationExit += (o, ev) => { this.Dispose(false); };
			Application.Idle += (o, ev) => { GC.Collect(); };
			TraceLogger.Instance.WriteLineInfo("Application has inited.");

			// check license
			while (!IsLicenseValid())
			{
				UpdateConfiguration(null);
				Thread.Sleep(10 * 1000);
			}

			// record policy
			TraceLogger.Instance.WriteLineInfo("RecordPolicy is creating: " + Global.Config.RecordPolicy);
			_policy = CreateRecordPolicy();
			TraceLogger.Instance.WriteLineInfo("RecordPolicy has created.");

			// start hook
			TraceLogger.Instance.WriteLineInfo("RawInputWnd is creating...");
			this.MainForm = this.CreateRawInputWnd();
			TraceLogger.Instance.WriteLineInfo(string.Format("RawInputWnd has created: {0}", (this.MainForm == null) ? "null" : this.MainForm.Handle.ToString()));

			// start tasks
			TraceLogger.Instance.WriteLineInfo("Tasks is starting...");
			_tasks = new Common.Task.PeriodTask(1000);
			_tasks.AddTask("Configuration", this.UpdateConfiguration, 60, 0);
			_tasks.AddTask("ShortSession", this.MakeShortSession, 10, 10);
			_tasks.Start();
			TraceLogger.Instance.WriteLineInfo("Tasks has started.");

			// start worker
			TraceLogger.Instance.WriteLineInfo("BackgroundWorker is starting...");
			_recordWorker = new BackgroundWorker();
			_recordWorker.DoWork += (o, ev) => { this.RecordWorker(); };
			_recordWorker.WorkerReportsProgress = true;
			_recordWorker.RunWorkerAsync();
			TraceLogger.Instance.WriteLineInfo("BackgroundWorker has started.");
		}

		private void InitNotifyIcon()
		{
			_notifyIcon = new NotifyIcon()
			{
				Icon = Resources.eye16,
				Text = APP_TITLE + Environment.NewLine + APP_SITE,
				Visible = true,
			};
		}

		protected override void Dispose(bool disposing)
		{
			if (_notifyIcon != null)
			{
				_notifyIcon.Visible = false;
				_notifyIcon = null;
			}
			base.Dispose(disposing);
		}

		private void RecordWorker()
		{
			try
			{
				TraceLogger.Instance.WriteLineInfo("New session is creating...");
				this._session = this.CreateRecordSession(this.SessionId);
				this.SessionId = this._session.SessionId;
				TraceLogger.Instance.WriteLineInfo(string.Format("UserName={0}, SessionId={1}, CreateTime={2:o}", _session.UserName, _session.SessionId, _session.CreateTime));
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				Application.Exit();
			}

			List<IRawEvent> evts = new List<IRawEvent>();
			while (true)
			{
				try
				{
					if (_events.Count > 0)
					{
						TraceLogger.Instance.WriteLineVerbos("Events data count: " + _events.Count);
						{
							object[] tempEvts = null;
							//lock (_eventsRoot)
							{
								tempEvts = _events.ToArray();
								_events.Clear();
							}
							evts.AddRange(Array.ConvertAll(tempEvts, e => e as IRawEvent));
							Debug.Assert(evts.Count > 0);
						}
						if (_policy.Snapshot(evts[evts.Count-1]))
						{
							ThreadPool.QueueUserWorkItem(this.SnapshotWorker, evts.ToArray());
							TraceLogger.Instance.WriteLineInfo("SnapshotWorker has queued at ticks: " + DateTime.Now.Ticks);
							evts.Clear();
						}
						_session.LastActiveTime = DateTime.Now;
					}
				}
				catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }

				Thread.Sleep(10);
			}
		}

		private DateTime _prevMouse = DateTime.Now;
		private RawInputWnd CreateRawInputWnd()
		{
			// if create RawInputWnd fail, exit process to try again.
			try
			{
				RawInputWnd form = null;
				form = new RawInputWnd();
				form.RawInputMouseEvent += (e, x, y) =>
				{
					TraceLogger.Instance.WriteLineVerbos("RawInputMouseEvent: " + e);
					var evt = new RawInputEvent(e, (short)x, (short)y, DateTime.Now);
					if (_policy.FireEvent(evt))
					{
						TraceLogger.Instance.WriteLineVerbos("Push event: " + evt.Evt);
						_events.Enqueue(evt);
					}
				};
				form.RawInputKeyboardEvent += (e, key) =>
				{
					TraceLogger.Instance.WriteLineVerbos("RawInputKeyboardEvent: " + e);
					var evt = new RawInputEvent(e, key, DateTime.Now);
					if (_policy.FireEvent(evt))
					{
						TraceLogger.Instance.WriteLineVerbos("Push event: " + evt.Evt);
						_events.Enqueue(evt);
					}
					///? GetKeyState to get state of shift, ctrl and alt, mask it in key value by Keys.Shift/Control/Alt.
				};
				return form;
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				Application.Exit();
				return null;
			}
		}

		private IRecordPolicy CreateRecordPolicy()
		{
			IRecordPolicy policy = null;
			try
			{
				if (!string.IsNullOrEmpty(Global.Config.RecordPolicy))
				{
					string path = System.IO.Path.Combine(Application.StartupPath, "Policy.dll");
					if (System.IO.File.Exists(path))
					{
						var assembly = System.Reflection.Assembly.LoadFrom(path);
						if (assembly != null)
							policy = assembly.CreateInstance(Global.Config.RecordPolicy) as IRecordPolicy;
					}
					else
					{
						var assembly = System.Reflection.Assembly.GetEntryAssembly();
						if (assembly != null)
							policy = assembly.CreateInstance(Global.Config.RecordPolicy) as IRecordPolicy;
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			if (policy == null)
				policy = new GenericPolicy();
			return policy;
		}
	}
}
