using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace bfbd.MiniWeb
{
	using ICSharpCode.SharpZipLib.Zip;
	using ICSharpCode.SharpZipLib.Core;

	class FileStorage
	{
		public readonly static string WebPackage = Path.Combine(Application.StartupPath, "web.zip");

		private Hashtable _files = Hashtable.Synchronized(new Hashtable());
		private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

		public bool Exists(string fpath)
		{
			Debug.Assert(!string.IsNullOrEmpty(fpath));
			fpath = fpath.TrimStart('/');
			bool exists = false;
			try
			{
				exists = _files.ContainsKey(fpath);
				if (!exists)
				{
					if (File.Exists(WebPackage))
					{
						using (FileStream fs = new FileStream(WebPackage, FileMode.Open, FileAccess.Read, FileShare.Read))
							exists = ZipStream.IsExists(fs, fpath);
					}
					else
					{
						// package not exists.
						using (MemoryStream fs = new MemoryStream(Resources.ResourceManager.GetObject("web") as byte[]))
							exists = ZipStream.IsExists(fs, fpath);
					}
				}
				if (exists)
				{
					if (!_files.ContainsKey(fpath))
						_files[fpath] = Guid.NewGuid().ToString();
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return exists;
		}

		public byte[] Extract(string fpath)
		{
			Debug.Assert(!string.IsNullOrEmpty(fpath));
			fpath = fpath.TrimStart('/');
			byte[] content = null;
			try
			{
				var fkey = _files[fpath] as string;
				content = _files.ContainsKey(fpath) ? _cache[fkey] as byte[] : null;
				if (content == null)
				{
					if (File.Exists(WebPackage))
					{
						using (FileStream fs = new FileStream(WebPackage, FileMode.Open, FileAccess.Read, FileShare.Read))
							content = ZipStream.Extract(fs, fpath);
					}
					else
					{
						// package not exists.
						using (MemoryStream fs = new MemoryStream(Resources.ResourceManager.GetObject("web") as byte[]))
							content = ZipStream.Extract(fs, fpath);
					}
					// file not exists in _cache.
					if (content != null)
					{
						string key = _files.ContainsKey(fpath) ? _files[fpath] as string : Guid.NewGuid().ToString();
						_cache[key] = content;
						_files[fpath] = key;
					}
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return content;
		}


		//private byte[] FindWebContent()
		//{
		//    // find web content from EntryAssembly.
		//    byte[] web = null;
		//    Assembly assembly = Assembly.GetEntryAssembly();
		//    string[] names = assembly.GetManifestResourceNames();
		//    foreach(string name in names)
		//    {
		//        System.Resources.ResourceManager mgr = new System.Resources.ResourceManager(name, assembly);
		//        web = mgr.GetObject("web") as byte[];
		//        if (web != null)
		//            break;
		//    }
		//    return web;
		//}
	}

	static class ZipStream
	{
		private static object _syncRoot = new object();
		private static byte[] _lockedTempBuffer = new byte[4096];

		public static bool IsExists(Stream zs, string fpath)
		{
			using (ZipFile zip = new ZipFile(zs))
			{
				return zip.FindEntry(fpath, true) >= 0;
			}
		}

		public static byte[] Extract(Stream zs, string fpath)
		{
			byte[] content = null;
			using (ZipFile zip = new ZipFile(zs))
			{
				ZipEntry entry = zip.GetEntry(fpath);
				if (entry != null)
				{
					using (MemoryStream ms = new MemoryStream())
					{
						Stream fs = zip.GetInputStream(entry);
						lock (_syncRoot)
						{
							StreamUtils.Copy(fs, ms, _lockedTempBuffer);
						}
						content = ms.ToArray();
					}
				}
			}
			return content;
		}
	}

}
