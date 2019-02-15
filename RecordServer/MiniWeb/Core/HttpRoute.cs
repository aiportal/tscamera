using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace bfbd.MiniWeb.Core
{
	/// <summary>
	/// Route url to other
	/// </summary>
 	partial class HttpRoute : IHttpHandler
	{
		public bool IsMatch(HttpListenerRequest Request, string prefix)
		{
			string url = Request.GetFilePath(prefix);
			return _rules.Exists(r => Regex.IsMatch(url, r.Pattern));
		}

		public void ProcessRequest(HttpListenerRequest Request, HttpListenerResponse Response, string prefix = null)
		{
			string url = Request.Url.AbsolutePath;
			var rule = _rules.Find(r => Regex.IsMatch(url, r.Pattern));
			url = Regex.Replace(url, rule.Pattern, rule.Replace);
			Response.Redirect(url);
		}
	}

	partial class HttpRoute
	{
		List<Rule> _rules = new List<Rule>();

		public void AddRule(string pattern, string replace)
		{
			_rules.Add(new Rule() { Pattern = pattern, Replace = replace });
		}

		class Rule
		{
			public string Pattern;
			public string Replace;
		}
	}
}
