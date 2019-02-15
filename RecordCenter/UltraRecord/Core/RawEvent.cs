using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Core
{
	interface KeyboardEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }
		Keys Key { get; }
	}

	interface MouseEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }
		short X { get; }
		short Y { get; }
	}

	partial class RawInputEvent : KeyboardEvent, MouseEvent
	{
		public DateTime Time { get; private set; }
		public RawEventType Evt { get; private set; }
		public int Data { get; private set; }

		public Keys Key { get { return (Keys)Data; } }
		public short X { get { return (short)(Data & UInt16.MaxValue); } }
		public short Y { get { return (short)(Data >> 16); } }
		
		KeyboardEvent KeyEvt { get { return (RawEventType.KeyStart <= Evt && Evt <= RawEventType.KeyEnd) ? this as KeyboardEvent : null; } }
		MouseEvent MouseEvt { get { return (RawEventType.MouseStart <= Evt && Evt <= RawEventType.MouseEnd) ? this as MouseEvent : null; } }
	}

	partial class RawInputEvent
	{
		protected RawInputEvent() { }

		public static byte[] ToBinary(IEnumerable<RawInputEvent> evts)
		{
			using(System.IO.MemoryStream ms = new System.IO.MemoryStream())
			using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
			{
				foreach (var e in evts)
				{
					bw.Write(e.Time.ToBinary());
					bw.Write((int)e.Evt);
					bw.Write(e.Data);
				}
				bw.Flush();
				return ms.ToArray();
			}
		}

		public static IEnumerable<RawInputEvent> FromBinary(byte[] bs)
		{
			using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bs))
			using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms))
			{
				RawInputEvent evt = new RawInputEvent();
				while(br.PeekChar() != -1)
				{
					evt.Time = DateTime.FromBinary(br.ReadInt64());
					evt.Evt = (RawEventType)br.ReadInt32();
					evt.Data = br.ReadInt32();
					yield return evt;
				}				
			}
		}
	}

	enum RawEventType : int
	{
		None,

		KeyStart = 0x0100,
		KeyDown = KeyStart,
		KeyUp,
		SystemKeyDown,
		SystemKeyUp,
		KeyEnd = SystemKeyUp,

		MouseStart = 0x200,
		MouseMove = MouseStart,
		LeftButtonDown,
		LeftButtonUp,
		MiddleButtonDown,
		MiddleButtonUp,
		RightButtonDown,
		RightButtonUp,
		MouseWheel,
		MouseEnd = MouseWheel,
	}
}
