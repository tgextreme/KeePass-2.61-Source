/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — DTO perfil de navegador.

namespace KeePass.Integration.BrowserImport
{
	/// <summary>Tipo de navegador detectado.</summary>
	public enum BrowserType
	{
		Chrome,
		Edge,
		Brave,
		Firefox
	}

	/// <summary>
	/// Perfil de un navegador instalado en el equipo.
	/// Inmutable tras la construcción.
	/// </summary>
	public sealed class BrowserProfile
	{
		/// <summary>Navegador al que pertenece este perfil.</summary>
		public BrowserType Browser     { get; private set; }
		/// <summary>Nombre legible (p. ej. "Default", "Perfil 1").</summary>
		public string ProfileName      { get; private set; }
		/// <summary>Ruta completa a la carpeta del perfil.</summary>
		public string ProfilePath      { get; private set; }
		/// <summary>Descripción combinada para mostrar en la UI.</summary>
		public string DisplayName      { get { return Browser + " — " + ProfileName; } }

		public BrowserProfile(BrowserType browser, string profileName, string profilePath)
		{
			Browser     = browser;
			ProfileName = profileName ?? string.Empty;
			ProfilePath = profilePath ?? string.Empty;
		}

		public override string ToString() { return DisplayName; }
	}
}
