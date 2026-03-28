/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

// F15-E — Column configuration (extended columns).
// Controls which KeePass Modern Vibe custom columns are visible in
// the main entry list. Standard columns are managed by AceMainWindow.
// Stored under Program.Config.ColumnConfig.

using System.ComponentModel;

namespace KeePass.App.Configuration
{
	/// <summary>
	/// F15-E: Persists visibility settings for the extended custom columns
	/// added by KeePass Modern Vibe (TOTP, Favourites, …).
	/// Standard columns are handled by <see cref="AceMainWindow.EntryListColumns"/>.
	/// </summary>
	public sealed class AceColumnConfig
	{
		public AceColumnConfig() { }

		// ── F6 — columna TOTP ──────────────────────────────────────────────────

		private bool m_showTotp = false;
		/// <summary>
		/// Si es <c>true</c>, la columna "TOTP" se registra en
		/// <c>Program.ColumnProviderPool</c> y aparece en Configurar columnas.
		/// </summary>
		[DefaultValue(false)]
		public bool ShowTotpColumn
		{
			get { return m_showTotp; }
			set { m_showTotp = value; }
		}

		// ── F2 — columna Favorito ──────────────────────────────────────────────

		private bool m_showFavorite = true;
		/// <summary>
		/// Si es <c>true</c>, la columna "Favorito" (estrella) se registra en
		/// <c>Program.ColumnProviderPool</c>.
		/// </summary>
		[DefaultValue(true)]
		public bool ShowFavoriteColumn
		{
			get { return m_showFavorite; }
			set { m_showFavorite = value; }
		}

		// ── General ────────────────────────────────────────────────────────────

		private bool m_autoResize = true;
		/// <summary>
		/// Si es <c>true</c>, las columnas se ajustan automáticamente al contenido
		/// al abrir una base de datos.
		/// </summary>
		[DefaultValue(true)]
		public bool AutoResizeOnOpen
		{
			get { return m_autoResize; }
			set { m_autoResize = value; }
		}
	}
}
