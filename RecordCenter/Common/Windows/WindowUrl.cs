using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace bfbd.Common.Windows
{
	class WindowUrl
	{
		private Uri _uri = null;

		public WindowUrl(Uri uri)
		{
			_uri = uri;
		}

		internal Uri Uri { get { return _uri; } }

		public bool IsFile { get { return _uri.IsFile; } }

		public string HostSimpleName
		{
			get
			{
				if (_uri.IsFile)
				{
					return _uri.Scheme;
				}
				else
				{
					string name = _uri.Host;
					name = name.StartsWith("www.") ? name.Substring(4) : name;
					name = name.EndsWith(".com") ? name.Substring(0, name.Length - 4) : name;
					return name;
				}
			}
		}

		public string HostName
		{
			get
			{
				if (_uri.IsFile)
					return Path.GetPathRoot(_uri.AbsolutePath).TrimEnd('\\');
				else
					return _uri.Host;
			}
		}

		public string AbsoluteUri { get { return _uri.AbsoluteUri; } }

		public static WindowUrl Create(string url)
		{
			WindowUrl host = null;
			Uri uri;
			if (Uri.TryCreate(url, UriKind.Absolute, out uri))
			{
				host = new WindowUrl(uri);
			}
			else if (Uri.TryCreate("http://" + url, UriKind.Absolute, out uri))
			{
				if (uri.Host.Contains("."))
					host = new WindowUrl(uri);
			}
			return host;
		}
	}
}
