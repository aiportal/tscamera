using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.Linq
{
	#region Switch
	static class SwithCaseExtension
	{
		public class SwithCase<T>
		{
			public SwithCase(T value) { Value = value; }
			public T Value { get; private set; }
		}

		public static SwithCase<T> Switch<T>(this T value, Action nullAction = null)
		{
			if (value != null)
				return new SwithCase<T>(value);
			else if (nullAction != null)
				nullAction();
			return null;
		}
		
		public static SwithCase<T> Case<T>(this SwithCase<T> sc, T option, Action<T> action = null, bool bBreak= true)
		{
			if (sc == null) return null;
			if (action == null) return sc;
			if (sc.Value.Equals(option))
			{
				action(sc.Value);
				return bBreak ? null : sc;
			}
			return sc;
		}
		
		public static SwithCase<T> Case<T>(this SwithCase<T> sc, Func<T, bool> func, Action<T> action = null, bool bBreak = true)
		{
			if (sc == null) return null;
			if (action == null) return sc;
			if (func(sc.Value))
			{
				action(sc.Value);
				return bBreak ? null : sc;
			}
			return sc;
		}
		
		public static void Default<T>(this SwithCase<T> sc, Action<T> action)
		{
			if (sc == null) return;
			action(sc.Value);
		}
	}
	#endregion Switch

	#region Map
	static class MapExtension
	{
		public class MapResult<T, R>
		{
			public MapResult(T value, R result) { Value = value; Result = result; }
			public T Value { get; private set; }
			public R Result { get; internal set; }
			public bool Success { get; internal set; }
			public static implicit operator R(MapResult<T, R> mc) { return mc.Result; }
		}

		public static MapResult<T, R> Map<T, R>(this T value, R defaultResult = default(R)) { return new MapResult<T, R>(value, defaultResult); }

		public static MapResult<T, R> To<T, R>(this MapResult<T, R> mr, R result, params T[] vals)
		{
			if (mr.Success) return mr;

			var comparer = EqualityComparer<T>.Default;
			if (vals != null && Array.Exists(vals, v => comparer.Equals(v ,mr.Value)))
				mr.Result = result;
			return mr;
		}
		
		public static MapResult<T, R> To<T, R>(this MapResult<T, R> mr, R result, Func<T, bool> func)
		{
			if (mr.Success) return mr;
			if (func != null && func(mr.Value))
				mr.Result = result;
			return mr;
		}
	}
	#endregion Map
}
