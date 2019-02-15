using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Core.New
{
	interface KeyboardEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }
		Keys Key { get; }
		bool Shift { get; }
		bool Ctrl { get; }
		bool Alt { get; }
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
		private DateTime _time;
		private RawEventType _evt;
		private Int16 _reserved;
		private UInt32 _data;

		public DateTime Time { get { return _time; } }
		public RawEventType Evt { get { return _evt; } }

		public Keys Key { get { return ((Keys)_data & Keys.KeyCode); } }
		public bool Shift { get { return ((Keys)_data & Keys.Shift) == Keys.Shift; } }
		public bool Ctrl { get { return ((Keys)_data & Keys.Control) == Keys.Control; } }
		public bool Alt { get { return ((Keys)_data & Keys.Alt) == Keys.Alt; } }

		public short X { get { return (short)(_data & Int16.MaxValue); } }
		public short Y { get { return (short)(_data >> 16); } }
		
		KeyboardEvent KeyEvt { get { return (_evt & RawEventType.KeyEvent) > 0 ? this as KeyboardEvent : null; } }
		MouseEvent MouseEvt { get { return (_evt & RawEventType.MouseEvent) > 0 ? this as MouseEvent : null; } }
	}

	partial class RawInputEvent
	{
		public RawInputEvent(KeyboardEventType kEvt, System.Windows.Forms.Keys key) : this(kEvt, key, DateTime.Now) { }
		public RawInputEvent(KeyboardEventType kEvt, System.Windows.Forms.Keys key, DateTime time)
		{
			this._time = time;
			this._evt = KEvts[kEvt];
			this._data = (uint)key;
		}

		public RawInputEvent(MouseEventType mEvt, short x, short y) : this(mEvt, x, y, DateTime.Now) { }
		public RawInputEvent(MouseEventType mEvt, short x, short y, DateTime time)
		{
			this._time = time;
			this._evt = MEvts[mEvt];
			this._data = (uint)((((int)y) << 16) | (int)x);
		}

		internal static readonly Dictionary<KeyboardEventType, RawEventType> KEvts = new Dictionary<KeyboardEventType, RawEventType>
		{
			{KeyboardEventType.None, RawEventType.None},
			{KeyboardEventType.KeyDown, RawEventType.KeyDown},
			{KeyboardEventType.KeyUp, RawEventType.KeyUp},
			{KeyboardEventType.SystemKeyDown, RawEventType.SystemKeyDown},
			{KeyboardEventType.SystemKeyUp, RawEventType.SystemKeyUp},
		};
		internal static readonly Dictionary<MouseEventType, RawEventType> MEvts = new Dictionary<MouseEventType, RawEventType>
		{
			{MouseEventType.MouseMove, RawEventType.MouseMove},
			{MouseEventType.LeftButtonDown, RawEventType.LeftButtonDown},
			{MouseEventType.LeftButtonUp, RawEventType.LeftButtonUp },
			{MouseEventType.MiddleButtonDown, RawEventType.MiddleButtonDown},
			{MouseEventType.MiddleButtonUp,RawEventType.MiddleButtonUp},
			{MouseEventType.RightButtonDown,RawEventType.RightButtonDown},
			{MouseEventType.RightButtonUp,RawEventType.RightButtonUp},
			{MouseEventType.MouseWheel,RawEventType.MouseWheel},
		};

		protected RawInputEvent() { }

		public static byte[] ToBinary(IEnumerable<RawInputEvent> evts)
		{
			using(System.IO.MemoryStream ms = new System.IO.MemoryStream())
			using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
			{
				foreach (var e in evts)
				{
					bw.Write((Int64)e._time.ToBinary());
					bw.Write((Int16)e._evt);
					bw.Write(e._reserved);
					bw.Write(e._data);
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
				RawInputEvent e = new RawInputEvent();
				while(br.PeekChar() != -1)
				{
					e._time = DateTime.FromBinary(br.ReadInt64());
					e._evt = (RawEventType)br.ReadInt16();
					e._reserved = br.ReadInt16();
					e._data = br.ReadUInt32();
					yield return e;
				}				
			}
		}
	}

	enum RawEventType : short
	{
		None,

		KeyDown = 0x0001,
		KeyUp = 0x0002,
		SystemKeyDown = 0x0003,
		SystemKeyUp = 0x0004,
		/// <summary>
		/// Key Event Mask
		/// </summary>
		KeyEvent = 0x000F,

		MouseMove = 0x0010,
		LeftButtonDown = 0x0020,
		LeftButtonUp = 0x0030,
		MiddleButtonDown = 0x0040,
		MiddleButtonUp = 0x0050,
		RightButtonDown = 0x0060,
		RightButtonUp = 0x0070,
		MouseWheel = 0x0080,
		/// <summary>
		/// Mouse Event Mask
		/// </summary>
		MouseEvent = 0x00F0,	
	}
}
