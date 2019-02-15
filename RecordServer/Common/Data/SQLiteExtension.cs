using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace bfbd.Common.Data.SQLite
{
	[Obsolete]
	[SQLiteFunction(Name = "MD5", Arguments = 1, FuncType = FunctionType.Scalar)]
	public class MD5 : SQLiteFunction
	{
		public override object Invoke(object[] args)
		{
			return Encryption.MD5(args[0] as string);
		}
	}

	[Obsolete]
	[SQLiteFunction(Name = "Trim", Arguments = 1, FuncType = FunctionType.Scalar)]
	public class Trim : SQLiteFunction
	{
		public override object Invoke(object[] args)
		{
			var str = args[0] as string;
			return str == null ? null : str.Trim();
		}
	}

}
