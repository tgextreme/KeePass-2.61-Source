/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — DTO credencial.

namespace KeePass.Integration.BrowserImport
{
	/// <summary>
	/// Credencial extraída de un navegador.  Inmutable tras la construcción.
	/// </summary>
	public sealed class BrowserCredential
	{
		/// <summary>Título derivado del hostname de la URL.</summary>
		public string Title    { get; private set; }
		/// <summary>URL de origen tal como la almacena el navegador.</summary>
		public string Url      { get; private set; }
		/// <summary>Nombre de usuario (puede estar vacío).</summary>
		public string Username { get; private set; }
		/// <summary>Contraseña en texto claro (descifrada en memoria; no se escribe en disco).</summary>
		public string Password { get; private set; }
		/// <summary>Identificador de origen ("Chrome — Default", "Firefox — default-release", etc.).</summary>
		public string Origin   { get; private set; }

		public BrowserCredential(string title, string url, string username,
			string password, string origin)
		{
			Title    = title    ?? string.Empty;
			Url      = url      ?? string.Empty;
			Username = username ?? string.Empty;
			Password = password ?? string.Empty;
			Origin   = origin   ?? string.Empty;
		}
	}
}
