/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — servicio principal.
// Orquesta los readers de Chrome y Firefox, detecta duplicados y
// crea las entradas PwEntry en el grupo destino.

using System;
using System.Collections.Generic;

using KeePass.Integration.BrowserImport;
using KeePass.Integration.BrowserImport.Chrome;
using KeePass.Integration.BrowserImport.Firefox;

using KeePassLib;
using KeePassLib.Delegates;
using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Implementación de <see cref="IBrowserImportService"/>. Thread-safe para lectura;
	/// la llamada a <see cref="Import"/> debe realizarse desde el hilo UI.
	/// </summary>
	public sealed class BrowserImportService : IBrowserImportService
	{
		private static readonly IBrowserReader[] s_readers = new IBrowserReader[]
		{
			new ChromeBrowserReader(),
			new FirefoxBrowserReader(),
		};

		// ── IBrowserImportService ─────────────────────────────────────────────────

		/// <inheritdoc/>
		public List<BrowserProfile> GetAvailableProfiles()
		{
			var all = new List<BrowserProfile>();
			foreach(IBrowserReader reader in s_readers)
			{
				try { all.AddRange(reader.DetectProfiles()); }
				catch { /* Ignorar errores de detección; puede que el navegador no esté instalado */ }
			}
			return all;
		}

		/// <inheritdoc/>
		public List<BrowserCredential> PreviewCredentials(BrowserProfile profile)
		{
			if(profile == null) throw new ArgumentNullException("profile");
			IBrowserReader reader = GetReaderFor(profile);
			return reader.ReadCredentials(profile);
		}

		/// <inheritdoc/>
		public List<BrowserCredential> DetectDuplicates(
			List<BrowserCredential> credentials, PwDatabase db)
		{
			if(credentials == null) throw new ArgumentNullException("credentials");
			if(db == null || !db.IsOpen) return new List<BrowserCredential>();

			// Recopilar todas las entradas de la base para comparar.
			List<PwEntry> allEntries = new List<PwEntry>();
			EntryHandler ehCollect = delegate(PwEntry pe) { allEntries.Add(pe); return true; };
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, ehCollect);

			var duplicates = new List<BrowserCredential>();
			foreach(BrowserCredential cred in credentials)
			{
				foreach(PwEntry entry in allEntries)
				{
					string url  = entry.Strings.ReadSafe(PwDefs.UrlField);
					string user = entry.Strings.ReadSafe(PwDefs.UserNameField);
					if(string.Equals(url, cred.Url, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(user, cred.Username, StringComparison.OrdinalIgnoreCase))
					{
						duplicates.Add(cred);
						break;
					}
				}
			}
			return duplicates;
		}

		/// <inheritdoc/>
		public BrowserImportResult Import(
			List<BrowserCredential> toImport, PwGroup targetGroup, PwDatabase db)
		{
			if(toImport == null)     throw new ArgumentNullException("toImport");
			if(targetGroup == null)  throw new ArgumentNullException("targetGroup");
			if(db == null)           throw new ArgumentNullException("db");

			var result = new BrowserImportResult();

			foreach(BrowserCredential cred in toImport)
			{
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
						new ProtectedString(false, "Importado desde " + cred.Origin));

					targetGroup.AddEntry(entry, true);
					result.Imported++;
				}
				catch { result.Skipped++; }
			}

			if(result.Imported > 0)
				db.Modified = true;

			return result;
		}

		// ── privado ───────────────────────────────────────────────────────────────

		private static IBrowserReader GetReaderFor(BrowserProfile profile)
		{
			if(profile.Browser == BrowserType.Firefox)
				return new FirefoxBrowserReader();
			return new ChromeBrowserReader();
		}
	}
}
