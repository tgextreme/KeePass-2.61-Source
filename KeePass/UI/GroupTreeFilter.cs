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

// F15-F — Filtro de árbol de grupos.
// Inserta un TextBox encima del árbol de grupos para filtrar por nombre.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeePass.UI
{
	/// <summary>
	/// F15-F: Añade un cuadro de búsqueda encima del árbol de grupos (m_tvGroups).
	/// Uso: GroupTreeFilter.Attach(m_splitVertical.Panel1, m_tvGroups);
	/// Llamar GroupTreeFilter.Refresh() después de reconstruir el árbol.
	/// </summary>
	internal static class GroupTreeFilter
	{
		private static TextBox m_filterBox  = null;
		private static TreeView m_tvGroups  = null;
		private static string  m_lastFilter = string.Empty;
		private static bool    m_isPlaceholder = true;

		private const string PlaceholderText = "Filtrar grupos\u2026";

		// ────────────────────────────────────────────────────────────────────────

		public static void Attach(Control panel1, TreeView tvGroups)
		{
			if(panel1 == null || tvGroups == null) return;

			m_tvGroups = tvGroups;

			m_filterBox = new TextBox
			{
				Dock      = DockStyle.Top,
				Height    = 22,
				Font      = new Font(SystemFonts.DefaultFont.FontFamily, 8.25f),
				ForeColor = SystemColors.GrayText,
				Text      = PlaceholderText,
			};
			m_isPlaceholder = true;

			m_filterBox.Enter       += OnFilterEnter;
			m_filterBox.Leave       += OnFilterLeave;
			m_filterBox.TextChanged += OnFilterTextChanged;

			panel1.Controls.Add(m_filterBox);
			m_filterBox.BringToFront();
		}

		public static void Detach()
		{
			if(m_filterBox != null)
			{
				m_filterBox.Enter       -= OnFilterEnter;
				m_filterBox.Leave       -= OnFilterLeave;
				m_filterBox.TextChanged -= OnFilterTextChanged;

				if(m_filterBox.Parent != null)
					m_filterBox.Parent.Controls.Remove(m_filterBox);

				m_filterBox.Dispose();
				m_filterBox = null;
			}

			m_tvGroups   = null;
			m_lastFilter = string.Empty;
		}

		/// <summary>
		/// Vuelve a aplicar el filtro activo sobre los nodos actuales del árbol.
		/// Llamar justo después de m_tvGroups.EndUpdate() en UpdateGroupList().
		/// </summary>
		public static void Refresh()
		{
			if(m_filterBox == null || m_tvGroups == null) return;
			ApplyFilter(GetCurrentFilterText());
		}

		// ── Eventos del TextBox ──────────────────────────────────────────────────

		private static void OnFilterEnter(object sender, EventArgs e)
		{
			if(m_isPlaceholder)
			{
				m_isPlaceholder    = false;
				m_filterBox.Text   = string.Empty;
				m_filterBox.ForeColor = SystemColors.WindowText;
			}
		}

		private static void OnFilterLeave(object sender, EventArgs e)
		{
			if(m_filterBox.Text.Length == 0)
			{
				m_isPlaceholder    = true;
				m_filterBox.Text   = PlaceholderText;
				m_filterBox.ForeColor = SystemColors.GrayText;
			}
		}

		private static void OnFilterTextChanged(object sender, EventArgs e)
		{
			if(m_isPlaceholder) return;

			string text = m_filterBox.Text.Trim();
			if(string.Equals(text, m_lastFilter, StringComparison.Ordinal)) return;
			m_lastFilter = text;

			ApplyFilter(text);
		}

		// ── Lógica de filtrado ───────────────────────────────────────────────────

		private static string GetCurrentFilterText()
		{
			if(m_filterBox == null) return string.Empty;
			if(m_isPlaceholder)    return string.Empty;
			return m_filterBox.Text.Trim();
		}

		private static void ApplyFilter(string text)
		{
			if(m_tvGroups == null || m_tvGroups.Nodes.Count == 0) return;

			m_tvGroups.BeginUpdate();
			try
			{
				if(string.IsNullOrEmpty(text))
					ClearFilter(m_tvGroups.Nodes);
				else
				{
					ApplyFilterToNodes(m_tvGroups.Nodes, text.ToLowerInvariant());
					m_tvGroups.ExpandAll();

					// Desplazar hasta el primer coincidente
					ScrollToFirstMatch(m_tvGroups.Nodes);
				}
			}
			finally { m_tvGroups.EndUpdate(); }
		}

		private static bool ApplyFilterToNodes(TreeNodeCollection nodes, string lowerText)
		{
			bool anyMatch = false;
			foreach(TreeNode node in nodes)
			{
				bool childMatch = ApplyFilterToNodes(node.Nodes, lowerText);
				bool selfMatch  = node.Text.ToLowerInvariant().Contains(lowerText);
				bool visible    = childMatch || selfMatch;

				node.ForeColor = visible ? SystemColors.WindowText : SystemColors.GrayText;
				if(visible) anyMatch = true;
			}
			return anyMatch;
		}

		private static void ClearFilter(TreeNodeCollection nodes)
		{
			foreach(TreeNode node in nodes)
			{
				node.ForeColor = SystemColors.WindowText;
				ClearFilter(node.Nodes);
			}
		}

		private static void ScrollToFirstMatch(TreeNodeCollection nodes)
		{
			foreach(TreeNode node in nodes)
			{
				if(node.ForeColor != SystemColors.GrayText)
				{
					node.EnsureVisible();
					return;
				}
				ScrollToFirstMatch(node.Nodes);
			}
		}
	}
}
