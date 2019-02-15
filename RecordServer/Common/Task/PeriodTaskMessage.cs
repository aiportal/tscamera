using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Tasks
{
	partial class PeriodTask
	{
		#region Message Implement

		private static System.Collections.Queue _messages = Queue.Synchronized(new Queue());

		public static void PostMessage(string taskName, object param = null)
		{
			_messages.Enqueue(new TaskMsg() { Name = taskName, Param = param });
		}

		private void ProcessMessage()
		{
			while (_messages.Count > 0)
			{
				var msg = _messages.Dequeue() as TaskMsg;
				var task = _tasks.Find(t => t.Name == msg.Name);
				if (task != null)
				{
					ExecuteTask(task, msg.Param == null ? task.State : msg.Param);
				}
			}
		}

		class TaskMsg
		{
			public string Name;
			public object Param;
		}

		#endregion Message Implement
	}
}
