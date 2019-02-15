using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace bfbd.Common.Data.SQLite
{
	[SQLiteFunction(Name = "MD5", Arguments = 1, FuncType = FunctionType.Scalar)]
	public class MD5 : SQLiteFunction
	{
		public override object Invoke(object[] args)
		{
			return Encryption.MD5(args[0] as string);
		}
	}

	[SQLiteFunction(Name = "Trim", Arguments = 1, FuncType = FunctionType.Scalar)]
	public class Trim : SQLiteFunction
	{
		public override object Invoke(object[] args)
		{
			var str = args[0] as string;
			return str == null ? null : str.Trim();
		}
	}


	//[SQLiteFunction(Name = "TotalTime", Arguments = 2, FuncType = FunctionType.Aggregate)]
	//public class TotalTime : SQLiteFunction
	//{
	//    public Dictionary<string, TimeSpan> Dic = new Dictionary<string, TimeSpan>();
	//    public string Prev;
	//    public DateTime Min;
	//    public DateTime Max;

	//    public override void Step(object[] args, int stepNumber, ref object contextData)
	//    {
	//        if (contextData == null)
	//            contextData = new Context();
	//        var c = contextData as Context;
	//        var name = args[0] as string;
	//        DateTime time = DateTime.Parse(args[1] as string);

	//        if (name != c.Prev)
	//        {
	//            if (!c.Dic.ContainsKey(c.Prev) )
	//                c.Dic[c.Prev] = new TimeSpan(0,0,0);
	//            c.Dic[c.Prev].Add(c.Max- c.Min);

	//            c.Prev = name == null ? string.Empty : name;
	//            c.Min = c.Max = time;
	//        }
	//        else
	//        {
	//            c.Max = time > c.Max ? time : c.Max;
	//        }
	//    }

	//    public override object Final(object contextData)
	//    {
	//        Context c = contextData as Context;
	//        return null;
	//    }

	//    class Context
	//    {
	//        public Dictionary<string, TimeSpan> Dic = new Dictionary<string, TimeSpan>();
	//        public string Prev = string.Empty;
	//        public DateTime Min;
	//        public DateTime Max;

	//        public void Reset(string name, DateTime time)
	//        {
	//            Prev = name;
	//            Min = Max = time;
	//        }
	//    }
	//}
}
