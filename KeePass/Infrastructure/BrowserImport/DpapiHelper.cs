/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — helper DPAPI.
// Usa System.Security.Cryptography.ProtectedData (API gestionada .NET) para descifrar
// los blobs de contraseña que Chrome/Edge/Brave almacenan con DPAPI.
// Solo funciona con el mismo usuario de Windows que cifró los datos.

using System;
using System.Security.Cryptography;

namespace KeePass.Infrastructure.BrowserImport
{
	/// <summary>
	/// Descifrado DPAPI mediante <see cref="ProtectedData"/> del BCL de .NET.
	/// </summary>
	public static class DpapiHelper
	{
		/// <summary>
		/// Descifra un blob DPAPI y devuelve los bytes en claro.
		/// </summary>
		/// <param name="encrypted">Bytes cifrados con DPAPI.</param>
		/// <returns>Bytes descifrados.</returns>
		/// <exception cref="CryptographicException">Si el descifrado falla.</exception>
		public static byte[] DecryptBytes(byte[] encrypted)
		{
			if(encrypted == null) throw new ArgumentNullException("encrypted");
			if(encrypted.Length == 0) return new byte[0];

			// ProtectedData.Unprotect es el wrapper gestionado de CryptUnprotectData.
			// El ámbito CurrentUser garantiza que solo el mismo usuario puede descifrar.
			return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
		}
	}
}
