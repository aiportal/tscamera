using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Linq
{
	static class StringHelper
	{
		public static bool IgnoreCaseEquals(string x, string y)
		{
			return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
		}

		public static IEqualityComparer<string> IgnoreCaseComparer
		{
			get { return new IgnoreCaseComparerClass(); }
		}

		private class IgnoreCaseComparerClass : IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode(string obj)
			{
				return obj.GetHashCode();
			}
		}
	}
}
