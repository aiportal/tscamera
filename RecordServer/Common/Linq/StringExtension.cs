using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Linq
{
	static class StringExtension
	{
		public static bool HasValue(this string s) { return !string.IsNullOrEmpty(s); }

		public static IEnumerable<string> Except(this IEnumerable<string> first, IEnumerable<string> second, bool ignoreCase)
		{
			if (first == null)
				throw new ArgumentNullException("first");
			if (second == null)
				throw new ArgumentNullException("second");

			var list = new List<string>(second);
			foreach (var elem in first)
			{
				bool skip;
				if (ignoreCase)
					skip = list.Exists(s => string.Equals(s, elem, StringComparison.OrdinalIgnoreCase));
				else
					skip = list.Contains(elem);

				if (!skip)
					yield return elem;
			}
		}

		public static IEnumerable<string> Merge(this IEnumerable<string> first, IEnumerable<string> second, bool ignoreCase)
		{
			if (first == null)
				throw new ArgumentNullException("first");
			if (second == null)
				throw new ArgumentNullException("second");

			foreach (var elem in first)
				yield return elem;

			var list = new List<string>(first);
			foreach (var elem in second)
			{
				bool skip;
				if (ignoreCase)
					skip = list.Exists(s => string.Equals(s, elem, StringComparison.OrdinalIgnoreCase));
				else
					skip = list.Contains(elem);

				if (!skip)
					yield return elem;
			}
		}
	}
}
