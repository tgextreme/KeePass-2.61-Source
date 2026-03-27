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

// F15-A — Live Search Bar
// Enhances the existing QuickFind ComboBox with live-as-you-type search
// (300 ms debounce), a clear button (X) and a result-count label injected
// into the main toolbar.

using System;
using System.Windows.Forms;

namespace KeePass.UI
{
	/// <summary>
	/// F15-A: Hooks into the existing QuickFind combo to provide live
	/// search, a clear button and a result count label in the toolbar.
	/// </summary>
	internal static class LiveSearchBox
	{
		private static ComboBox m_cmb;
		private static ToolStrip m_toolMain;
		private static ToolStripComboBox m_tbQuickFind;
		private static Action<string> m_searchCallback;

		private static Timer m_timer;
		private static ToolStripButton m_btnClear;
		private static ToolStripLabel m_lblCount;

		private static bool m_bInitialized = false;

		// ----------------------------------------------------------------
		// Public API called by MainForm
		// ----------------------------------------------------------------

		/// <summary>
		/// Attach live-search behaviour to the existing QuickFind combo.
		/// Call once after m_cmbQuickFind is assigned in OnFormLoad.
		/// </summary>
		public static void Attach(ComboBox cmb, ToolStrip toolMain,
			ToolStripComboBox tbQuickFind, Action<string> searchCallback)
		{
			if(m_bInitialized) Detach();

			if(cmb == null || toolMain == null || tbQuickFind == null ||
				searchCallback == null) { return; }

			m_cmb = cmb;
			m_toolMain = toolMain;
			m_tbQuickFind = tbQuickFind;
			m_searchCallback = searchCallback;

			// 300 ms debounce timer (WinForms Timer fires on the UI thread)
			m_timer = new Timer();
			m_timer.Interval = 300;
			m_timer.Tick += OnDebounceElapsed;

			// Wire events on the underlying ComboBox
			m_cmb.TextChanged += OnTextChanged;
			m_cmb.KeyDown += OnKeyDown;

			// Inject clear button right after the QuickFind item
			m_btnClear = new ToolStripButton("X")
			{
				Name = "m_tbLiveSearchClear",
				ToolTipText = "Borrar búsqueda (Esc)",
				DisplayStyle = ToolStripItemDisplayStyle.Text,
				Visible = false,
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(1, 0, 1, 0)
			};
			m_btnClear.Click += OnClearClick;

			// Result count label
			m_lblCount = new ToolStripLabel(string.Empty)
			{
				Name = "m_tbLiveSearchCount",
				ForeColor = System.Drawing.SystemColors.GrayText,
				Visible = false,
				AutoSize = true
			};

			int idx = toolMain.Items.IndexOf(tbQuickFind);
			if(idx >= 0)
			{
				toolMain.Items.Insert(idx + 1, m_btnClear);
				toolMain.Items.Insert(idx + 2, m_lblCount);
			}

			m_bInitialized = true;
		}

		/// <summary>Detach and clean up all resources.</summary>
		public static void Detach()
		{
			if(!m_bInitialized) return;

			if(m_timer != null)
			{
				m_timer.Stop();
				m_timer.Tick -= OnDebounceElapsed;
				m_timer.Dispose();
				m_timer = null;
			}

			if(m_cmb != null)
			{
				m_cmb.TextChanged -= OnTextChanged;
				m_cmb.KeyDown -= OnKeyDown;
				m_cmb = null;
			}

			if(m_toolMain != null)
			{
				if(m_btnClear != null)
				{
					m_toolMain.Items.Remove(m_btnClear);
					m_btnClear.Dispose();
					m_btnClear = null;
				}
				if(m_lblCount != null)
				{
					m_toolMain.Items.Remove(m_lblCount);
					m_lblCount.Dispose();
					m_lblCount = null;
				}
				m_toolMain = null;
			}

			m_tbQuickFind = null;
			m_searchCallback = null;
			m_bInitialized = false;
		}

		/// <summary>
		/// Called by MainForm after each live search completes so the label
		/// can display the result count. Pass -1 to hide (e.g. after clear).
		/// </summary>
		public static void SearchCompleted(int resultCount)
		{
			if(!m_bInitialized || m_cmb == null) return;

			bool bIsSearching = !string.IsNullOrEmpty(m_cmb.Text);

			if(m_lblCount != null)
			{
				if(bIsSearching && resultCount >= 0)
				{
					m_lblCount.Text = resultCount.ToString() +
						(resultCount == 1 ? " resultado" : " resultados");
					m_lblCount.Visible = true;
				}
				else
				{
					m_lblCount.Text = string.Empty;
					m_lblCount.Visible = false;
				}
			}

			if(m_btnClear != null)
				m_btnClear.Visible = bIsSearching;
		}

		// ----------------------------------------------------------------
		// Private event handlers
		// ----------------------------------------------------------------

		private static void OnTextChanged(object sender, EventArgs e)
		{
			// Restart the debounce window on every keystroke
			if(m_timer != null)
			{
				m_timer.Stop();
				m_timer.Start();
			}

			// If the box was cleared, hide the count/clear UI immediately
			if(m_cmb != null && string.IsNullOrEmpty(m_cmb.Text))
			{
				if(m_btnClear != null) m_btnClear.Visible = false;
				if(m_lblCount != null)
				{
					m_lblCount.Text = string.Empty;
					m_lblCount.Visible = false;
				}
			}
		}

		private static void OnDebounceElapsed(object sender, EventArgs e)
		{
			if(m_timer != null) m_timer.Stop();
			if(m_cmb == null || m_searchCallback == null) return;

			string strSearch = m_cmb.Text ?? string.Empty;
			m_searchCallback(strSearch);
		}

		private static void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Escape)
			{
				ClearSearch();
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private static void OnClearClick(object sender, EventArgs e)
		{
			ClearSearch();
		}

		private static void ClearSearch()
		{
			if(m_timer != null) m_timer.Stop();
			if(m_cmb == null) return;

			m_cmb.Text = string.Empty;

			if(m_searchCallback != null)
				m_searchCallback(string.Empty);
		}
	}
}
