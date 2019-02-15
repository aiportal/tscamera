using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace bfbd.UltraRecord.Core
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
		public int EventsCount { get { return EventsData == null ? 0 : EventsData.Length / 16; } }
		[NonSerialized]
		public string UrlHost;

		[Obsolete]
		public byte[] ToBinary()
		{
			using (MemoryStream ms = new MemoryStream())
			using(BinaryWriter bw = new BinaryWriter(ms))
			{
				bw.Write(new byte[] { 1, 2, 6, 0 });
				bw.Write(new Guid(SessionId).ToByteArray());		// 16 bytes.
				bw.Write(new Guid(SnapshotId).ToByteArray());		// 16 bytes.
				bw.Write(SnapTime.ToBinary());						// 8 bytes.
				
				bw.Write(ProcessId);
				bw.Write(ProcessName);
				bw.Write(FileName);

				bw.Write(WindowHandle);
				bw.Write(WindowRect.X);
				bw.Write(WindowRect.Y);
				bw.Write(WindowRect.Width);
				bw.Write(WindowRect.Height);
				bw.Write(WindowTitle);
				bw.Write(WindowUrl);

				bw.Write(ControlText);
				bw.Write(InputText);

				//bw.Write((int)Mouse.ClickOption);
				//bw.Write(Mouse.X);
				//bw.Write(Mouse.Y);

				bw.Write(IsGrayScale);
				bw.Write(ImageData.Length);
				bw.Write(ImageData);
				bw.Write(EventsData.Length);
				bw.Write(EventsData);

				bw.Flush();
				return ms.ToArray();
			}
		}
		[Obsolete]
		public Snapshot FromBinary(byte[] bs)
		{		
			using (MemoryStream ms = new MemoryStream(bs))
			using (BinaryReader br = new BinaryReader(ms))
			{
				byte[] version = br.ReadBytes(4);

				Snapshot ss = new Snapshot();
				ss.SessionId = new Guid(br.ReadBytes(16)).ToString("n");
				ss.SnapshotId = new Guid(br.ReadBytes(16)).ToString("n");
				ss.SnapTime = DateTime.FromBinary(br.ReadInt64());

				ss.ProcessId = br.ReadInt32();
				ss.ProcessName = br.ReadString();
				ss.FileName = br.ReadString();

				ss.WindowHandle = br.ReadInt32();
				ss.WindowRect = new Rectangle(br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
				ss.WindowTitle = br.ReadString();
				ss.WindowUrl = br.ReadString();

				ss.ControlText = br.ReadString();
				ss.InputText = br.ReadString();

				//ss.Mouse = new MouseState()
				//{
				//    ClickOption = (MouseClickOption)br.ReadInt32(),
				//    X = br.ReadInt32(),
				//    Y = br.ReadInt32(),
				//};

				ss.IsGrayScale = br.ReadBoolean();
				int imageLen = br.ReadInt32();
				ss.ImageData = br.ReadBytes(imageLen);
				int eventsLen = br.ReadInt32();
				ss.EventsData = br.ReadBytes(eventsLen);

				return ss;
			}
		}
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
