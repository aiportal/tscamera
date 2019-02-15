using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace bfbd.Common.Tasks
{
	partial class PeriodTask : PeriodTaskBase
	{
		#region Task Implement

		private DateTime _startTime = DateTime.Now;

		public PeriodTask(int pulse, int wait = 0, ThreadPriority priority= ThreadPriority.BelowNormal)
		{
			base.Pulse = pulse;
			base.Wait = wait;
			base.Priority = priority;
		}

		public override void Start()
		{
			_startTime = DateTime.Now;
			base.Start();
		}

		protected override void Execute()
		{
			ProcessMessage();

			foreach (Task task in _tasks)
			{
				if (task.Interval == 0)
					continue;
				if (DateTime.Now < _startTime.AddSeconds(task.Wait))
					continue;
				if (DateTime.Now < task.LastEndTime.AddSeconds(task.Interval))
					continue;

				ExecuteTask(task, task.State);
			}
		}

		private void ExecuteTask(Task task, object param)
		{
			Debug.Assert(task != null);
			try
			{
				task.Action(param);
			}
			catch (Exception ex)
			{
				TraceLog.WriteException(ex);
				if (OnException != null)
				{
					try { OnException(this, new TaskExceptionArgs(task.Name, param, ex)); }
					catch (Exception) { }
				}
			}
			finally
			{
				task.LastEndTime = DateTime.Now;
			}
		}

		public event EventHandler<TaskExceptionArgs> OnException;

		#endregion Task Implement
	}

	partial class PeriodTask
	{
		#region Task Init

		private List<Task> _tasks = new List<Task>();

		public void AddTask(string name, Action<object> action, object state, int intervalSeconds, int waitSeconds)
		{
			if (IsRunning)
				throw new InvalidOperationException("Task is running.");
			if (_tasks.Exists(t => t.Name == name))
				throw new ArgumentException("Task name has existed.");

			_tasks.Add(new Task()
			{
				Name = name,
				Action = action,
				State = state,
				Wait = waitSeconds,
				Interval = intervalSeconds,
				LastEndTime = DateTime.MinValue
			});
		}

		class Task
		{
			public string Name;
			public Action<object> Action;
			public object State;
			public int Wait;
			public int Interval;
			public DateTime LastEndTime;
		}

		#endregion Task Init
	}

	#region Task Exception

	class TaskExceptionArgs : EventArgs
	{
		public string TaskName { get; private set;}
		public object State { get; private set; }
		public Exception Exception { get; private set; }

		public TaskExceptionArgs(string name, object state, Exception ex)
		{
			TaskName = name; State = state; Exception = ex;
		}
	}

	#endregion Task Exception
}
