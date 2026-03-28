/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde navegadores — interfaz del servicio.

using System.Collections.Generic;

using KeePass.Integration.BrowserImport;
using KeePassLib;

namespace KeePass.Services
{
	/// <summary>Resultado de una operación de importación.</summary>
	public sealed class BrowserImportResult
	{
		public int Imported   { get; internal set; }
		public int Skipped    { get; internal set; }
		public int Duplicates { get; internal set; }

		public override string ToString()
		{
			return string.Format("{0} importadas, {1} duplicadas omitidas, {2} errores",
				Imported, Duplicates, Skipped);
		}
	}

	/// <summary>
	/// Contrato del servicio de importación desde navegadores.
	/// </summary>
	public interface IBrowserImportService
	{
		/// <summary>Devuelve todos los perfiles detectados de Chrome, Edge, Brave y Firefox.</summary>
		List<BrowserProfile> GetAvailableProfiles();

		/// <summary>
		/// Lee las credenciales de <paramref name="profile"/> sin importarlas.
		/// Permite la vista previa en el wizard.
		/// </summary>
		List<BrowserCredential> PreviewCredentials(BrowserProfile profile);

		/// <summary>
		/// Detecta qué credenciales de la lista ya existen en <paramref name="db"/>.
		/// </summary>
		List<BrowserCredential> DetectDuplicates(
			List<BrowserCredential> credentials, PwDatabase db);

		/// <summary>
		/// Importa las credenciales marcadas en <paramref name="toImport"/>
		/// al grupo <paramref name="targetGroup"/> de la base activa.
		/// </summary>
		BrowserImportResult Import(
			List<BrowserCredential> toImport, PwGroup targetGroup, PwDatabase db);
	}
}
