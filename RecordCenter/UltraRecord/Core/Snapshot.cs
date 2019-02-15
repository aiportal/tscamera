using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace bfbd.UltraRecord.Core
{
	[Serializable]
	public class Snapshot
	{
		public string SessionId;
		public string SnapshotId;
		public DateTime SnapTime;

		public int ProcessId;
		public string ProcessName;
		public string FileName;

		public int WindowHandle;
		public string WindowTitle;
		public RECT WindowRect;

		public MouseState Mouse;
		public bool IsGrayScale;
		public byte[] ImageData;

		public string InputText;
		public byte[] EventsData;
	}

	[Serializable]
	public class MouseState
	{
		public MouseClickOption ClickOption;
		public int X;
		public int Y;
	}

	[Serializable]
	public enum MouseClickOption
	{
		None,
		RightButtonDown,
		LeftButtonDown
	}
}
