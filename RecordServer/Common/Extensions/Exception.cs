using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common
{
	static class ExceptionExtension
	{
		public static string GetInnerMessage(this Exception ex)
		{
			while (ex.InnerException != null)
				ex = ex.InnerException;
			return ex.Message;
		}
	}
}
