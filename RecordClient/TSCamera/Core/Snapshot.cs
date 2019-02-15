using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace bfbd.TSCamera.Core
{
	[Serializable]
	public class Snapshot
	{
		public string SessionId;
		public string SnapshotId;
		public string BackgroundId;
		public DateTime SnapTime;

		public int ProcessId;
		public string ProcessName;
		public string FileName;

		public int ScreenWidth;
		public int ScreenHeight;

		public int WindowHandle;
		public Rectangle WindowRect;
		public string WindowTitle;
		public string WindowUrl;

		//public int ControlHandle;
		public string ControlText;
		public string InputText;

		public MouseState Mouse;
		public bool IsGrayScale;
		public byte[] ImageData;
		public byte[] EventsData;

		// additional
		[NonSerialized]
		public string UrlHost;
		public int EventsCount { get { return EventsData == null ? 0 : EventsData.Length / 16; } }
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
