using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace bfbd.Common
{
	public static partial class Encryption
	{
		public static string Encrypt(this string str)
		{
			return string.IsNullOrEmpty(str) ? str : Encrypt(str, SecureKey, SecureVi);
		}

		public static string Decrypt(this string str)
		{
			return string.IsNullOrEmpty(str) ? str : Decrypt(str, SecureKey, SecureVi);
		}

		public static byte[] Encrypt(this byte[] data)
		{
			return data == null ? data : Encrypt(data, SecureKey, SecureVi);
		}

		public static byte[] Decrypt(this byte[] data)
		{
			return data == null ? data : Decrypt(data, SecureKey, SecureVi);
		}

		public static string MD5(this string str, string salt = null)
		{
			return str == null ? str : MD5(string.Format("{1}{0}{1}", str, salt), Encoding.UTF8);
		}
	}

	static partial class Encryption
	{
		private static readonly byte[] SecureKey = Convert.FromBase64String("OWtu4Z2VT3mHgx9qb5kReA==");
		private static readonly byte[] SecureVi = Convert.FromBase64String("zPOrlQZDqhI=");

		/// <summary>  
		/// TripleDES加密  
		/// </summary>  
		/// <param name="data">待加密的字符数据</param>  
		/// <param name="key">密匙，长度可以为：128位(byte[16])，192位(byte[24])</param>  
		/// <param name="iv">iv向量，长度必须为64位（byte[8]）</param>  
		/// <returns>加密后的字符</returns>
		private static string Encrypt(string data, Byte[] key, Byte[] iv)
		{
			Byte[] tmp = null;
			System.Security.Cryptography.TripleDES tripleDes = System.Security.Cryptography.TripleDES.Create();
			ICryptoTransform encryptor = tripleDes.CreateEncryptor(key, iv);
			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
				{
					StreamWriter writer = new StreamWriter(cs);
					writer.Write(data);
					writer.Flush();
				}
				tmp = ms.ToArray();
			}
			return Convert.ToBase64String(tmp);
		}

		/// <summary>  
		/// TripleDES解密  
		/// </summary>  
		/// <param name="data">待加密的字符数据</param>  
		/// <param name="key">密匙，长度可以为：128位(byte[16])，192位(byte[24])</param>  
		/// <param name="iv">iv向量，长度必须为64位（byte[8]）</param>  
		/// <returns>加密后的字符</returns>  
		private static string Decrypt(string data, Byte[] key, Byte[] iv)
		{
			Byte[] tmp = Convert.FromBase64String(data);
			string result = string.Empty;

			System.Security.Cryptography.TripleDES tripleDES = System.Security.Cryptography.TripleDES.Create();
			ICryptoTransform decryptor = tripleDES.CreateDecryptor(key, iv);
			using (MemoryStream ms = new MemoryStream(tmp))
			{
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				{
					StreamReader reader = new StreamReader(cs);
					result = reader.ReadLine();
				}
			}
			tripleDES.Clear();
			return result;
		}

		/// <summary>  
		/// TripleDES加密  
		/// </summary>  
		/// <param name="data">待加密的数据</param>  
		/// <param name="key">密匙，长度可以为：128位(byte[16])，192位(byte[24])</param>  
		/// <param name="iv">iv向量，长度必须为64位（byte[8]）</param>  
		/// <returns>加密后的字符</returns>
		private static byte[] Encrypt(byte[] data, Byte[] key, Byte[] iv)
		{
			Byte[] result = null;

			System.Security.Cryptography.TripleDES tripleDes = System.Security.Cryptography.TripleDES.Create();
			ICryptoTransform encryptor = tripleDes.CreateEncryptor(key, iv);
			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
				{
					cs.Write(data, 0, data.Length);
					cs.Flush();	//这句很重要，在对流操作结束后必须用这句话强制将缓冲区中的数据全部写入到目标对象中  
				}
				result = ms.ToArray();
			}
			return result;
		}

		/// <summary>  
		/// TripleDES解密  
		/// </summary>  
		/// <param name="data">待加密的数据</param>  
		/// <param name="key">密匙，长度可以为：128位(byte[16])，192位(byte[24])</param>  
		/// <param name="iv">iv向量，长度必须为64位（byte[8]）</param>  
		/// <returns>加密后的字符</returns>  
		private static byte[] Decrypt(byte[] data, Byte[] key, Byte[] iv)
		{
			//Byte[] tmp = Convert.FromBase64String(data);
			byte[] result = new byte[] { };

			System.Security.Cryptography.TripleDES tripleDES = System.Security.Cryptography.TripleDES.Create();
			ICryptoTransform decryptor = tripleDES.CreateDecryptor(key, iv);
			using (MemoryStream ms = new MemoryStream(data))
			{
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				{
					result = new BinaryReader(cs).ReadBytes(8192);
				}
			}
			tripleDES.Clear();
			return result;
		}

		private static string MD5(string str, Encoding encoding)
		{
			using (System.Security.Cryptography.MD5CryptoServiceProvider provider = new System.Security.Cryptography.MD5CryptoServiceProvider())
			{
				byte[] bs = encoding.GetBytes(str);
				bs = provider.ComputeHash(bs);
				return BitConverter.ToString(bs).Replace("-", "");
			}
		}
	}
}
