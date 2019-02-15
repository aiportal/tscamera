using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace bfbd.MiniWeb.Core
{
	static class ScriptEngine
	{
		public static string GenerateServiceScript(string name, object svc)
		{
			var members = GenerateFunctions(svc);
			var implement = PROXY_IMPLEMENT;
			var script = string.Format(@"
var {0} = function (type) {{
    this.name = '{0}.svc';
    this.type = type ? type : 'GET';
{1}
}};
{0}.prototype = {2};"
				, name, members, implement);
			return script;
		}

		private static string GenerateFunctions(object svc)
		{
			///> exclude parameters of RawRequestAttribute, RawResponseAttribute, RawParametersAttribute
			StringBuilder sb = new StringBuilder();
			var ms = svc.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			foreach(MethodInfo mi in ms)
			{
				sb.AppendFormat(@"this.{0} = function (p, onResult, onError) {{ return this.implement('{0}', p, onResult, onError); }};", mi.Name);
				sb.AppendLine();
			
				var ps = new List<string>();
				foreach (var p in mi.GetParameters())
					ps.Add(p.Name + ":''");
				sb.AppendFormat(@"this.{0}.params = {{ {1} }};", mi.Name, string.Join(",", ps.ToArray()));
				sb.AppendLine();
			}
			return sb.ToString();
		}

		private const string PROXY_IMPLEMENT = @"
{
    ajaxInvoke: function (url, prams, onResult, onError) {
        $.support.cors = true;
        $.ajax({
            url: url,
            type: this.type,
            data: prams,
            cache: false,
            dataType: 'json',
            success: function (data) {
                if (!data || !data.Exception)
                    onResult(data);
                else if (onError)
                    onError(data);					
            },
            error: function (request) {
                if (onError)
                    onError(request.responseText);
            }
        });
    },
    implement: function (method, prams, onResult, onError) {
        var url = this.name + '?$m=' + method;
        for (var attr in prams)
            prams[attr] = prams[attr] ? escape(prams[attr]) : prams[attr];
        if (onResult) {
            this.ajaxInvoke(url, prams, onResult, onError);
        }
        else {
            for (var attr in prams)
                url += prams[attr] ? '&' + attr + '=' + prams[attr] : '';
            return url;
        }
    }
}";
	}
}
