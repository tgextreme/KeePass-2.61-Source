// Command Palette — Búsqueda rápida de entradas (Ctrl+P)
// Formulario flotante sin bordes, activado por atajo de teclado.
// Muestra resultados en tiempo real usando SearchService.

using System;
using System.Collections.Generic;
using KeePassLib.Security;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Services;
using KeePassLib;

namespace KeePass.Forms
{
	/// <summary>
	/// Paleta de comandos al estilo IDE: campo de búsqueda + lista de resultados.
	/// Se activa desde MainForm con Ctrl+P.
	/// </summary>
	public sealed class CommandPaletteForm : Form
	{
		// ── controles ─────────────────────────────────────────────────────────────
		private TextBox    m_txtQuery;
		private ListView   m_lvResults;

		// ── estado ────────────────────────────────────────────────────────────────
		private readonly PwDatabase      m_db;
		private readonly ISearchService  m_search;
		private Timer    m_debounce;

		/// <summary>Entrada seleccionada por el usuario (null si canceló).</summary>
		public PwEntry SelectedEntry { get; private set; }

		// ── constructor ───────────────────────────────────────────────────────────

		/// <param name="db">Base de datos activa.</param>
		/// <param name="searchService">Servicio de búsqueda (AppServices.Search).</param>
		public CommandPaletteForm(PwDatabase db, ISearchService searchService)
		{
			m_db     = db ?? throw new ArgumentNullException("db");
			m_search = searchService ?? throw new ArgumentNullException("searchService");
			BuildUI();
		}

		// ── construcción UI ───────────────────────────────────────────────────────

		private void BuildUI()
		{
			this.Text            = string.Empty;
			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition   = FormStartPosition.CenterScreen;
			this.Size            = new Size(560, 380);
			this.TopMost         = true;
			this.KeyPreview      = true;
			this.BackColor       = Color.FromArgb(45, 45, 48);

			// Caja de búsqueda
			m_txtQuery = new TextBox {
				Text        = string.Empty,
				Dock        = DockStyle.Top,
				Height      = 40,
				Font        = new Font("Segoe UI", 14f),
				BorderStyle = BorderStyle.None,
				BackColor   = Color.FromArgb(60, 60, 63),
				ForeColor   = Color.FromArgb(220, 220, 220),
				Padding     = new Padding(8, 8, 8, 8),
			};
			m_txtQuery.TextChanged += OnQueryChanged;
			m_txtQuery.KeyDown     += OnQueryKeyDown;

			// Lista de resultados
			m_lvResults = new ListView {
				Dock          = DockStyle.Fill,
				View          = View.Details,
				FullRowSelect = true,
				GridLines     = false,
				HeaderStyle   = ColumnHeaderStyle.None,
				BackColor     = Color.FromArgb(37, 37, 38),
				ForeColor     = Color.FromArgb(200, 200, 200),
				BorderStyle   = BorderStyle.None,
				Font          = new Font("Segoe UI", 10f),
				MultiSelect   = false,
			};
			m_lvResults.Columns.Add("Title",    280);
			m_lvResults.Columns.Add("Username", 150);
			m_lvResults.Columns.Add("URL",      110);
			m_lvResults.DoubleClick  += OnResultDoubleClick;
			m_lvResults.KeyDown      += OnResultKeyDown;

			this.Controls.Add(m_lvResults);
			this.Controls.Add(m_txtQuery);

			// Debounce timer — 150 ms
			m_debounce = new Timer { Interval = 150 };
			m_debounce.Tick += (s, e) => {
				m_debounce.Stop();
				RunSearch(m_txtQuery.Text);
			};

			this.KeyDown += OnFormKeyDown;

			// Mostrar recientes al abrir
			this.Shown += (s, e) => {
				LoadRecent();
				m_txtQuery.Focus();
			};
		}

		// ── búsqueda ──────────────────────────────────────────────────────────────

		private void LoadRecent()
		{
			try
			{
				IList<PwEntry> recent = m_search.GetRecent(m_db, 10);
				PopulateList(recent);
			}
			catch { /* base de datos no disponible */ }
		}

		private void RunSearch(string query)
		{
			if(m_db == null || !m_db.IsOpen) return;

			if(string.IsNullOrWhiteSpace(query))
			{
				LoadRecent();
				return;
			}

			try
			{
				IList<PwEntry> results = m_search.Search(m_db, query.Trim());
				PopulateList(results);
			}
			catch { /* evita crash si la DB se cierra durante la búsqueda */ }
		}

		private void PopulateList(IList<PwEntry> entries)
		{
			m_lvResults.BeginUpdate();
			m_lvResults.Items.Clear();

			if(entries != null)
			{
				foreach(PwEntry pe in entries)
				{
					string title = pe.Strings.ReadSafe(PwDefs.TitleField);
					string user  = pe.Strings.ReadSafe(PwDefs.UserNameField);
					string url   = pe.Strings.ReadSafe(PwDefs.UrlField);

					var item = new ListViewItem(new[] { title, user, url });
					item.Tag = pe;
					m_lvResults.Items.Add(item);
				}
			}

			m_lvResults.EndUpdate();
		}

		// ── eventos ───────────────────────────────────────────────────────────────

		private void OnQueryChanged(object sender, EventArgs e)
		{
			m_debounce.Stop();
			m_debounce.Start();
		}

		private void OnQueryKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Down && m_lvResults.Items.Count > 0)
			{
				m_lvResults.Focus();
				m_lvResults.Items[0].Selected = true;
				e.Handled = true;
			}
			else if(e.KeyCode == Keys.Escape)
			{
				this.DialogResult = DialogResult.Cancel;
				this.Close();
			}
		}

		private void OnResultKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)   { SelectCurrent(); e.Handled = true; }
			if(e.KeyCode == Keys.Escape)  { this.DialogResult = DialogResult.Cancel; this.Close(); }
		}

		private void OnResultDoubleClick(object sender, EventArgs e)
		{
			SelectCurrent();
		}

		private void OnFormKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Escape)
			{
				this.DialogResult = DialogResult.Cancel;
				this.Close();
			}
		}

		private void SelectCurrent()
		{
			if(m_lvResults.SelectedItems.Count == 0) return;
			SelectedEntry     = m_lvResults.SelectedItems[0].Tag as PwEntry;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// ── limpieza ──────────────────────────────────────────────────────────────

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				m_debounce?.Stop();
				m_debounce?.Dispose();
				m_txtQuery?.Dispose();
				m_lvResults?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
