/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — helper DPAPI.
// Envuelve CryptUnprotectData de crypt32.dll para descifrar los blobs de contraseña
// que Chrome/Edge/Brave almacenan con DPAPI (Data Protection API de Windows).
// Solo funciona con el mismo usuario de Windows que cifró los datos.

using System;
using System.Runtime.InteropServices;

namespace KeePass.Infrastructure.BrowserImport
{
	/// <summary>
	/// Descifrado DPAPI mediante P/Invoke a <c>crypt32.dll</c>.
	/// </summary>
	public static class DpapiHelper
	{
		// ── estructuras P/Invoke ──────────────────────────────────────────────────

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct DataBlob
		{
			public int    cbData;
			public IntPtr pbData;
		}

		[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool CryptUnprotectData(
			ref DataBlob pDataIn,
			string       ppszDataDescr,
			IntPtr       pOptionalEntropy,
			IntPtr       pvReserved,
			IntPtr       pPromptStruct,
			int          dwFlags,
			ref DataBlob pDataOut);

		// ── API pública ───────────────────────────────────────────────────────────

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

			DataBlob dataIn  = new DataBlob();
			DataBlob dataOut = new DataBlob();

			// Copiar bytes cifrados a memoria no administrada.
			dataIn.cbData = encrypted.Length;
			dataIn.pbData = Marshal.AllocHGlobal(encrypted.Length);
			try
			{
				Marshal.Copy(encrypted, 0, dataIn.pbData, encrypted.Length);

				bool ok = CryptUnprotectData(ref dataIn, null, IntPtr.Zero,
					IntPtr.Zero, IntPtr.Zero, 0, ref dataOut);

				if(!ok)
					throw new System.Security.Cryptography.CryptographicException(
						"CryptUnprotectData falló. Código: " + Marshal.GetLastWin32Error());

				// Copiar resultado de vuelta a array administrado.
				byte[] result = new byte[dataOut.cbData];
				Marshal.Copy(dataOut.pbData, result, 0, dataOut.cbData);
				return result;
			}
			finally
			{
				// Liberar memoria no administrada.
				if(dataIn.pbData  != IntPtr.Zero) Marshal.FreeHGlobal(dataIn.pbData);
				if(dataOut.pbData != IntPtr.Zero) Marshal.FreeHGlobal(dataOut.pbData);
			}
		}
	}
}
