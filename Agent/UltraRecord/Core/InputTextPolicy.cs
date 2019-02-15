using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	public class InputTextPolicy : IRecordPolicy
	{
		public bool RecordKeyboardEvent(KeyboardEventType evt, Keys key)
		{
			return evt == KeyboardEventType.KeyDown;
		}

		public bool RecordMouseEvent(MouseEventType evt, int x, int y)
		{
			return evt == MouseEventType.LeftButtonDown || evt == MouseEventType.RightButtonDown;
		}

		private bool _isLastEventFromKeyboard = false;

		private DateTime _lastKeyTime;
		private Keys _lastKeyValue;
		private DateTime _prevGeneralKeyTime;

		public bool SnapshotOnKeyboardEvent(KeyboardEventType evt, Keys key)
		{
			bool snap = false;
			if (evt == KeyboardEventType.KeyDown)
			{
				if (key == Keys.Enter || key == Keys.Delete)
				{
					if (DateTime.Now.Subtract(_lastKeyTime).Milliseconds > 1000)
					{
						snap = true;
					}
					else
					{
						if (!_isLastEventFromKeyboard || key != _lastKeyValue)
						{
							snap = true;
						}
					}
				}
				else // General Key
				{
					if (!_isLastEventFromKeyboard && DateTime.Now.Subtract(_prevGeneralKeyTime).TotalMilliseconds > 1000)
					{
						snap = true;
						_prevGeneralKeyTime = DateTime.Now;
					}
					if (DateTime.Now.Subtract(_prevGeneralKeyTime).TotalMilliseconds > 10 * 1000)
					{
						snap = true;
						_prevGeneralKeyTime = DateTime.Now;
					}
				}
				_isLastEventFromKeyboard = true;
				_lastKeyTime = DateTime.Now;
				_lastKeyValue = key;
			}
			TraceLogger.Instance.WriteLineInfo("SnapshotOnKeyboardEvent: " + snap.ToString());
			return snap;
		}

		private DateTime _lastMouseTime;
		private MouseState _lastMouseState = new MouseState();
		private DateTime _prevLButtonDownTime;

		public bool SnapshotOnMouseEvent(MouseEventType evt, int x, int y)
		{
			bool snap = false;
			if ((evt == MouseEventType.LeftButtonDown) || (evt == MouseEventType.RightButtonDown))
			{
				MouseState mouse = SnapshotEngine.CaptureMouseState(evt);
				if (mouse.ClickOption != _lastMouseState.ClickOption)
				{
					snap = true;
				}
				else
				{
					if (evt == MouseEventType.LeftButtonDown)
					{
						if (DateTime.Now.Subtract(_prevLButtonDownTime).TotalMilliseconds < 1000)
						{
							if ((Math.Abs(mouse.X - _lastMouseState.X) < 15) && (Math.Abs(mouse.Y - _lastMouseState.Y) < 10))
							{
								snap = true;
							}
						}
						_prevLButtonDownTime = DateTime.Now;
					}
					if ((Math.Abs((int)(mouse.X - _lastMouseState.X)) > 5) || (Math.Abs((int)(mouse.Y - _lastMouseState.Y)) > 5))
					{
						snap = true;
					}
				}
				_isLastEventFromKeyboard = false;
				_lastMouseTime = DateTime.Now;
			}
			TraceLogger.Instance.WriteLineInfo("SnapshotOnMouseEvent: " + snap.ToString());
			return snap;
		}
	}
}
