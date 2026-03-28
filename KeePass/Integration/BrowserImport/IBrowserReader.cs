/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — interfaz de lector de credenciales.

using System.Collections.Generic;

namespace KeePass.Integration.BrowserImport
{
	/// <summary>
	/// Abstracción común para los lectores de credenciales de cada navegador.
	/// </summary>
	public interface IBrowserReader
	{
		/// <summary>
		/// Detecta los perfiles instalados en el equipo para este navegador.
		/// Devuelve lista vacía si el navegador no está instalado.
		/// </summary>
		List<BrowserProfile> DetectProfiles();

		/// <summary>
		/// Lee las credenciales almacenadas en <paramref name="profile"/>.
		/// Las contraseñas se descifran en memoria.  Nunca se escriben en disco.
		/// </summary>
		/// <exception cref="System.Exception">Si no se puede acceder al perfil.</exception>
		List<BrowserCredential> ReadCredentials(BrowserProfile profile);
	}
}
