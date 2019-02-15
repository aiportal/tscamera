using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace bfbd.WindowsAPI
{
	class kernel32
	{
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
		public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageName")] 
		public static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags, StringBuilder lpExeName, out int size);

		[DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CloseHandle")]
		public static extern bool CloseHandle(IntPtr hHandle);
	}

	partial class util
	{
		//public const int PROCESS_ALL_ACCESS;  //所有能获得的权限
		public const int PROCESS_CREATE_PROCESS = 0x0080;  //需要创建一个进程
		public const int PROCESS_CREATE_THREAD = 0x0002;   //需要创建一个线程
		public const int PROCESS_DUP_HANDLE = 0x0040;      //重复使用DuplicateHandle句柄
		public const int PROCESS_QUERY_INFORMATION = 0x0400;   //获得进程信息的权限，如它的退出代码、优先级
		public const int PROCESS_QUERY_LIMITED_INFORMATION= 0x1000;  /*获得某些信息的权限，如果获得了PROCESS_QUERY_INFORMATION，也拥有PROCESS_QUERY_LIMITED_INFORMATION权限*/
		public const int PROCESS_SET_INFORMATION = 0x0200;    //设置某些信息的权限，如进程优先级
		public const int PROCESS_SET_QUOTA = 0x0100;          //设置内存限制的权限，使用SetProcessWorkingSetSize
		public const int PROCESS_SUSPEND_RESUME = 0x0800;     //暂停或恢复进程的权限
		public const int PROCESS_TERMINATE = 0x0001;          //终止一个进程的权限，使用TerminateProcess
		public const int PROCESS_VM_OPERATION = 0x0008;       //操作进程内存空间的权限(可用VirtualProtectEx和WriteProcessMemory) 
		public const int PROCESS_VM_READ = 0x0010;            //读取进程内存空间的权限，可使用ReadProcessMemory
		public const int PROCESS_VM_WRITE = 0x0020;           //读取进程内存空间的权限，可使用WriteProcessMemory
		public const int SYNCHRONIZE = 0x0020;                //等待进程终止
	}
}
