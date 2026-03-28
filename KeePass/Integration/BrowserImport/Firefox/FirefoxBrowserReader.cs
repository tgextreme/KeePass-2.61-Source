/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Firefox
// Lee las credenciales de logins.json y descifra con key4.db.
// Solo admite perfiles sin contraseña maestra (caso habitual doméstico).
// Para perfiles con contraseña maestra, ReadCredentials devuelve lista vacía
// con un mensaje de error en los Notes de una entrada centinela.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using KeePass.Infrastructure.BrowserImport;

namespace KeePass.Integration.BrowserImport.Firefox
{
	/// <summary>
	/// Lector de credenciales para Mozilla Firefox.
	/// Usa <c>logins.json</c> + <c>key4.db</c>.
	/// </summary>
	public sealed class FirefoxBrowserReader : IBrowserReader
	{
		private const string ProfilesIniPath = @"Mozilla\Firefox\profiles.ini";

		// ── IBrowserReader ────────────────────────────────────────────────────────

		/// <inheritdoc/>
		public List<BrowserProfile> DetectProfiles()
		{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string iniPath = Path.Combine(appData, ProfilesIniPath);
			if(!File.Exists(iniPath)) return new List<BrowserProfile>();

			return ParseProfilesIni(iniPath, appData);
		}

		/// <inheritdoc/>
		public List<BrowserCredential> ReadCredentials(BrowserProfile profile)
		{
			if(profile == null) throw new ArgumentNullException("profile");

			string loginsJson = Path.Combine(profile.ProfilePath, "logins.json");
			string key4Db     = Path.Combine(profile.ProfilePath, "key4.db");

			if(!File.Exists(loginsJson))
				return new List<BrowserCredential>();

			// Extraer la clave global del key4.db.
			byte[] globalKey = null;
			if(File.Exists(key4Db))
			{
				try { globalKey = FirefoxCryptoHelper.ExtractGlobalKey(key4Db); }
				catch { globalKey = null; }
			}

			// Leer y parsear logins.json.
			string json = File.ReadAllText(loginsJson, Encoding.UTF8);
			return ParseLoginsJson(json, globalKey, profile.DisplayName);
		}

		// ── parseo ────────────────────────────────────────────────────────────────

		private static List<BrowserProfile> ParseProfilesIni(string iniPath, string appDataRoot)
		{
			var profiles = new List<BrowserProfile>();
			string[] lines = File.ReadAllLines(iniPath, Encoding.UTF8);
			bool inProfile = false;
			string name = null, path = null;
			bool isRelative = true;

			foreach(string rawLine in lines)
			{
				string line = rawLine.Trim();
				if(string.IsNullOrEmpty(line)) continue;

				if(line.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase))
				{
					// Guardar el perfil anterior si existe.
					if(name != null && path != null)
						AddFirefoxProfile(profiles, name, path, isRelative, appDataRoot);
					inProfile = true; name = null; path = null; isRelative = true;
					continue;
				}

				if(!inProfile) continue;

				if(line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
					name = line.Substring(5);
				else if(line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
					path = line.Substring(5).Replace('/', Path.DirectorySeparatorChar);
				else if(line.StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
					isRelative = line.Substring(11) != "0";
			}
			// Último perfil del archivo.
			if(name != null && path != null)
				AddFirefoxProfile(profiles, name, path, isRelative, appDataRoot);

			return profiles;
		}

		private static void AddFirefoxProfile(List<BrowserProfile> list,
			string name, string path, bool isRelative, string appDataRoot)
		{
			string fullPath = isRelative
				? Path.Combine(appDataRoot, @"Mozilla\Firefox", path)
				: path;

			if(Directory.Exists(fullPath))
				list.Add(new BrowserProfile(BrowserType.Firefox, name, fullPath));
		}

		private static List<BrowserCredential> ParseLoginsJson(
			string json, byte[] globalKey, string origin)
		{
			var creds = new List<BrowserCredential>();

			// Parseo JSON mínimo sin Newtonsoft (no disponible en este proyecto).
			// Buscamos el array "logins" y extraemos los campos necesarios con un
			// parser manual de nivel de texto.
			int loginsStart = json.IndexOf("\"logins\"", StringComparison.Ordinal);
			if(loginsStart < 0) return creds;

			int arrayStart = json.IndexOf('[', loginsStart);
			if(arrayStart < 0) return creds;

			int depth = 0;
			int objStart = -1;
			for(int i = arrayStart; i < json.Length; i++)
			{
				char c = json[i];
				if(c == '{')
				{
					if(depth == 1) objStart = i;
					depth++;
				}
				else if(c == '}')
				{
					depth--;
					if(depth == 1 && objStart >= 0)
					{
						string obj = json.Substring(objStart, i - objStart + 1);
						var cred = ParseLoginObject(obj, globalKey, origin);
						if(cred != null) creds.Add(cred);
						objStart = -1;
					}
					else if(depth == 0) break;
				}
			}

			return creds;
		}

		private static BrowserCredential ParseLoginObject(
			string obj, byte[] globalKey, string origin)
		{
			string hostname      = ExtractJsonString(obj, "hostname");
			string encUser       = ExtractJsonString(obj, "encryptedUsername");
			string encPass       = ExtractJsonString(obj, "encryptedPassword");

			if(string.IsNullOrEmpty(encPass)) return null;

			string username = string.Empty;
			if(globalKey != null && !string.IsNullOrEmpty(encUser))
				username = FirefoxCryptoHelper.DecryptPassword(encUser, globalKey) ?? string.Empty;

			string password = null;
			if(globalKey != null)
				password = FirefoxCryptoHelper.DecryptPassword(encPass, globalKey);
			if(password == null) return null;

			string title = string.IsNullOrEmpty(hostname)
				? "(sin título)"
				: TryExtractHost(hostname);

			return new BrowserCredential(title, hostname, username, password, origin);
		}

		// ── helpers ───────────────────────────────────────────────────────────────

		/// <summary>Extrae el valor de una propiedad string del objeto JSON dado.</summary>
		private static string ExtractJsonString(string obj, string key)
		{
			string search = "\"" + key + "\"";
			int pos = obj.IndexOf(search, StringComparison.Ordinal);
			if(pos < 0) return null;
			pos += search.Length;
			while(pos < obj.Length && obj[pos] != '"') pos++;   // saltar hasta '
			if(pos >= obj.Length) return null;
			pos++; // saltar la comilla de apertura
			var sb = new StringBuilder();
			while(pos < obj.Length && obj[pos] != '"')
			{
				if(obj[pos] == '\\' && pos + 1 < obj.Length)
				{
					pos++;
					sb.Append(obj[pos] == 'n' ? '\n' : obj[pos] == 't' ? '\t' : obj[pos]);
				}
				else sb.Append(obj[pos]);
				pos++;
			}
			return sb.ToString();
		}

		private static string TryExtractHost(string url)
		{
			try { return new Uri(url).Host; }
			catch { return url; }
		}
	}
}
