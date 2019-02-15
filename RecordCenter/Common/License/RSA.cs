using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace bfbd.Common.License
{
	static partial class RSA
	{
		public static string Encrypt(string data, string keyName)
		{
			return string.IsNullOrEmpty(data) ? data : RsaEncrypt(data, KeyStorage.GetPrivateKey(keyName));
		}

		public static string Decrypt(string data, string keyName)
		{
			return string.IsNullOrEmpty(data) ? data : RsaDecrypt(data, KeyStorage.GetPublicKey(keyName));
		}

		public static string SignXml(string xml, string keyName = null)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			RsaSign(doc, KeyStorage.GetPrivateKey(keyName));
			return doc.OuterXml;
		}

		public static bool VerifyXml(string xml, string keyName = null)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			return RsaVerify(doc, KeyStorage.GetPublicKey(keyName));
		}
	}

	static partial class RSA
	{
		private static XmlDocument RsaSign(XmlDocument doc, string privateKey)
		{
			RSACryptoServiceProvider key = new RSACryptoServiceProvider();
			key.FromXmlString(privateKey);

			SignedXml signedXml = new SignedXml(doc);
			signedXml.SigningKey = key;
			Reference reference = new Reference();
			reference.Uri = "";
			reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
			signedXml.AddReference(reference);
			signedXml.ComputeSignature();
			XmlElement signElement = signedXml.GetXml();

			doc.DocumentElement.AppendChild(doc.ImportNode(signElement, true));
			return doc;
		}

		private static bool RsaVerify(XmlDocument doc, string publicKey)
		{
			bool verify = false;
			RSACryptoServiceProvider key = new RSACryptoServiceProvider();
			key.FromXmlString(publicKey);

			SignedXml signedXml = new SignedXml(doc);
			XmlNodeList nodeList = doc.GetElementsByTagName("Signature");
			if (nodeList.Count == 1)
			{
				signedXml.LoadXml((XmlElement)nodeList[0]);
				verify = signedXml.CheckSignature(key);
			}
			return verify;
		}

		private static string RsaEncrypt(string data, string privateKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(privateKey);
			if (data.Length < (rsa.KeySize / 8 - 11))
			{
				BigInteger biD = new BigInteger(rsa.ExportParameters(true).D);
				BigInteger biN = new BigInteger(rsa.ExportParameters(true).Modulus);
				return EncryptString(data, biD, biN);
			}
			else
				throw new ArgumentOutOfRangeException("Data is too long to encrypt by rsa.");
		}

		private static string RsaDecrypt(string data, string publicKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(publicKey);
			BigInteger biE = new BigInteger(rsa.ExportParameters(false).Exponent);
			BigInteger biN = new BigInteger(rsa.ExportParameters(false).Modulus);
			return DecryptString(data, biE, biN);
		}

		private static string EncryptString(string source, BigInteger d, BigInteger n)
		{
			int len = source.Length;
			int len1 = 0;
			int blockLen = 0;
			if ((len % 128) == 0)
				len1 = len / 128;
			else
				len1 = len / 128 + 1;
			
			string block = "";
			List<string> vals = new List<string>();
			for (int i = 0; i < len1; i++)
			{
				if (len >= 128)
					blockLen = 128;
				else
					blockLen = len;
				block = source.Substring(i * 128, blockLen);
				byte[] oText = System.Text.Encoding.Default.GetBytes(block);
				BigInteger biText = new BigInteger(oText);
				BigInteger biEnText = biText.modPow(d, n);
				vals.Add(Convert.ToBase64String(biEnText.getBytes()));
				len -= blockLen;
			}
			return string.Join("@", vals.ToArray());
		}

		private static string DecryptString(string encryptString, BigInteger e, BigInteger n)
		{
			StringBuilder result = new StringBuilder();
			string[] array = encryptString.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				byte[] block = Convert.FromBase64String(array[i]);
				BigInteger biText = new BigInteger(block);
				BigInteger biEnText = biText.modPow(e, n);
				string temp = System.Text.Encoding.Default.GetString(biEnText.getBytes());
				result.Append(temp);
			}
			return result.ToString();
		}
	}
}
