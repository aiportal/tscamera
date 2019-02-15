using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace bfbd.UltraRecord.Core
{
	using bfbd.WindowsAPI.RawInput;

	class RawInputWnd : System.Windows.Forms.Form
	{
		public event KeyboardEventHandler RawInputKeyboardEvent;
		public event MouseEventHandler RawInputMouseEvent;

		public RawInputWnd()
		{
			base.Visible = false;
			base.ShowInTaskbar = false;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams prams = base.CreateParams;
				prams.Parent = (IntPtr)util.HWND_MESSAGE;
				return prams;
			}
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case util.WM_CREATE:
					this.RegisterRawInput(base.Handle);
					break;
				case util.WM_INPUT:
					this.GetRawInputData(m.LParam);
					break;
			}
			base.WndProc(ref m);
		}

		private void RegisterRawInput(IntPtr hWnd)
		{
			// if create RegisterRawInput fail, exit process to try again.
			try
			{
				RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[]
				{
					new RAWINPUTDEVICE(){
						usUsagePage = util.HID_USAGEPAGE_GENERIC,
						usUsage = util.HID_USAGE_KEYBOARD,
						dwFlags = (int)RawInputDeviceFlags.InputSink,
						hwndTarget = hWnd
					},
#if !DEBUG
					new RAWINPUTDEVICE(){
					    usUsagePage = util.HID_USAGEPAGE_GENERIC,
					    usUsage = util.HID_USAGE_MOUSE,				    
					    dwFlags = (int)RawInputDeviceFlags.InputSink,
					    hwndTarget = hWnd
					},
#endif
				};
				bool succeed = user32.RegisterRawInputDevices(rid, rid.Length, Marshal.SizeOf(rid[0]));
				TraceLogger.Instance.WriteLineInfo("RegisterRawInputDevices : " + succeed);
				if (!succeed)
					throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
				Application.Exit();
			}
		}

		private void GetRawInputData(IntPtr hRawInput)
		{
			try
			{
				int bsCount = -1;
				int blen = 0;
				int hlen = Marshal.SizeOf(typeof(RAWINPUTHEADER));

				//TraceLogger.Instance.WriteLineInfo("Get RawInput data.");
				bsCount = user32.GetRawInputData(hRawInput, RawInputCommand.Input, IntPtr.Zero, ref blen, hlen);
				if ((bsCount == -1) || (blen < 1))
				{ throw new Win32Exception(Marshal.GetLastWin32Error(), "GetRawInputData Error Retreiving Buffer size."); }
				else
				{
					IntPtr pBuffer = Marshal.AllocHGlobal(blen);
					try
					{
						bsCount = user32.GetRawInputData(hRawInput, RawInputCommand.Input, pBuffer, ref blen, hlen);
						if (bsCount != blen)
						{ throw new Win32Exception(Marshal.GetLastWin32Error(), "GetRawInputData Error Retreiving Buffer data."); }
						else
						{
							RawInput ri = (RawInput)Marshal.PtrToStructure(pBuffer, typeof(RawInput));
							FireRawInputEvent(ref ri);
						}
					}
					catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
					finally
					{
						Marshal.FreeHGlobal(pBuffer);
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
		}

		private void FireRawInputEvent(ref RawInput ri)
		{
			try
			{
				TraceLogger.Instance.WriteLineVerbos("FireRawInputEvent: " + ri.Header.dwType);
				switch (ri.Header.dwType)
				{
					case (int)RawInputType.Mouse:
						if (RawInputMouseEvent != null)
						{
							//if (Enum.IsDefined(typeof(MouseEventType), (short)(ri.Data.Mouse.ulButtons & ushort.MaxValue)))
							{
								MouseEventType evt = (MouseEventType)ri.Data.Mouse.ulButtons;
								int x = Cursor.Position.X;
								int y = Cursor.Position.Y;
								RawInputMouseEvent(evt, x, y);
							}
							//else
							{
								//TraceLogger.Instance.WriteLineInfo(string.Format("Undefined MouseEventType: {0:X}", ri.Data.Mouse.ulButtons));
							}
						}
						break;
					case (int)RawInputType.KeyBoard:
						if (RawInputKeyboardEvent != null)
						{
							//if (Enum.IsDefined(typeof(KeyboardEventType), (short)ri.Data.Keyboard.Message))
							{
								KeyboardEventType evt = (KeyboardEventType)ri.Data.Keyboard.Message;
								Keys key = (Keys)ri.Data.Keyboard.VKey;
								RawInputKeyboardEvent(evt, key);
							}
							//else
							{
								//TraceLogger.Instance.WriteLineInfo(string.Format("Undefined KeyboardEventType: {0:X}", ri.Data.Keyboard.Message));
							}
						}
						break;
					default:
						break;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Instance.WriteException(ex);
			}
		}

		public delegate void KeyboardEventHandler(KeyboardEventType evt, System.Windows.Forms.Keys key);
		public delegate void MouseEventHandler(MouseEventType evt, int x, int y);

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
	}
}
