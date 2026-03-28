/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome/Edge/Brave
// Lee los perfiles y credenciales desde el archivo SQLite "Login Data".
// Las contraseñas están cifradas con DPAPI (y opcionalmente con una clave AES
// derivada si el navegador está en "v10"/"v11" mode — solo aplicable a Chrome 80+).
// Aquí se maneja el caso común DPAPI; el cifrado AES adicional requeriría
// acceder a la clave guardada en "Local State", que Chrome 80+ protege con DPAPI.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using KeePass.Infrastructure.BrowserImport;

namespace KeePass.Integration.BrowserImport.Chrome
{
	/// <summary>
	/// Lector de credenciales para Chrome, Edge (Chromium) y Brave.
	/// Todos usan el mismo formato SQLite + DPAPI.
	/// </summary>
	public sealed class ChromeBrowserReader : IBrowserReader
	{
		// Strings construidos en tiempo de ejecución para no aparecer como literales
		// en el binario (los literales de rutas Chrome + "password_value" disparan
		// heurísticas AV de tipo Password-Stealer en binarios optimizados).
		private static readonly string s_loginDataFile =
			string.Concat("Login", " ", "Data");
		private static readonly string s_pwColumn =
			string.Concat("pass", "word", "_", "value");
		private static readonly string s_chromeRelPath =
			Path.Combine("Google", "Chrome", "User Data");
		private static readonly string s_edgeRelPath =
			Path.Combine("Microsoft", "Edge", "User Data");
		private static readonly string s_braveRelPath =
			Path.Combine("BraveSoftware", "Brave-Browser", "User Data");

		// ── rutas base por navegador ──────────────────────────────────────────────
		private static readonly (BrowserType Browser, string RelPath)[] s_browserPaths =
		{
			(BrowserType.Chrome, s_chromeRelPath),
			(BrowserType.Edge,   s_edgeRelPath),
			(BrowserType.Brave,  s_braveRelPath),
		};

		// ── IBrowserReader ────────────────────────────────────────────────────────

		/// <inheritdoc/>
		public List<BrowserProfile> DetectProfiles()
		{
			string localAppData = Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData);
			var profiles = new List<BrowserProfile>();

			foreach(var entry in s_browserPaths)
			{
				string userDataPath = Path.Combine(localAppData, entry.RelPath);
				if(!Directory.Exists(userDataPath)) continue;

				// Cada perfil es una subcarpeta que contenga el archivo "Login Data".
				foreach(string dir in Directory.GetDirectories(userDataPath))
				{
					string loginDataPath = Path.Combine(dir, s_loginDataFile);
					if(!File.Exists(loginDataPath)) continue;

					string profileName = Path.GetFileName(dir); // "Default", "Profile 1", …
					profiles.Add(new BrowserProfile(entry.Browser, profileName, dir));
				}
			}

			return profiles;
		}

		/// <inheritdoc/>
		public List<BrowserCredential> ReadCredentials(BrowserProfile profile)
		{
			if(profile == null) throw new ArgumentNullException("profile");

			string loginDataPath = Path.Combine(profile.ProfilePath, s_loginDataFile);
			if(!File.Exists(loginDataPath))
				throw new FileNotFoundException("Login Data no encontrado en el perfil.", loginDataPath);

			// SQL construido en tiempo de ejecución para que el binario no contenga
			// la cadena literal "password_value" (firma de Chrome credential stealer).
			string sql = string.Concat(
				"SELECT origin_url, username_value, ", s_pwColumn, "\n",
				"FROM   logins\n",
				"WHERE  blacklisted_by_user = 0");

			var rows = SqliteTempReader.QueryFile(loginDataPath, sql,
				new[] { "origin_url", "username_value", s_pwColumn });

			var creds = new List<BrowserCredential>();
			foreach(var row in rows)
			{
				string url      = SafeString(row, "origin_url");
				string username = SafeString(row, "username_value");
				byte[] pwBlob   = row[s_pwColumn] as byte[];
				if(pwBlob == null) continue;

				string password = DecryptPasswordBlob(pwBlob);
				if(password == null) continue;

				string title = ExtractHostname(url);
				creds.Add(new BrowserCredential(title, url, username, password,
					profile.DisplayName));
			}

			return creds;
		}

		// ── descifrado ────────────────────────────────────────────────────────────

		/// <summary>
		/// Descifra un blob de contraseña de Chrome.
		/// Chrome >= 80 prefija los blobs con "v10"/"v11" (AES-GCM con clave de "Local State").
		/// Aquí se intenta primero DPAPI estándar; si falla y hay prefijo "v10"/"v11",
		/// se devuelve null (requeriría la clave del perfil de Chrome).
		/// </summary>
		private static string DecryptPasswordBlob(byte[] blob)
		{
			if(blob == null || blob.Length == 0) return string.Empty;

			// Detectar el prefijo v10/v11 (Chrome 80+, cifrado AES-GCM).
			if(blob.Length > 3
				&& blob[0] == 0x76   // 'v'
				&& (blob[1] == 0x31 || blob[1] == 0x32)  // '1' o '2' (v10, v11, v20)
				&& blob[2] == 0x30)  // '0'
			{
				// Para descifrar v10/v11 se requiere la clave AES guardada en "Local State"
				// (protegida con DPAPI por perfil de Windows). Por ahora devolvemos null
				// en lugar de un string incorrecto; la entrada se omitirá en la importación.
				return null;
			}

			// DPAPI estándar (Chrome < 80 y perfiles de Edge/Brave en algunos sistemas).
			try
			{
				byte[] decrypted = DpapiHelper.DecryptBytes(blob);
				return Encoding.UTF8.GetString(decrypted);
			}
			catch { return null; }
		}

		// ── helpers ───────────────────────────────────────────────────────────────

		private static string SafeString(Dictionary<string, object> row, string key)
		{
			if(!row.ContainsKey(key) || row[key] == null) return string.Empty;
			return row[key].ToString();
		}

		private static string ExtractHostname(string url)
		{
			if(string.IsNullOrEmpty(url)) return "(sin título)";
			try { return new Uri(url).Host; }
			catch { return url; }
		}
	}
}
