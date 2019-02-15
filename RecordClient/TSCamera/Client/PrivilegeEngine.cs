using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace bfbd.TSCamera.Client
{
	static partial class PrivilegeEngine
	{
		public static bool SetProcessPrivileges(int processId, params string[] privilegesName)
		{
			bool succeed = false;
			if (processId == 0)
				processId = Process.GetCurrentProcess().Id;

			IntPtr hProcess = kernel32.OpenProcess(MAXIMUM_ALLOWED, false, (uint)processId);
			if (hProcess != IntPtr.Zero)
			{
				IntPtr hToken;
				if (advapi32.OpenProcessToken(hProcess, TokenAccess.TOKEN_ALL_ACCESS, out hToken))
				{
					succeed = true;
					foreach (string privilege in privilegesName)
						succeed &= SetTokenPrivilege(hToken, privilege);

					kernel32.CloseHandle(hToken);
				}
				kernel32.CloseHandle(hProcess);
			}
			return succeed;
		}

		private static bool SetTokenPrivilege(IntPtr hToken, string privilegeName)
		{
			LUID luid = new LUID();
			if (advapi32.LookupPrivilegeValue(null, privilegeName, ref luid))
			{
				TOKEN_PRIVILEGES priv = new TOKEN_PRIVILEGES();
				priv.PrivilegeCount = 1;
				priv.Luid = luid;
				priv.Attributes = SE_PRIVILEGE_ENABLED;

				if (advapi32.AdjustTokenPrivileges(hToken, false, ref priv, 0, IntPtr.Zero, IntPtr.Zero))
					return true;
			}
			return false;
		}
	}

	partial class PrivilegeEngine
	{
		#region Windows API

		class advapi32
		{
			[DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
			internal static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

			[DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeValue", SetLastError = true)]
			internal static extern bool LookupPrivilegeValue(string systemName, string privilegeName, ref LUID pluid);

			[DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", SetLastError = true)]
			internal static extern bool AdjustTokenPrivileges(IntPtr hToken, bool disable, ref TOKEN_PRIVILEGES TokPriv, int len, IntPtr prev, IntPtr relen);
		}

		class kernel32
		{
			[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
			internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

			[DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
			internal static extern bool CloseHandle(IntPtr hSnapshot);
		}

		#endregion Windows API

		#region Constants

		const int SE_PRIVILEGE_ENABLED = 0x0002;
		const int TOKEN_DUPLICATE = 0x0002;
		const uint MAXIMUM_ALLOWED = 0x2000000;

		#endregion Constants

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		struct TOKEN_PRIVILEGES
		{
			public UInt32 PrivilegeCount;
			public LUID Luid;
			public UInt32 Attributes;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		struct TokenAccess
		{
			//Use these for DesiredAccess
			public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
			public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
			public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
			public const UInt32 TOKEN_DUPLICATE = 0x0002;
			public const UInt32 TOKEN_IMPERSONATE = 0x0004;
			public const UInt32 TOKEN_QUERY = 0x0008;
			public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
			public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
			public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
			public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
			public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
			public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
			public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
				TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
				TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
				TOKEN_ADJUST_SESSIONID);
		}

		#endregion Structs
	}

	/// <summary>
	/// NtPrivileges
	/// </summary>
	/// <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/bb530716(v=vs.85).aspx"/>
	partial class PrivilegeEngine
	{
		#region NtPrivileges
		
		/// <summary>
		/// 允许访问所有进程.
		/// </summary>
		public const string SE_DEBUG_NAME = "SeDebugPrivilege";
		/// <summary>
		/// 还原文件和目录,允许绕过文件及目录权限来恢复备份文件.
		/// </summary>
		public const string SE_RESTORE_NAME = "SeRestorePrivilege";
		/// <summary>
		/// 备份文件和目录,不多说了,就是翻阅遍历,执行文件,读取文件和文件夹所有信息的权限
		/// </summary>
		public const string SE_BACKUP_NAME = "SeBackupPrivilege";
		/// <summary>
		/// 以操作系统方式操作,成为操作系统的一部分.
		/// </summary>
		public const string SE_TCB_NAME = "SeTcbPrivilege";
		/// <summary>
		/// 替换进程级记号,允许初始化一个进程,以取代与已启动的子进程相关的默认令牌.
		/// </summary>
		public const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
		/// <summary>
		/// 调整进程的内存配额
		/// </summary>
		public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
		/// <summary>
		/// 身份验证后模拟客户端
		/// </summary>
		public const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";
		/// <summary>
		/// 作为服务登录
		/// </summary>
		public const string SE_SERVICE_LOGON_NAME = "SeServiceLogonRight";
	
		#endregion NtPrivileges

		#region Other Privileges

		//SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege"
		//替换进程级记号,允许初始化一个进程,以取代与已启动的子进程相关的默认令牌.

		//SE_AUDIT_NAME = "SeAuditPrivilege"
		//产生安全审核,允许将条目添加到安全日志.

		//SE_BACKUP_NAME = "SeBackupPrivilege"
		//备份文件和目录,不多说了,就是翻阅遍历,执行文件,读取文件和文件夹所有信息的权限

		//SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege"
		//跳过遍历检查,允许用户来回移动目录,但是不能列出文件夹的内容...这个权限还有个大用途...就是干坏事不留痕迹...- -!

		//SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege"
		//创建页面文件,允许用户创建和改变一个分页文件的大小

		//SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege"
		//创建永久共享对象,例如某目录...有点很多余的感觉...- -嗯...或者有时候会有用

		//SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege"
		//创建令牌对象,允许进程调用NtCreateToken()或者是其他的Token-Creating APIs创建一个访问令牌...

		//SE_DEBUG_NAME = "SeDebugPrivilege " 
		//哈这个最清楚了吧?允许访问所有进程.

		//SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege"
		//更改优先级时,只有获得此权限后才能设置进程优先级为"实时".

		//SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege"
		//调整进程的内存配额

		//SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege"
		//装载和卸载设备驱动程序,允许动态地加载和卸载设备驱动程序.安装即插即用设备的驱动程序时需要此特权.

		//SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege"
		//内存中锁定页,允许使用进程在物理内存中保存数据,从而避免系统将这些数据分页保存到磁盘的虚拟内存中.采用此策略会减少可用的随机存取内存(RAM)总数,从而可能极大地影响系统性能.

		//SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege"
		//域中添加工作站,用于识别 Active Directory 中已有的帐户和组.

		//SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege"
		//配置单一进程,允许使用性能监视工具来监视非系统进程的性能.

		//SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege"
		//从远端系统强制关机,允许从网络上的远程位置关闭计算机.

		//SE_RESTORE_NAME = "SeRestorePrivilege"
		//还原文件和目录,允许绕过文件及目录权限来恢复备份文件.

		//SE_SECURITY_NAME = "SeSecurityPrivilege"
		//管理审核和安全日志,允许指定文件,Active Directory对象和注册表项之类的单个资源的对象访问审核选项.还可以查看和清除安全日志.

		//SE_SHUTDOWN_NAME = "SeShutdownPrivilege " 
		//关闭系统,没有这个权限是关不了机的哦...

		//SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege"
		//修改固件环境值,查看,修改环境变量SET命令.

		//SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege"
		//配置系统性能,允许监视系统进程的性能.

		//SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege"
		//更改系统时间,也要权限的哦!- -

		//SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege"
		//获得文件或对象的所有权,包括 Active Directory 对象,文件和文件夹,打印机,注册表项,进程和线程.

		//SE_TCB_NAME = "SeTcbPrivilege"
		//以操作系统方式操作,成为操作系统的一部分.

		//SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege"
		//从终端设备读取未经请求的输入,这个...在策略组找不到了...是不是更新以后删除了?

		//SE_IMPERSONATE_NAME = "SeImpersonatePrivilege"
		//身份验证后模拟客户端

		//SE_MANAFE_VOLUME_NAME = "SeManageVolumePrivilege"
		//执行卷维护任务

		//SE_UNDOCK_NAME = "SeUndockPrivilege"
		//从插接工作站中取出计算机

		//SE_BATCH_LOGON_NAME = "SeBatchLogonRight"
		//作为批处理作业登录

		//SE_INTERACTIVE_LOGON_NAME = "SeInteractiveLogonRight"
		//本地登录

		//SE_NETWORK_LOGON_NAME = "SeNetworkLogonRight"
		//从网络访问此计算机

		//SE_SERVICE_LOGON_NAME = "SeServiceLogonRight"
		//作为服务登录

		#endregion Other Privileges
	}
}
