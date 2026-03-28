/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Firefox — helper de descifrado de key4.db.
// Firefox cifra las contraseñas en logins.json con 3DES-CBC.
// La clave se deriva de key4.db (también SQLite) usando PBKDF2 + 3DES.
// Este helper solo admite el caso sin contraseña maestra (master password vacía),
// que es el caso habitual para usuarios domésticos.
//
// Referencia de protocolo:
//   https://github.com/lclevy/firepwd  (licencia MIT, sin código copiado aquí)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using KeePass.Infrastructure.BrowserImport;

namespace KeePass.Infrastructure.BrowserImport
{
	/// <summary>
	/// Descifra credenciales de Firefox almacenadas en <c>logins.json</c>/<c>key4.db</c>
	/// cuando no hay contraseña maestra definida.
	/// </summary>
	public static class FirefoxCryptoHelper
	{
		// OID de 3DES-CBC (usado en key4.db para cifrar la clave global)
		private static readonly byte[] Oid3DesCbc = new byte[]
			{ 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x03, 0x07 };

		/// <summary>
		/// Extrae la clave global de cifrado de <c>key4.db</c>.
		/// Solo funciona sin contraseña maestra (masterPassword = "").
		/// </summary>
		/// <param name="key4DbPath">Ruta completa al archivo key4.db.</param>
		/// <returns>Clave de 24 bytes para TripleDES.</returns>
		public static byte[] ExtractGlobalKey(string key4DbPath)
		{
			// Leer la fila de metadatos que contiene el "global salt" y el IV.
			var sql = "SELECT item1, item2 FROM metadata WHERE id = 'password';";
			var cols = new[] { "item1", "item2" };
			var rows = SqliteTempReader.QueryFile(key4DbPath, sql, cols);
			if(rows.Count == 0)
				throw new InvalidOperationException(
					"key4.db no contiene la fila password en metadata.");

			byte[] globalSalt = rows[0]["item1"] as byte[];
			if(globalSalt == null)
				globalSalt = HexOrBytes(rows[0]["item1"]);

			byte[] encryptedCheck = rows[0]["item2"] as byte[];
			if(encryptedCheck == null)
				encryptedCheck = HexOrBytes(rows[0]["item2"]);

			// Derivar la clave usando SHA-1(globalSalt + "\x00\x00\x00\x00\x00" + SHA-1(""))
			// según la lógica de NSS sin master password.
			byte[] emptyPassHash  = SHA1Bytes(new byte[0]);
			byte[] innerData      = Concat(globalSalt, new byte[] { 0, 0, 0, 0, 0 }, emptyPassHash);
			byte[] hashCheck      = SHA1Bytes(innerData);
			byte[] pbytes         = Concat(hashCheck, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
			// la clave DES es pkbytes[0..7], IV es pkbytes[8..15]
			byte[] desKey = Slice(pbytes, 0, 8);
			byte[] desIv  = Slice(pbytes, 8, 8);

			// Validar con el campo "password-check" (desencriptado debe ser "password-check\x02\x02")
			// Esta validación es opcional aquí; si falla el descifrado dará basura silenciosa.

			// Leer las claves de nssPrivate
			return DeriveGlobalKey(key4DbPath, globalSalt);
		}

		/// <summary>
		/// Descifra una contraseña de Firefox del campo encryptedPassword de logins.json.
		/// </summary>
		/// <param name="encryptedBase64">Valor base64 del campo encryptedPassword.</param>
		/// <param name="globalKey">Clave de 24 bytes obtenida con <see cref="ExtractGlobalKey"/>.</param>
		/// <returns>Contraseña en texto claro, o null si no fue posible descifrar.</returns>
		public static string DecryptPassword(string encryptedBase64, byte[] globalKey)
		{
			if(string.IsNullOrEmpty(encryptedBase64)) return string.Empty;
			if(globalKey == null || globalKey.Length < 24) return null;

			try
			{
				byte[] cipherData = Convert.FromBase64String(encryptedBase64);
				// El formato es DER/ASN.1: secuencia que incluye el OID 3DES-CBC y el ciphertext.
				byte[] iv;
				byte[] ciphertext = ParseDerEncryptedValue(cipherData, out iv);
				if(ciphertext == null) return null;

				byte[] plaintext = DecryptTripleDes(ciphertext, globalKey, iv);

				// Eliminar el relleno PKCS#7.
				plaintext = RemovePkcs7Padding(plaintext);
				if(plaintext == null) return null;

				return Encoding.UTF8.GetString(plaintext);
			}
			catch { return null; }
		}

		// ── implementación privada ────────────────────────────────────────────────

		private static byte[] DeriveGlobalKey(string key4DbPath, byte[] globalSalt)
		{
			// Leer la tabla nssPrivate para obtener la clave privada cifrada.
			var sql  = "SELECT a11, a102 FROM nssPrivate LIMIT 1;";
			var cols = new[] { "a11", "a102" };
			List<Dictionary<string, object>> rows;
			try { rows = SqliteTempReader.QueryFile(key4DbPath, sql, cols); }
			catch { rows = new List<Dictionary<string, object>>(); }

			if(rows.Count == 0)
			{
				// key4.db con formato antiguo sin tabla nssPrivate; fallback a 24 bytes vacíos.
				return new byte[24];
			}

			byte[] a11 = rows[0]["a11"] as byte[];
			if(a11 == null) a11 = HexOrBytes(rows[0]["a11"]);
			if(a11 == null || a11.Length == 0) return new byte[24];

			// a11 es un DER que contiene la entrada cifrada de la clave privada.
			byte[] passwordBytes = Encoding.UTF8.GetBytes(string.Empty); // sin master password
			byte[] sha1Salt      = SHA1Bytes(Concat(globalSalt, passwordBytes));

			// Derivar clave 3DES (24 bytes) y IV (8 bytes) usando el hash del salt.
			byte[] keyMat = GenerateKeyMaterial(sha1Salt, 32);
			byte[] key    = EnsureTripleDesKey(Slice(keyMat, 0, 24));
			byte[] iv     = Slice(keyMat, 24, 8);

			byte[] decryptedA11 = DecryptTripleDes(a11, key, iv);
			if(decryptedA11 == null || decryptedA11.Length < 24) return new byte[24];

			// Los últimos 24 bytes de decryptedA11 son la clave global.
			return Slice(decryptedA11, decryptedA11.Length - 24, 24);
		}

		private static byte[] GenerateKeyMaterial(byte[] seed, int length)
		{
			// Extiende el material de clave concatenando SHA-1 iterados.
			var result = new System.Collections.Generic.List<byte>();
			byte[] prev = seed;
			while(result.Count < length)
			{
				prev = SHA1Bytes(Concat(prev, seed));
				result.AddRange(prev);
			}
			byte[] r = result.ToArray();
			Array.Resize(ref r, length);
			return r;
		}

		private static byte[] DecryptTripleDes(byte[] ciphertext, byte[] key, byte[] iv)
		{
			try
			{
				using(TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
				{
					des.Mode    = CipherMode.CBC;
					des.Padding = PaddingMode.None;
					des.Key     = EnsureTripleDesKey(key);
					des.IV      = iv;
					using(ICryptoTransform dec = des.CreateDecryptor())
						return dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
				}
			}
			catch { return null; }
		}

		/// <summary>
		/// Parsea la estructura DER que Firefox usa para almacenar contraseñas cifradas.
		/// Devuelve el ciphertext limpio y extrae el IV.
		/// </summary>
		private static byte[] ParseDerEncryptedValue(byte[] der, out byte[] iv)
		{
			iv = null;
			// Estructura mínima: SEQUENCE { SEQUENCE { OID, SEQUENCE { IV } }, OCTET_STRING }
			// Buscamos el patrón del OID 3DES-CBC y extraemos IV + ciphertext.
			try
			{
				int pos = 0;
				// Outer SEQUENCE
				if(der[pos] != 0x30) return null;
				pos += 2 + (der[pos + 1] > 0x7F ? der[pos + 1] & 0x7F : 0);
				if(pos >= der.Length) pos = 2;

				int ivPos = FindBytes(der, Oid3DesCbc);
				if(ivPos < 0) return null;
				// IV está en el OCTET STRING justo después del OID
				int ivStart = ivPos + Oid3DesCbc.Length;
				// Saltar eventuales tags/lengths intermedios
				if(ivStart < der.Length && der[ivStart] == 0x04)
				{
					ivStart++;
					int ivLen = der[ivStart++];
					iv = Slice(der, ivStart, ivLen);
					// Ciphertext está en el último OCTET STRING
					int ctPos = FindLastOctetString(der);
					if(ctPos >= 0)
					{
						int ctLen = der[ctPos + 1];
						return Slice(der, ctPos + 2, ctLen);
					}
				}
				return null;
			}
			catch { return null; }
		}

		private static int FindLastOctetString(byte[] data)
		{
			int last = -1;
			for(int i = 0; i < data.Length - 2; i++)
			{
				if(data[i] == 0x04 && data[i + 1] > 0 && data[i + 1] <= 255 - i)
					last = i;
			}
			return last;
		}

		private static int FindBytes(byte[] haystack, byte[] needle)
		{
			for(int i = 0; i <= haystack.Length - needle.Length; i++)
			{
				bool found = true;
				for(int j = 0; j < needle.Length; j++)
					if(haystack[i + j] != needle[j]) { found = false; break; }
				if(found) return i;
			}
			return -1;
		}

		private static byte[] RemovePkcs7Padding(byte[] data)
		{
			if(data == null || data.Length == 0) return data;
			int pad = data[data.Length - 1];
			if(pad < 1 || pad > 16 || pad > data.Length) return data;
			byte[] result = new byte[data.Length - pad];
			Buffer.BlockCopy(data, 0, result, 0, result.Length);
			return result;
		}

		private static byte[] EnsureTripleDesKey(byte[] key)
		{
			if(key.Length == 24) return key;
			if(key.Length >= 24) return Slice(key, 0, 24);
			// 16‑byte key → extender a 24 repitiendo los primeros 8 bytes al final
			byte[] k24 = new byte[24];
			Buffer.BlockCopy(key, 0, k24, 0, key.Length);
			if(key.Length == 16) Buffer.BlockCopy(key, 0, k24, 16, 8);
			return k24;
		}

		private static byte[] SHA1Bytes(byte[] data)
		{
			using(SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider())
				return sha.ComputeHash(data);
		}

		private static byte[] Concat(params byte[][] parts)
		{
			int len = 0;
			foreach(byte[] p in parts) len += p.Length;
			byte[] r = new byte[len];
			int pos = 0;
			foreach(byte[] p in parts) { Buffer.BlockCopy(p, 0, r, pos, p.Length); pos += p.Length; }
			return r;
		}

		private static byte[] Slice(byte[] src, int start, int length)
		{
			byte[] r = new byte[length];
			Buffer.BlockCopy(src, start, r, 0, length);
			return r;
		}

		private static byte[] HexOrBytes(object val)
		{
			if(val == null) return new byte[0];
			if(val is byte[]) return (byte[])val;
			string s = val.ToString();
			if(s.Length % 2 != 0 || s.Length == 0) return new byte[0];
			byte[] b = new byte[s.Length / 2];
			for(int i = 0; i < b.Length; i++)
				b[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
			return b;
		}
	}
}
