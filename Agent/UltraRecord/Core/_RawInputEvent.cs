using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace bfbd.UltraRecord.Core
{
	delegate void KeyboardEventHandler(KeyboardEventType evt, System.Windows.Forms.Keys key);
	delegate void MouseEventHandler(MouseEventType evt, int x, int y);

	public enum KeyboardEventType : short
	{
		None = 0,
		KeyDown = 0x0100,
		KeyUp = 0x0101,
		SystemKeyDown = 0x0104,
		SystemKeyUp = 0x0105,
	}

	public enum MouseEventType : short
	{
		MouseMove = 0,
		LeftButtonDown = 0x0001,
		LeftButtonUp = 0x0002,
		MiddleButtonDown = 0x0010,
		MiddleButtonUp = 0x0020,
		RightButtonDown = 0x0004,
		RightButtonUp = 0x0008,
		MouseWheel = 0x0400,
	}

	#region RawInputData

	//[Obsolete]
	abstract class RawInputData
	{
		public DateTime Time;
	}
	[Obsolete]
	class KeyboardInputData : RawInputData
	{
		public KeyboardEventType Evt;
		public Keys Key;
	}
	[Obsolete]
	class MouseInputData : RawInputData
	{
		public MouseEventType Evt;
		public short X;
		public short Y;
	}

	#endregion RawInputData
}
