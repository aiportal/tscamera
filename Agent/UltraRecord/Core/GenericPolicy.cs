using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	partial class GenericPolicy : IRecordPolicy
	{
		public bool FireEvent(IRawEvent e)
		{
			bool fire = false;
			if (e.IsKeyEvent)
				fire = e.Evt == RawEventType.KeyUp;
			else if (e.IsMouseEvent)
				fire = e.Evt == RawEventType.LeftButtonDown || e.Evt == RawEventType.RightButtonDown;
			TraceLogger.Instance.WriteLineVerbos(string.Format("FireEvent policy return {0} for event: {1} on {2}", fire, e.Evt, e.Time));
			return fire;
		}

		public bool Snapshot(IRawEvent e)
		{
			//TraceLogger.Instance.WriteLineVerbos("Snapshot policy for event: " + e.Evt + " on " + e.Time);
			if (e.IsKeyEvent)
				return SnapshotOnKeyboardEvent(e.KeyEvent.Evt, e.KeyEvent.Key);
			else if (e.IsMouseEvent)
				return SnapshotOnMouseEvent(e.MouseEvent.Evt, e.MouseEvent.X, e.MouseEvent.Y);
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
			return sb.Length < 1024 ? sb.ToString() : sb.ToString().Substring(0, 1024);
		}
	}

	partial class GenericPolicy 
	{
		bool _isLastEventFromKeyboard = false;

		DateTime _lastKeyTime;
		Keys _lastKeyValue;
		DateTime _prevGeneralKeyTime;

		bool SnapshotOnKeyboardEvent(RawEventType evt, Keys key)
		{
			bool snap = false;
			if (evt == RawEventType.KeyUp)
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
					if (DateTime.Now.Subtract(_prevGeneralKeyTime).TotalMilliseconds > 1000)
					{
						snap = true;
						_prevGeneralKeyTime = DateTime.Now;
					}
				}
				_isLastEventFromKeyboard = true;
				_lastKeyTime = DateTime.Now;
				_lastKeyValue = key;
			}
			TraceLogger.Instance.WriteLineVerbos("SnapshotOnKeyboardEvent: " + snap.ToString());
			return snap;
		}

		DateTime _lastMouseTime;
		MouseState _lastMouseState = new MouseState();
		DateTime _prevLButtonDownTime;

		bool SnapshotOnMouseEvent(RawEventType evt, int x, int y)
		{
			bool snap = false;
			if ((evt == RawEventType.LeftButtonDown) || (evt == RawEventType.RightButtonDown))
			{
				MouseState mouse = CaptureMouseState(evt);
				if (mouse.ClickOption != _lastMouseState.ClickOption)
				{
					snap = true;
				}
				else
				{
					if (evt == RawEventType.LeftButtonDown)
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
			TraceLogger.Instance.WriteLineVerbos("SnapshotOnMouseEvent: " + snap.ToString());
			return snap;
		}

		private MouseState CaptureMouseState(RawEventType evt)
		{
			MouseState mouse = null;
			try
			{
				if (!Cursor.Position.IsEmpty)
				{
					mouse = new MouseState()
					{
						ClickOption = MouseClickOption.None,
						X = Cursor.Position.X,
						Y = Cursor.Position.Y,
					};
					if (evt == RawEventType.LeftButtonDown)
						mouse.ClickOption = MouseClickOption.LeftButtonDown;
					else if (evt == RawEventType.RightButtonDown)
						mouse.ClickOption = MouseClickOption.RightButtonDown;
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return mouse;
		}

		public class MouseState
		{
			public MouseClickOption ClickOption;
			public int X;
			public int Y;
		}

		public enum MouseClickOption
		{
			None,
			RightButtonDown,
			LeftButtonDown
		}
	}
}