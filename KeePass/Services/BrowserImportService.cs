/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde CSV de navegador — servicio principal.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using KeePass.DataExchange;
using KeePass.Integration.BrowserImport;

using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Delegates;
using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Implementación de <see cref="IBrowserImportService"/> para CSV exportado.
	/// </summary>
	public sealed class BrowserImportService : IBrowserImportService
	{
		private static readonly List<BrowserCsvFormatInfo> s_formats =
			new List<BrowserCsvFormatInfo>
			{
				new BrowserCsvFormatInfo(
					BrowserCsvFormat.Chrome,
					"Chrome (CSV de contraseñas)",
					"name, url, username, password, note",
					"Chrome > Configuración > Gestor de contraseñas > Exportar"),
				new BrowserCsvFormatInfo(
					BrowserCsvFormat.Edge,
					"Edge (CSV de contraseñas)",
					"name, url, username, password, note",
					"Edge > Configuración > Contraseñas > Exportar"),
				new BrowserCsvFormatInfo(
					BrowserCsvFormat.Brave,
					"Brave (CSV de contraseñas)",
					"name, url, username, password, note",
					"Brave > Configuración > Contraseñas > Exportar"),
				new BrowserCsvFormatInfo(
					BrowserCsvFormat.Firefox,
					"Firefox (CSV de contraseñas)",
					"url, username, password",
					"Firefox > Contraseñas > Menú de 3 puntos > Exportar"),
				new BrowserCsvFormatInfo(
					BrowserCsvFormat.Generico,
					"CSV genérico",
					"title/name, url, username/user, password/pass, notes",
					"Úsalo cuando el CSV no coincide con Chrome/Edge/Brave/Firefox")
			};

		private struct ColumnMap
		{
			public int Title;
			public int Url;
			public int Username;
			public int Password;
		}

		public List<BrowserCsvFormatInfo> GetSupportedFormats()
		{
			return new List<BrowserCsvFormatInfo>(s_formats);
		}

		public List<BrowserCredential> PreviewCredentialsFromCsv(string csvPath,
			BrowserCsvFormat format)
		{
			if(string.IsNullOrEmpty(csvPath))
				throw new ArgumentNullException("csvPath");
			if(!File.Exists(csvPath))
				throw new FileNotFoundException("No se encontró el archivo CSV.", csvPath);

			string csvData = File.ReadAllText(csvPath, Encoding.UTF8);
			CsvOptions opt = new CsvOptions();
			opt.BackslashIsEscape = false;
			CsvStreamReaderEx csr = new CsvStreamReaderEx(csvData, opt);

			string[] header = csr.ReadLine();
			if((header == null) || (header.Length == 0))
				return new List<BrowserCredential>();

			Dictionary<string, int> hm = BuildHeaderMap(header);
			ColumnMap cm = BuildColumnMap(format, hm);
			string origin = GetOriginName(format);

			var creds = new List<BrowserCredential>();
			string[] row;
			while((row = csr.ReadLine()) != null)
			{
				string url = GetCell(row, cm.Url);
				string user = GetCell(row, cm.Username);
				string password = GetCell(row, cm.Password);
				if(string.IsNullOrEmpty(password)) continue;

				string title = GetCell(row, cm.Title);
				if(string.IsNullOrEmpty(title)) title = ExtractHostname(url);
				if(string.IsNullOrEmpty(title)) title = "(sin título)";

				creds.Add(new BrowserCredential(title, url, user, password, origin));
			}

			return creds;
		}

		public List<BrowserCredential> DetectDuplicates(
			List<BrowserCredential> credentials, PwDatabase db)
		{
			if(credentials == null) throw new ArgumentNullException("credentials");
			if(db == null || !db.IsOpen) return new List<BrowserCredential>();

			HashSet<string> existing = BuildExistingEntryKeySet(db);
			var duplicates = new List<BrowserCredential>();

			foreach(BrowserCredential cred in credentials)
			{
				if(existing.Contains(BuildEntryKey(cred.Url, cred.Username)))
					duplicates.Add(cred);
			}

			return duplicates;
		}

		public BrowserImportResult Import(List<BrowserCredential> toImport,
			PwGroup targetGroup, PwDatabase db)
		{
			if(toImport == null) throw new ArgumentNullException("toImport");
			if(targetGroup == null) throw new ArgumentNullException("targetGroup");
			if(db == null) throw new ArgumentNullException("db");

			var result = new BrowserImportResult();
			HashSet<string> existing = BuildExistingEntryKeySet(db);

			foreach(BrowserCredential cred in toImport)
			{
				string key = BuildEntryKey(cred.Url, cred.Username);
				if(existing.Contains(key))
				{
					result.Duplicates++;
					continue;
				}

				try
				{
					PwEntry entry = new PwEntry(true, true);
					entry.Strings.Set(PwDefs.TitleField,
						new ProtectedString(false, cred.Title));
					entry.Strings.Set(PwDefs.UserNameField,
						new ProtectedString(false, cred.Username));
					entry.Strings.Set(PwDefs.PasswordField,
						new ProtectedString(true, cred.Password));
					entry.Strings.Set(PwDefs.UrlField,
						new ProtectedString(false, cred.Url));
					entry.Strings.Set(PwDefs.NotesField,
						new ProtectedString(false, "Importado desde CSV" +
							(string.IsNullOrEmpty(cred.Origin) ? string.Empty :
							(" (" + cred.Origin + ")"))));

					targetGroup.AddEntry(entry, true);
					existing.Add(key);
					result.Imported++;
				}
				catch
				{
					result.Skipped++;
				}
			}

			if(result.Imported > 0)
				db.Modified = true;

			return result;
		}

		private static Dictionary<string, int> BuildHeaderMap(string[] header)
		{
			var hm = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for(int i = 0; i < header.Length; ++i)
			{
				string k = NormalizeHeader(header[i]);
				if(string.IsNullOrEmpty(k)) continue;
				if(!hm.ContainsKey(k)) hm[k] = i;
			}
			return hm;
		}

		private static ColumnMap BuildColumnMap(BrowserCsvFormat format,
			Dictionary<string, int> hm)
		{
			ColumnMap cm;
			cm.Title = -1;
			cm.Url = -1;
			cm.Username = -1;
			cm.Password = -1;

			if(format == BrowserCsvFormat.Firefox)
			{
				cm.Url = FindColumn(hm, "url", "hostname", "site", "originurl");
				cm.Username = FindColumn(hm, "username", "user", "login");
				cm.Password = FindColumn(hm, "password", "pass");
				cm.Title = FindColumn(hm, "title", "name");
			}
			else if((format == BrowserCsvFormat.Chrome) ||
				(format == BrowserCsvFormat.Edge) ||
				(format == BrowserCsvFormat.Brave))
			{
				cm.Title = FindColumn(hm, "name", "title");
				cm.Url = FindColumn(hm, "url", "originurl", "website");
				cm.Username = FindColumn(hm, "username", "usernamevalue", "user", "login");
				cm.Password = FindColumn(hm, "password", "passwordvalue", "pass");
			}
			else
			{
				cm.Title = FindColumn(hm, "title", "name", "site");
				cm.Url = FindColumn(hm, "url", "originurl", "website", "link", "hostname");
				cm.Username = FindColumn(hm, "username", "user", "login", "email");
				cm.Password = FindColumn(hm, "password", "pass", "pwd", "secret");
			}

			if(cm.Password < 0)
				throw new FormatException("El CSV no contiene columna de contraseña (password/pass).");

			if((cm.Url < 0) && (cm.Title < 0))
				throw new FormatException("El CSV no contiene columna URL ni título reconocible.");

			return cm;
		}

		private static int FindColumn(Dictionary<string, int> hm, params string[] names)
		{
			for(int i = 0; i < names.Length; ++i)
			{
				string k = NormalizeHeader(names[i]);
				int idx;
				if(hm.TryGetValue(k, out idx)) return idx;
			}
			return -1;
		}

		private static string NormalizeHeader(string s)
		{
			if(string.IsNullOrEmpty(s)) return string.Empty;
			s = s.Trim().ToLowerInvariant();
			return s.Replace(" ", string.Empty).Replace("_", string.Empty)
				.Replace("-", string.Empty);
		}

		private static string GetCell(string[] row, int index)
		{
			if((row == null) || (index < 0) || (index >= row.Length)) return string.Empty;
			return (row[index] ?? string.Empty).Trim();
		}

		private static string ExtractHostname(string url)
		{
			if(string.IsNullOrEmpty(url)) return string.Empty;
			try { return new Uri(url).Host; }
			catch { return url; }
		}

		private static string GetOriginName(BrowserCsvFormat format)
		{
			for(int i = 0; i < s_formats.Count; ++i)
			{
				if(s_formats[i].Format == format)
					return s_formats[i].DisplayName;
			}
			return format.ToString();
		}

		private static HashSet<string> BuildExistingEntryKeySet(PwDatabase db)
		{
			var set = new HashSet<string>(StringComparer.Ordinal);
			if((db == null) || !db.IsOpen || (db.RootGroup == null)) return set;

			EntryHandler eh = delegate(PwEntry pe)
			{
				string url = pe.Strings.ReadSafe(PwDefs.UrlField);
				string user = pe.Strings.ReadSafe(PwDefs.UserNameField);
				set.Add(BuildEntryKey(url, user));
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return set;
		}

		private static string BuildEntryKey(string url, string user)
		{
			string u = (url ?? string.Empty).Trim().ToLowerInvariant();
			string n = (user ?? string.Empty).Trim().ToLowerInvariant();
			return u + "\n" + n;
		}
	}
}
