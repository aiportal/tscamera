using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace bfbd.WindowsAPI.RawInput
{
	class user32
	{
		//[DllImport("user32.dll", EntryPoint = "GetRawInputDeviceList", SetLastError = true)]
		//public static extern int GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref int uiNumDevices, int cbSize);

		//[DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfo", SetLastError = true)]
		//public static extern int GetRawInputDeviceInfo(IntPtr hDevice, int uiCommand, IntPtr pData, ref int pcbSize);

		[DllImport("user32.dll", EntryPoint = "RegisterRawInputDevices", SetLastError = true)]
		public static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]RAWINPUTDEVICE[] pRawInputDevices, int uiNumDevices, int cbSize);

		[DllImport("user32.dll", EntryPoint = "GetRawInputData", SetLastError = true)]
		public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, IntPtr pData, ref int pcbSize, int cbSizeHeader);

		//[DllImport("user32.dll", EntryPoint = "GetKeyboardState", SetLastError = true)]
		//public static extern bool GetKeyboardState(ref byte lpKeyState);

		[DllImport("user32.dll", EntryPoint = "GetKeyState", SetLastError = true)]
		public static extern short GetKeyState(int nVirtKey);
	}

	partial class util
	{
		public const int HWND_MESSAGE = -3;

		public const int WM_CREATE = 0x0001; // 1
		public const int WM_CLOSE = 0x0010; // 16

		public const int WM_INPUT = 0x00FF; // 255
		public const int WM_INPUT_DEVICE_CHANGE = 0x00FE; // 254

		public const ushort HID_USAGEPAGE_GENERIC = 0x1;
		public const ushort HID_USAGE_MOUSE = 0x2;
		public const ushort HID_USAGE_KEYBOARD = 0x6;
	}

	/// <summary>RawInput device flags.</summary>
	[Flags]
	enum RawInputDeviceFlags : int
	{
		/// <summary>No flags.</summary>
		None = 0,
		/// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
		Remove = 0x1,
		/// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
		Exclude = 0x10,
		/// <summary>If set, this specifies all devices whose top level collection is from the specified usUsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
		PageOnly = 0x20,
		/// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
		NoLegacy = 0x30,
		/// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
		InputSink = 0x100,
		/// <summary>If set, the mouse button click does not activate the other window.</summary>
		CaptureMouse = 0x200,
		/// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
		NoHotKeys = 0x200,
		/// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
		AppKeys = 0x400
	}

	/// <summary> Enumeration containing the type device the raw input is coming from. </summary>
	enum RawInputType : int
	{
		/// <summary> Mouse input. </summary>
		Mouse = 0,
		/// <summary> Keyboard input. </summary>
		KeyBoard = 1,
		/// <summary> Another device that is not the keyboard or the mouse. </summary>
		HID = 2
	}

	/// <summary> Enumeration contanining the command types to issue. </summary>
	enum RawInputCommand : int
	{
		/// <summary> Get input data. </summary>
		Input = 0x10000003,
		/// <summary> Get header data. </summary>
		Header = 0x10000005
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RawInput
	{
		public RAWINPUTHEADER Header;
		public Union Data;
		[StructLayout(LayoutKind.Explicit)]
		public struct Union
		{
			[FieldOffset(0)]
			public RAWMOUSE Mouse;
			[FieldOffset(0)]
			public RAWKEYBOARD Keyboard;
			[FieldOffset(0)]
			public RAWHID HID;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RAWINPUTHEADER
	{
		[MarshalAs(UnmanagedType.U4)]
		public int dwType;
		[MarshalAs(UnmanagedType.U4)]
		public int dwSize;
		public IntPtr hDevice;
		public IntPtr wParam;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RAWHID
	{
		[MarshalAs(UnmanagedType.U4)]
		public int dwSizeHID;
		[MarshalAs(UnmanagedType.U4)]
		public int dwCount;
		public IntPtr pData;
		public int Length
		{
			get { return dwSizeHID; }
		}
		public byte[] data(int index)
		{
			byte[] result = new byte[1];
			if (((dwCount > 0) && (index < dwSizeHID)))
			{
				result = new byte[dwCount];
				Marshal.Copy(pData, result, Convert.ToInt32(index * dwCount), dwCount);
			}
			return result;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct BUTTONSSTR
	{
		[MarshalAs(UnmanagedType.U2)]
		public ushort usButtonFlags;
		[MarshalAs(UnmanagedType.U2)]
		public ushort usButtonData;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct RAWMOUSE
	{
		[MarshalAs(UnmanagedType.U2)]
		[FieldOffset(0)]
		public ushort usFlags;
		[MarshalAs(UnmanagedType.U4)]
		[FieldOffset(4)]
		public uint ulButtons;
		[FieldOffset(4)]
		public BUTTONSSTR buttonsStr;
		[MarshalAs(UnmanagedType.U4)]
		[FieldOffset(8)]
		public uint ulRawButtons;
		[FieldOffset(12)]
		public int lLastX;
		[FieldOffset(16)]
		public int lLastY;
		[MarshalAs(UnmanagedType.U4)]
		[FieldOffset(20)]
		public uint ulExtraInformation;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RAWKEYBOARD
	{
		[MarshalAs(UnmanagedType.U2)]
		public ushort MakeCode;
		[MarshalAs(UnmanagedType.U2)]
		public ushort Flags;
		[MarshalAs(UnmanagedType.U2)]
		public ushort Reserved;
		[MarshalAs(UnmanagedType.U2)]
		public ushort VKey;
		[MarshalAs(UnmanagedType.U4)]
		public uint Message;
		[MarshalAs(UnmanagedType.U4)]
		public uint ExtraInformation;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RAWINPUTDEVICE
	{
		[MarshalAs(UnmanagedType.U2)]
		public ushort usUsagePage;
		[MarshalAs(UnmanagedType.U2)]
		public ushort usUsage;
		[MarshalAs(UnmanagedType.U4)]
		public int dwFlags;
		public IntPtr hwndTarget;
	}
}
