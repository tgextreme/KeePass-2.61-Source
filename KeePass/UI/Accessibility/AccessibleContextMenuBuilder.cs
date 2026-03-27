// AccessibleContextMenuBuilder.cs
// F14 — Menús más Accesibles y Reorganizados
// Añade cabeceras de categoría al menú contextual de entradas.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KeePass.UI.Accessibility
{
	/// <summary>
	/// Enriquece el menú contextual de la lista de entradas añadiendo cabeceras
	/// de categoría (Copiar / Editar / Seguridad / Organizar) como items visuales.
	/// Las cabeceras se insertan en cada apertura del menú y se eliminan al cerrarlo.
	/// </summary>
	public static class AccessibleContextMenuBuilder
	{
		private const string HeaderTag = "F14_CtxHeader";

		// Pares (nombre del item de anclaje, texto de la cabecera a insertar ANTES de él)
		// El ancla es el primer item de cada sección.
		private static readonly string[] AnchorNames = new string[]
		{
			"m_ctxEntryCopyPassword",   // → cabecera "📋 Copiar"
			"m_ctxEntryAdd",            // → cabecera "✏️ Editar"
			"m_ctxEntryRearrange",      // → cabecera "🎨 Organizar"
		};

		private static readonly string[] HeaderTexts = new string[]
		{
			"\U0001F4CB  Copiar",
			"\u270F\uFE0F  Editar",
			"\U0001F3A8  Organizar",
		};

		/// <summary>
		/// Registra el menú contextual para recibir cabeceras de categoría accesibles.
		/// Llamar una vez tras <c>ConstructContextMenus</c>.
		/// </summary>
		public static void Attach(ContextMenuStrip ctxPwList)
		{
			if(ctxPwList == null) return;

			ctxPwList.Opening += OnOpening;
			ctxPwList.Closed  += OnClosed;
		}

		private static void OnOpening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ContextMenuStrip ctx = sender as ContextMenuStrip;
			if(ctx == null) return;

			// Primero limpiar cabeceras anteriores
			RemoveHeaders(ctx.Items);

			for(int i = 0; i < AnchorNames.Length; i++)
			{
				ToolStripItem[] found = ctx.Items.Find(AnchorNames[i], false);
				if(found == null || found.Length == 0) continue;

				ToolStripItem anchor = found[0];
				int idx = ctx.Items.IndexOf(anchor);
				if(idx < 0) continue;

				// Si ya hay un separador justo antes, asegurarnos de insertar DESPUÉS de él
				// para que la cabecera quede justo encima del primer item de la sección.
				ToolStripMenuItem header = CreateHeader(HeaderTexts[i]);
				ctx.Items.Insert(idx, header);
			}
		}

		private static void OnClosed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			ContextMenuStrip ctx = sender as ContextMenuStrip;
			if(ctx == null) return;
			RemoveHeaders(ctx.Items);
		}

		private static void RemoveHeaders(ToolStripItemCollection items)
		{
			for(int i = items.Count - 1; i >= 0; i--)
			{
				if(HeaderTag.Equals(items[i].Tag as string, StringComparison.Ordinal))
					items.RemoveAt(i);
			}
		}

		private static ToolStripMenuItem CreateHeader(string text)
		{
			var item = new ToolStripMenuItem(text)
			{
				Tag     = (object)HeaderTag,
				Enabled = false,
				Font    = new Font(SystemFonts.MenuFont, FontStyle.Bold)
			};
			// Semi-transparent tint via fore color
			item.ForeColor = Color.FromArgb(60, 100, 160);
			return item;
		}
	}
}
