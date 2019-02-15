using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Core
{
	interface IRecordPolicy
	{
		bool FireEvent(IRawEvent evt);
		bool Snapshot(IRawEvent evt);
		string GetText(IEnumerable<IRawEvent> evts);
	}

	class DebugPolicy : IRecordPolicy
	{
		public bool FireEvent(IRawEvent e)
		{
			if (e.IsKeyEvent)
				return true;
			else if (e.IsMouseEvent)
				return e.Evt != RawEventType.MouseMove;
			else
				return false;
		}

		public bool Snapshot(IRawEvent e)
		{
			if (e.IsKeyEvent)
				return e.Evt == RawEventType.KeyUp;
			else if (e.IsMouseEvent)
				return e.Evt == RawEventType.LeftButtonDown | e.Evt == RawEventType.RightButtonDown;
			else
				return false;
		}

		public string GetText(IEnumerable<IRawEvent> evts)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var e in evts)
			{
				if (e.IsKeyEvent && e.Evt == RawEventType.KeyUp)
					sb.Append((Char)user32.MapVirtualKey((uint)e.KeyEvent.Key, util.MAPVK_VK_TO_CHAR));
			}
			return sb.ToString();
		}
	}
}
