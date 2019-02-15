using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace bfbd.UltraRecord.Core
{
	interface IKeyboardEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }
		Keys Key { get; }
		bool Shift { get; }
		bool Ctrl { get; }
		bool Alt { get; }
	}

	interface IMouseEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }
		short X { get; }
		short Y { get; }
	}

	interface IRawEvent
	{
		DateTime Time { get; }
		RawEventType Evt { get; }

		bool IsKeyEvent { get; }
		bool IsMouseEvent { get; }

		IKeyboardEvent KeyEvent { get; }
		IMouseEvent MouseEvent { get; }
	}

	partial class RawInputEvent : IKeyboardEvent, IMouseEvent, IRawEvent
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

		public bool IsKeyEvent { get { return (_evt & RawEventType.KeyEvent) > 0; } }
		public bool IsMouseEvent { get { return (_evt & RawEventType.MouseEvent) > 0; } }

		public IKeyboardEvent KeyEvent { get { return (_evt & RawEventType.KeyEvent) > 0 ? this as IKeyboardEvent : null; } }
		public IMouseEvent MouseEvent { get { return (_evt & RawEventType.MouseEvent) > 0 ? this as IMouseEvent : null; } }
	}

	#region Converter
	partial class RawInputEvent
	{
		public RawInputEvent(RawInputWnd.KeyboardEventType kEvt, System.Windows.Forms.Keys key, DateTime time)
		{
			this._time = time;
			this._evt = KEvts[kEvt];
			this._data = (uint)key;
		}

		public RawInputEvent(RawInputWnd.MouseEventType mEvt, short x, short y, DateTime time)
		{
			this._time = time;
			this._evt = MEvts[mEvt];
			this._data = (uint)((((int)y) << 16) | (ushort)x);
			System.Diagnostics.Debug.Assert(((short)((ushort)x)) == x);
		}

		internal static readonly Dictionary<RawInputWnd.KeyboardEventType, RawEventType> KEvts = new Dictionary<RawInputWnd.KeyboardEventType, RawEventType>
		{
			{RawInputWnd.KeyboardEventType.None, RawEventType.None},
			{RawInputWnd.KeyboardEventType.KeyDown, RawEventType.KeyDown},
			{RawInputWnd.KeyboardEventType.KeyUp, RawEventType.KeyUp},
			{RawInputWnd.KeyboardEventType.SystemKeyDown, RawEventType.SystemKeyDown},
			{RawInputWnd.KeyboardEventType.SystemKeyUp, RawEventType.SystemKeyUp},
		};
		internal static readonly Dictionary<RawInputWnd.MouseEventType, RawEventType> MEvts = new Dictionary<RawInputWnd.MouseEventType, RawEventType>
		{
			{RawInputWnd.MouseEventType.MouseMove, RawEventType.MouseMove},
			{RawInputWnd.MouseEventType.LeftButtonDown, RawEventType.LeftButtonDown},
			{RawInputWnd.MouseEventType.LeftButtonUp, RawEventType.LeftButtonUp },
			{RawInputWnd.MouseEventType.MiddleButtonDown, RawEventType.MiddleButtonDown},
			{RawInputWnd.MouseEventType.MiddleButtonUp,RawEventType.MiddleButtonUp},
			{RawInputWnd.MouseEventType.RightButtonDown,RawEventType.RightButtonDown},
			{RawInputWnd.MouseEventType.RightButtonUp,RawEventType.RightButtonUp},
			{RawInputWnd.MouseEventType.MouseWheel,RawEventType.MouseWheel},
		};
	}
	#endregion

	#region Serilize
	partial class RawInputEvent
	{
		protected RawInputEvent() { }

		public static byte[] ToBinary(IEnumerable<IRawEvent> evts)
		{
			using(System.IO.MemoryStream ms = new System.IO.MemoryStream())
			using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
			{
				foreach (RawInputEvent e in evts)
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

		public static IEnumerable<IRawEvent> FromBinary(byte[] bs)
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
	#endregion

	#region RawEventType
	public enum RawEventType : short
	{
		None,

		KeyDown = 0x1,
		KeyUp = 0x2,
		SystemKeyDown = 0x3,
		SystemKeyUp = 0x4,
		/// <summary>
		/// Key Event Mask
		/// </summary>
		KeyEvent = 0xF,

		MouseMove = 0x10,
		LeftButtonDown = 0x20,
		LeftButtonUp = 0x30,
		MiddleButtonDown = 0x40,
		MiddleButtonUp = 0x50,
		RightButtonDown = 0x60,
		RightButtonUp = 0x70,
		MouseWheel = 0x80,
		/// <summary>
		/// Mouse Event Mask
		/// </summary>
		MouseEvent = 0xF0,
	}
	#endregion
}
