using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Task
{
	partial class PeriodTask : PeriodTaskBase
	{
		private DateTime _startTime = DateTime.Now;

		public PeriodTask(int pulse)
		{
			Pulse = pulse;
			Wait = 0;
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
				if (DateTime.Now < _startTime.AddSeconds(task.Wait))
					continue;
				if (DateTime.Now < task.LastEndTime.AddSeconds(task.Interval))
					continue;

				ExecuteTask(task);
			}
		}

		private void ExecuteTask(Task task)
		{
			System.Diagnostics.Debug.Assert(task != null);
			try
			{
				task.Action(task.Action.Target);
				task.LastEndTime = DateTime.Now;
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				if (OnException != null)
				{
					try
					{
						OnException(this, new TaskExceptionArgs() { TaskName = task.Name, Target = task.Action.Target, Exception = ex });
					}
					catch (Exception) { }
				}
			}
		}

		public event EventHandler<TaskExceptionArgs> OnException;
	}

	partial class PeriodTask
	{
		private List<Task> _tasks = new List<Task>();

		public void AddTask(string name, Action<object> action, int intervalSeconds, int waitSeconds = 0)
		{
			if (IsRunning)
				throw new InvalidOperationException("Task is running.");
			if (_tasks.Exists(t => t.Name == name))
				throw new ArgumentException("Task name exist.");

			_tasks.Add(new Task()
			{
				Name = name,
				Action = action,
				Wait = waitSeconds,
				Interval = intervalSeconds,
				LastEndTime = DateTime.MinValue
			});
		}

		class Task
		{
			public string Name { get; set; }
			public Action<object> Action { get; set; }
			public int Wait { get; set; }
			public int Interval { get; set; }
			public DateTime LastEndTime { get; set; }
		}
	}

	partial class PeriodTask
	{
		private static System.Collections.Queue _messages = Queue.Synchronized(new Queue());

		public static void PostMessage(string taskName)
		{
			_messages.Enqueue(taskName);
		}

		private void ProcessMessage()
		{
			while (_messages.Count > 0)
			{
				string msg = _messages.Dequeue() as string;
				var task = _tasks.Find(t => t.Name == msg);
				if (task != null)
				{
					ExecuteTask(task);
				}
			}
		}
	}

	class TaskExceptionArgs : EventArgs
	{
		public string TaskName { get;set;}
		public object Target { get; set; }
		public Exception Exception {get;set;}
	}
}
