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

// F15-E — Column Manager.
// Centraliza el registro de proveedores de columna personalizados y los
// ajustes relacionados (columnas por defecto, auto-resize al abrir DB).
//
// Uso en MainForm:
//   ColumnManager.Initialize(Program.Config.ColumnConfig);
//   ColumnManager.EnsureDefaultAceColumns(Program.Config.MainWindow);

using System.Collections.Generic;
using System.Windows.Forms;

using KeePass.App.Configuration;

namespace KeePass.UI
{
	/// <summary>
	/// F15-E: Centraliza el registro de proveedores de columna de KeePass Modern Vibe
	/// y la configuración de columnas por defecto de la lista de entradas.
	/// </summary>
	public static class ColumnManager
	{
		// ── Inicialización ───────────────────────────────────────────────────────

		/// <summary>
		/// Registra en <c>Program.ColumnProviderPool</c> los proveedores de columna
		/// habilitados por la configuración <paramref name="cfg"/>.
		/// Llamar una vez durante el arranque de <c>MainForm</c>.
		/// </summary>
		public static void Initialize(AceColumnConfig cfg)
		{
			if(cfg == null) cfg = new AceColumnConfig();

			if(cfg.ShowFavoriteColumn)
				Program.ColumnProviderPool.Add(new FavoriteColumnProvider()); // F2

			if(cfg.ShowTotpColumn)
				Program.ColumnProviderPool.Add(new TotpColumnProvider());     // F6

			// F15-C: El indicador de estado de seguridad siempre se registra.
			Program.ColumnProviderPool.Add(new EntryStatusRenderer());        // F15-C
		}

		// ── Columnas por defecto ─────────────────────────────────────────────────

		/// <summary>
		/// Si la lista <c>EntryListColumns</c> está vacía (nueva instalación o primera
		/// ejecución), la rellena con el conjunto de columnas por defecto de Modern Vibe.
		/// </summary>
		public static void EnsureDefaultAceColumns(AceMainWindow mw)
		{
			if(mw == null) return;
			if(mw.EntryListColumns.Count > 0) return; // ya configuradas — no toca nada

			foreach(AceColumn col in GetDefaultAceColumns())
				mw.EntryListColumns.Add(col);
		}

		/// <summary>
		/// Devuelve el conjunto de columnas estándar que debe tener la lista de entradas
		/// en una instalación nueva de KeePass Modern Vibe.
		/// </summary>
		public static List<AceColumn> GetDefaultAceColumns()
		{
			return new List<AceColumn>
			{
				new AceColumn(AceColumnType.Title,              string.Empty, false, 200),
				new AceColumn(AceColumnType.UserName,           string.Empty, false, 170),
				new AceColumn(AceColumnType.Password,           string.Empty, true,  150), // oculta por defecto
				new AceColumn(AceColumnType.Url,                string.Empty, false, 220),
				new AceColumn(AceColumnType.Notes,              string.Empty, false,  90),
				new AceColumn(AceColumnType.Tags,               string.Empty, false,  90),
				new AceColumn(AceColumnType.ExpiryTime,         string.Empty, false, 110),
				new AceColumn(AceColumnType.LastModificationTime, string.Empty, false, 130),
			};
		}

		// ── Auto-resize ──────────────────────────────────────────────────────────

		/// <summary>
		/// Ajusta el ancho de todas las columnas del <paramref name="lv"/> al contenido.
		/// Respeta <see cref="AceColumnConfig.AutoResizeOnOpen"/>.
		/// </summary>
		public static void AutoResizeColumns(ListView lv, AceColumnConfig cfg)
		{
			if(lv == null) return;
			if(cfg != null && !cfg.AutoResizeOnOpen) return;

			foreach(ColumnHeader ch in lv.Columns)
				ch.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}
	}
}
