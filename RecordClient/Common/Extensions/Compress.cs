using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace bfbd.Common
{
	public static partial class Compress
	{
		public static byte[] GZip(this byte[] data, bool compress)
		{
			if (compress)
			{
				using (MemoryStream ms = new MemoryStream())
				{
					using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress))
					{
						zs.Write(data, 0, data.Length);
						zs.Flush();
					}
					return ms.ToArray();
				}
			}
			else
			{
				using (MemoryStream ms = new MemoryStream(data))
				using (MemoryStream md = new MemoryStream())
				{
					using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress))
					{
						byte[] buf = new byte[4096];
						int len = 0;
						while ((len = zs.Read(buf, 0, buf.Length)) > 0)
						{
							md.Write(buf, 0, len);
						}
					}
					return md.ToArray();
				}
			}
		}

		public static byte[] Deflate(this byte[] data, bool compress)
		{
			if (compress)
			{
				using (MemoryStream ms = new MemoryStream())
				{
					using (DeflateStream zs = new DeflateStream(ms, CompressionMode.Compress))
					{
						zs.Write(data, 0, data.Length);
						zs.Flush();
					}
					return ms.ToArray();
				}
			}
			else
			{
				using (MemoryStream ms = new MemoryStream(data))
				using (MemoryStream md = new MemoryStream())
				{
					using (DeflateStream zs = new DeflateStream(ms, CompressionMode.Decompress))
					{
						byte[] buf = new byte[4096];
						int len = 0;
						while ((len = zs.Read(buf, 0, buf.Length)) > 0)
						{
							md.Write(buf, 0, len);
						}
					}
					return md.ToArray();
				}
			}
		}
	}
}
