using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace bfbd.MiniWeb.Core
{
	using bfbd.Common;
	using ICSharpCode.SharpZipLib.Zip;
	using ICSharpCode.SharpZipLib.Core;

	partial class ZipStorage
	{
		private string _pkgLocation;
		private WebCache _webCache = new WebCache();

		public ZipStorage(string packageName = "web.zip")
		{
			string fpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, packageName);
			if (File.Exists(fpath))
			{
				_pkgLocation = fpath;
			}
			else
			{
				var assembly = System.Reflection.Assembly.GetEntryAssembly();
				_pkgLocation = Array.Find(assembly.GetManifestResourceNames(), s => s.EndsWith(packageName));
			}
		}

		public bool Exists(string fpath)
		{
			this.Assert(!string.IsNullOrEmpty(fpath));
			if (fpath.StartsWith("/"))
				fpath = fpath.TrimStart('/');

			bool exists = false;
			try
			{
				exists = _webCache.Contains(fpath);
				if (!exists)
				{
					//if (File.Exists(_pkgLocation))
					if (Path.IsPathRooted(_pkgLocation))
					{
						using (FileStream fs = new FileStream(_pkgLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
							exists = ZipStream.IsExists(fs, fpath);
					}
					else
					{
						var assembly = System.Reflection.Assembly.GetEntryAssembly();
						var rs = assembly.GetManifestResourceStream(_pkgLocation);
							exists = ZipStream.IsExists(rs, fpath);
					}
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return exists;
		}

		public byte[] Extract(string fpath)
		{
			this.Assert(!string.IsNullOrEmpty(fpath));
			if (fpath.StartsWith("/"))
				fpath = fpath.TrimStart('/');

			byte[] content = null;
			try
			{
				content = _webCache.Extract(fpath);
				if (content == null)
				{
					//if (File.Exists(_pkgLocation))
					if (Path.IsPathRooted(_pkgLocation))
					{
						using (FileStream fs = new FileStream(_pkgLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
							content = ZipStream.Extract(fs, fpath);
					}
					else
					{
						var assembly = System.Reflection.Assembly.GetEntryAssembly();
						var rs = assembly.GetManifestResourceStream(_pkgLocation);
						content = ZipStream.Extract(rs, fpath);
					}
					if (content != null)
						_webCache.Add(fpath, content);
				}
			}
			catch (Exception ex) { TraceLog.WriteException(ex); throw; }
			return content;
		}
	}

	partial class ZipStorage
	{
		#region ZipStream

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

		#endregion ZipStream
	}

	partial class ZipStorage
	{
		#region WebCache

		class WebCache
		{
			private Hashtable _files = Hashtable.Synchronized(new Hashtable());
			private System.Web.Caching.Cache _cache = System.Web.HttpRuntime.Cache;

			public void Add(string fpath, byte[] content)
			{
				this.Assert(!string.IsNullOrEmpty(fpath) && content != null);

				string key = _files.ContainsKey(fpath) ? _files[fpath] as string : Guid.NewGuid().ToString("n");
				_cache[key] = content;
				_files[fpath] = key;
			}

			public bool Contains(string fpath)
			{
				return _files.ContainsKey(fpath);
			}

			public byte[] Extract(string fpath)
			{
				var fkey = _files[fpath] as string;
				return (fkey != null) ? _cache[fkey] as byte[] : null;
			}
		}

		#endregion WebCache
	}
}
