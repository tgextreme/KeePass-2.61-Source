/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F6 — Columna TOTP
// Lee el secreto TOTP almacenado en los campos estándar de KeePass
// (TimeOtp-Secret-Base32, etc.) y devuelve el código TOTP actual de 6 dígitos.

using System;
using System.Text;

using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Utility;

namespace KeePass.Services
{
	/// <summary>
	/// Genera códigos TOTP a partir de los secretos almacenados en una entrada.
	/// Utiliza los mismos nombres de campo que el motor de marcadores de posición
	/// de KeePass ({TIMEOTP}).
	/// </summary>
	public static class TotpService
	{
		// Nombres de campo (espejo de las constantes internas de EntryUtil)
		private const string TotpPrefix      = "TimeOtp-";
		private const string OtpSecret       = "Secret";
		private const string OtpSecretHex    = "Secret-Hex";
		private const string OtpSecretBase32 = "Secret-Base32";
		private const string OtpSecretBase64 = "Secret-Base64";
		private const string TotpLength      = TotpPrefix + "Length";
		private const string TotpPeriod      = TotpPrefix + "Period";
		private const string TotpAlg         = TotpPrefix + "Algorithm";

		/// <summary>
		/// Devuelve el código TOTP actual para la entrada dada,
		/// o una cadena vacía si no hay secreto TOTP configurado.
		/// </summary>
		public static string GetTotp(PwEntry pe)
		{
			if(pe == null) return string.Empty;

			byte[] secret = ReadSecret(pe);
			if(secret == null || secret.Length == 0) return string.Empty;

			// Número de dígitos (por defecto 6)
			uint uLength = 6;
			string strLength = pe.Strings.ReadSafe(TotpLength);
			uint.TryParse(strLength, out uLength);
			if(uLength == 0) uLength = 6;

			// Período en segundos (0 → usar el valor por defecto de HmacOtp = 30 s)
			uint uPeriod = 0;
			string strPeriod = pe.Strings.ReadSafe(TotpPeriod);
			uint.TryParse(strPeriod, out uPeriod);

			// Algoritmo de firma (vacío → SHA-1)
			string strAlg = pe.Strings.ReadSafe(TotpAlg);

			try
			{
				return HmacOtp.GenerateTimeOtp(secret, null, uPeriod, uLength, strAlg);
			}
			catch { return string.Empty; }
		}

		/// <summary>
		/// Indica si la entrada tiene un secreto TOTP configurado.
		/// Útil para decidir si mostrar la celda o no.
		/// </summary>
		public static bool HasTotp(PwEntry pe)
		{
			if(pe == null) return false;
			byte[] s = ReadSecret(pe);
			return (s != null && s.Length > 0);
		}

		// ── privado ──────────────────────────────────────────────────────────────

		private static byte[] ReadSecret(PwEntry pe)
		{
			try
			{
				// Prioridad idéntica a EntryUtil.GetOtpSecret
				string str = pe.Strings.ReadSafe(TotpPrefix + OtpSecret);
				if(!string.IsNullOrEmpty(str))
					return Encoding.UTF8.GetBytes(str);

				str = pe.Strings.ReadSafe(TotpPrefix + OtpSecretHex);
				if(!string.IsNullOrEmpty(str))
					return MemUtil.HexStringToByteArray(str);

				str = pe.Strings.ReadSafe(TotpPrefix + OtpSecretBase32);
				if(!string.IsNullOrEmpty(str))
					return MemUtil.ParseBase32(str, true);

				str = pe.Strings.ReadSafe(TotpPrefix + OtpSecretBase64);
				if(!string.IsNullOrEmpty(str))
					return Convert.FromBase64String(str);
			}
			catch { }

			return null;
		}
	}
}
