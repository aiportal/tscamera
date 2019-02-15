using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.ComponentModel;

namespace bfbd.TSCamera.Core
{
	using bfbd.Common;

	partial class SharedMemory : IDisposable
	{
		private string _name = string.Empty;
		private Mutex _mutex = null;
		private IntPtr _handle = IntPtr.Zero;
		private IntPtr _memory = IntPtr.Zero;

		#region Dispose

		public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
			if (disposing)
			{
				_mutex.Close();
				_mutex = null;
			}
			if (_memory != IntPtr.Zero)
			{
				Kernel32.UnmapViewOfFile(this._memory);
				_memory = IntPtr.Zero;
			}
			if (_handle != IntPtr.Zero)
			{
				Kernel32.CloseHandle(this._handle);
				_handle = IntPtr.Zero;
			}
        }

		~SharedMemory()
        {
            this.Dispose(false);
		}

		#endregion Dispose

		#region Constructor

		public SharedMemory(string name, int capcity, bool global)
		{
			this.Assert(!string.IsNullOrEmpty(name));
			this.Assert(capcity > 0);

			try
			{
				_mutex = new Mutex(false, string.Format(@"{1}\{0}_Mutex", name, global ? "Global" : "Local"));

				this._handle = Kernel32.CreateFileMapping((IntPtr)(-1), IntPtr.Zero, ProtectionLevel.PAGE_READWRITE, 0, capcity + 8, name);
				if (this._handle == IntPtr.Zero)
					throw new Win32Exception((int)Kernel32.GetLastError()) { Source = "SharedMemory" };

				this._memory = Kernel32.MapViewOfFile(_handle, FileMap.FILE_MAP_ALL_ACCESS, 0, 0, 0);
				if (this._memory == IntPtr.Zero)
					throw new Win32Exception((int)Kernel32.GetLastError()) { Source = "SharedMemory" };

				MutexInvoke(() => Marshal.WriteInt32(_memory, capcity));
				this.Capcity = capcity;
				this._name = name;
			}
			catch (Exception ex)
			{
				if (this._handle != IntPtr.Zero)
				{
					Kernel32.CloseHandle(this._handle);
					this._handle = IntPtr.Zero;
				}

				TraceLog.WriteException(ex); throw;
			}
		}

		public SharedMemory(string name, bool global = true, bool readOnly = true)
		{
			this.Assert(!string.IsNullOrEmpty(name));
			try
			{
				_mutex = new Mutex(false, string.Format(@"{1}\{0}_Mutex", name, global ? "Global" : "Local"));

				this._handle = Kernel32.OpenFileMapping(FileMap.FILE_MAP_READ, true, name);
				if (this._handle == IntPtr.Zero)
					throw new Win32Exception((int)Kernel32.GetLastError()) { Source = "SharedMemory" };

				this._memory = Kernel32.MapViewOfFile(this._handle, readOnly ? FileMap.FILE_MAP_READ : FileMap.FILE_MAP_ALL_ACCESS, 0, 0, 0);
				if (this._memory == IntPtr.Zero)
					throw new Win32Exception((int)Kernel32.GetLastError()) { Source = "SharedMemory" };

				MutexInvoke(() => { this.Capcity = Marshal.ReadInt32(_memory); });
				this._name = name;
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
		}

		public int Capcity { get; private set; }

		#endregion Constructor
	}

	partial class SharedMemory
	{
		#region Members

		public bool SetData(byte[] data, int millisecondsTimeout = Timeout.Infinite)
		{
			if (data.Length > Capcity)
				throw new ArgumentOutOfRangeException("data", "The data to be stored is too large for the SharedMemory.");

			bool succeed = false;
			this.MutexInvoke(() =>
			{
				Marshal.WriteInt32(this._memory, 4, data.Length);

				IntPtr pData = new IntPtr(this._memory.ToInt64() + 8);
				Marshal.Copy(data, 0, pData, data.Length);

				succeed = true;

			}, millisecondsTimeout);
			return succeed;
		}

		public byte[] GetData(int millisecondsTimeout = Timeout.Infinite)
		{
			byte[] buffer = null;
			this.MutexInvoke(() =>
			{
				int size = Marshal.ReadInt32(this._memory, 4);
				buffer = new byte[size];

				IntPtr pData = new IntPtr(this._memory.ToInt64() + 8);
				Marshal.Copy(pData, buffer, 0, buffer.Length);

			}, millisecondsTimeout);
			return buffer;
		}

		private void MutexInvoke(Action action, int timeout = Timeout.Infinite)
		{
			if (_mutex.WaitOne(timeout))
			{
				try
				{
					action();
				}
				catch (Exception ex) { TraceLog.WriteException(ex); throw; }
				finally
				{
					_mutex.ReleaseMutex();
				}
			}
		}

		#endregion Members
	}

	partial class SharedMemory
	{
		#region Windows API

		[SuppressUnmanagedCodeSecurity]
		class Kernel32
		{
			[DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping", SetLastError = true)]
			internal static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr secAttributes, ProtectionLevel dwProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

			[DllImport("Kernel32.dll", EntryPoint = "OpenFileMapping", SetLastError = true)]
			internal static extern IntPtr OpenFileMapping(FileMap dwDesiredAccess, bool bInheritHandle, string lpName);

			[DllImport("Kernel32.dll", EntryPoint="MapViewOfFile", SetLastError = true)]
			internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMap dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, int dwNumberOfBytesToMap);

			[DllImport("Kernel32.dll", EntryPoint="UnmapViewOfFile", SetLastError = true)]
			internal static extern bool UnmapViewOfFile(IntPtr map);

			[DllImport("Kernel32.dll", EntryPoint = "CopyMemory")]
			internal static extern void CopyMemory(IntPtr dest, IntPtr source, int size);

			[DllImport("Kernel32.dll", EntryPoint = "GetLastError")]
			internal static extern uint GetLastError();

			[DllImport("Kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
			internal static extern bool CloseHandle(IntPtr handle);
		}

		const int ERROR_INVALID_HANDLE = 6;
		const int INVALID_HANDLE_VALUE = -1;

		enum FileMap
		{
			FILE_MAP_ALL_ACCESS = 0xf001f,
			FILE_MAP_COPY = 1,
			FILE_MAP_READ = 4,
			FILE_MAP_WRITE = 2
		}

		enum ProtectionLevel
		{
			PAGE_EXECUTE = 0x10,
			PAGE_NOACCESS = 1,
			PAGE_READONLY = 2,
			PAGE_READWRITE = 4,
			PAGE_WRITECOPY = 8
		}

		enum StdHandle
		{
			STD_ERROR_HANDLE = -12,
			STD_INPUT_HANDLE = -10,
			STD_OUTPUT_HANDLE = -11
		}

		#endregion Windows API
	}
}
