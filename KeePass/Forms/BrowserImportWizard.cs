/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — Wizard de 3 pasos.
//
//  Paso 1 — Seleccionar navegador/perfil
//  Paso 2 — Previsualizar credenciales detectadas (con marcado de duplicados)
//  Paso 3 — Seleccionar grupo destino + confirmar importación
//
// Construido completamente por código (sin .Designer.cs).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Integration.BrowserImport;
using KeePass.Services;

using KeePassLib;

namespace KeePass.Forms
{
	/// <summary>
	/// Asistente de importación de contraseñas desde browsers web.
	/// </summary>
	public sealed class BrowserImportWizard : Form
	{
		// ── dependencias ──────────────────────────────────────────────────────────
		private readonly PwDatabase          m_db;
		private readonly IBrowserImportService m_service;

		// ── estado ────────────────────────────────────────────────────────────────
		private List<BrowserProfile>    m_profiles;
		private List<BrowserCredential> m_preview;
		private List<BrowserCredential> m_duplicates;
		private BrowserImportResult     m_result;

		// ── controles del wizard ──────────────────────────────────────────────────
		private Panel      m_pnlHeader;
		private Label      m_lblTitle;
		private Label      m_lblSubtitle;
		private TabControl m_tabs;

		// Paso 1
		private ListBox  m_lstProfiles;
		private Label    m_lblNoProfiles;

		// Paso 2
		private DataGridView m_grid;
		private Label        m_lblDupBadge;

		// Paso 3
		private TreeView m_tvGroups;
		private Label    m_lblResult;

		// Botones de navegación
		private Button m_btnBack;
		private Button m_btnNext;
		private Button m_btnCancel;

		// ── punto de entrada estático ─────────────────────────────────────────────

		/// <summary>
		/// Muestra el wizard de importación y espera a que el usuario lo cierre.
		/// </summary>
		public static void ShowWizard(IWin32Window owner, PwDatabase db)
		{
			if(db == null) throw new ArgumentNullException("db");
			using(BrowserImportWizard w = new BrowserImportWizard(db))
				w.ShowDialog(owner);
		}

		// ── constructor ───────────────────────────────────────────────────────────

		private BrowserImportWizard(PwDatabase db)
		{
			m_db      = db;
			m_service = new BrowserImportService();
			BuildUi();
		}

		// ── construcción de la UI ─────────────────────────────────────────────────

		private void BuildUi()
		{
			this.Text            = "Importar contraseñas desde navegador";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox     = false;
			this.MinimizeBox     = false;
			this.StartPosition   = FormStartPosition.CenterParent;
			this.ClientSize      = new Size(620, 500);

			// — Cabecera —
			m_pnlHeader = new Panel
			{
				Dock      = DockStyle.Top,
				Height    = 64,
				BackColor = Color.FromArgb(0x1E, 0x1E, 0x2E)
			};
			m_lblTitle = new Label
			{
				Text      = "Importar desde navegador",
				ForeColor = Color.White,
				Font      = new Font(this.Font.FontFamily, 12f, FontStyle.Bold),
				AutoSize  = false,
				Bounds    = new Rectangle(16, 8, 500, 24)
			};
			m_lblSubtitle = new Label
			{
				Text      = "Paso 1 de 3 — Seleccionar perfil",
				ForeColor = Color.FromArgb(180, 180, 200),
				AutoSize  = false,
				Bounds    = new Rectangle(16, 34, 500, 20)
			};
			m_pnlHeader.Controls.AddRange(new Control[] { m_lblTitle, m_lblSubtitle });

			// — TabControl (ocultar tabs para simular wizard) —
			m_tabs = new TabControl
			{
				Appearance = TabAppearance.FlatButtons,
				ItemSize   = new Size(0, 1),
				SizeMode   = TabSizeMode.Fixed,
				Bounds     = new Rectangle(0, 64, 620, 388)
			};

			m_tabs.TabPages.Add(BuildStep1());
			m_tabs.TabPages.Add(BuildStep2());
			m_tabs.TabPages.Add(BuildStep3());

			// — Botones de navegación —
			m_btnBack = new Button
			{
				Text    = "< Atrás",
				Enabled = false,
				Bounds  = new Rectangle(10, 460, 90, 28)
			};
			m_btnNext = new Button
			{
				Text   = "Siguiente >",
				Bounds = new Rectangle(106, 460, 110, 28)
			};
			m_btnCancel = new Button
			{
				Text         = "Cancelar",
				DialogResult = DialogResult.Cancel,
				Bounds       = new Rectangle(518, 460, 90, 28)
			};
			this.CancelButton = m_btnCancel;

			m_btnBack.Click   += OnBack;
			m_btnNext.Click   += OnNext;

			this.Controls.AddRange(new Control[] {
				m_pnlHeader, m_tabs,
				m_btnBack, m_btnNext, m_btnCancel
			});

			this.Load += OnLoad;
		}

		private TabPage BuildStep1()
		{
			var page = new TabPage { Text = "Paso1" };

			var lblInfo = new Label
			{
				Text     = "Se han detectado los siguientes perfiles de navegador. "
				         + "Selecciona el perfil del que quieres importar contraseñas:",
				AutoSize = false,
				Bounds   = new Rectangle(12, 12, 590, 36)
			};

			m_lstProfiles = new ListBox
			{
				Bounds         = new Rectangle(12, 52, 590, 300),
				IntegralHeight = false
			};

			m_lblNoProfiles = new Label
			{
				Text      = "No se encontró ningún perfil de Chrome, Edge, Brave o Firefox.",
				ForeColor = Color.Gray,
				AutoSize  = false,
				TextAlign = ContentAlignment.MiddleCenter,
				Bounds    = new Rectangle(12, 180, 590, 40),
				Visible   = false
			};

			page.Controls.AddRange(new Control[] { lblInfo, m_lstProfiles, m_lblNoProfiles });
			return page;
		}

		private TabPage BuildStep2()
		{
			var page = new TabPage { Text = "Paso2" };

			var lblInfo = new Label
			{
				Text     = "Credenciales detectadas. Desmarca las que NO quieras importar:",
				AutoSize = false,
				Bounds   = new Rectangle(12, 12, 500, 20)
			};

			m_lblDupBadge = new Label
			{
				Text      = string.Empty,
				ForeColor = Color.OrangeRed,
				AutoSize  = false,
				Bounds    = new Rectangle(12, 36, 590, 20)
			};

			m_grid = new DataGridView
			{
				Bounds                  = new Rectangle(12, 60, 590, 290),
				AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
				RowHeadersVisible       = false,
				AllowUserToAddRows      = false,
				AllowUserToDeleteRows   = false,
				SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
				ReadOnly                = false,
				AllowUserToResizeRows   = false
			};

			// Columna checkbox "Importar"
			var colImport = new DataGridViewCheckBoxColumn
			{
				HeaderText = "✓",
				Width      = 32,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			var colTitle = new DataGridViewTextBoxColumn
			{ HeaderText = "Título",   ReadOnly = true };
			var colUser  = new DataGridViewTextBoxColumn
			{ HeaderText = "Usuario",  ReadOnly = true };
			var colUrl   = new DataGridViewTextBoxColumn
			{ HeaderText = "URL",      ReadOnly = true };
			var colDup   = new DataGridViewTextBoxColumn
			{ HeaderText = "Duplicado", ReadOnly = true, Width = 80,
			  AutoSizeMode = DataGridViewAutoSizeColumnMode.None };

			m_grid.Columns.AddRange(colImport, colTitle, colUser, colUrl, colDup);

			page.Controls.AddRange(new Control[] { lblInfo, m_lblDupBadge, m_grid });
			return page;
		}

		private TabPage BuildStep3()
		{
			var page = new TabPage { Text = "Paso3" };

			var lblInfo = new Label
			{
				Text     = "Selecciona el grupo de destino donde se añadirán las entradas:",
				AutoSize = false,
				Bounds   = new Rectangle(12, 12, 590, 20)
			};

			m_tvGroups = new TreeView
			{
				Bounds       = new Rectangle(12, 36, 590, 280),
				HideSelection = false
			};

			m_lblResult = new Label
			{
				Text      = string.Empty,
				AutoSize  = false,
				Bounds    = new Rectangle(12, 322, 590, 40),
				ForeColor = Color.DarkGreen,
				TextAlign = ContentAlignment.MiddleCenter
			};

			page.Controls.AddRange(new Control[] { lblInfo, m_tvGroups, m_lblResult });
			return page;
		}

		// ── eventos ───────────────────────────────────────────────────────────────

		private void OnLoad(object sender, EventArgs e)
		{
			// Cargar perfiles en el paso 1.
			try { m_profiles = m_service.GetAvailableProfiles(); }
			catch { m_profiles = new List<BrowserProfile>(); }

			m_lstProfiles.Items.Clear();
			if(m_profiles.Count == 0)
			{
				m_lstProfiles.Visible    = false;
				m_lblNoProfiles.Visible  = true;
				m_btnNext.Enabled        = false;
			}
			else
			{
				foreach(BrowserProfile p in m_profiles) m_lstProfiles.Items.Add(p);
				if(m_lstProfiles.Items.Count > 0) m_lstProfiles.SelectedIndex = 0;
			}
		}

		private void OnNext(object sender, EventArgs e)
		{
			int current = m_tabs.SelectedIndex;

			if(current == 0) // Paso 1 → 2
			{
				if(m_lstProfiles.SelectedItem == null) return;
				BrowserProfile selected = (BrowserProfile)m_lstProfiles.SelectedItem;
				PopulateStep2(selected);
				GoToStep(1, "Paso 2 de 3 — Previsualizar credenciales");
				m_btnBack.Enabled = true;
			}
			else if(current == 1) // Paso 2 → 3
			{
				PopulateStep3();
				GoToStep(2, "Paso 3 de 3 — Confirmar importación");
				m_btnNext.Text = "Importar";
			}
			else if(current == 2) // Confirmar importación
			{
				DoImport();
			}
		}

		private void OnBack(object sender, EventArgs e)
		{
			int current = m_tabs.SelectedIndex;
			if(current == 1)
			{
				GoToStep(0, "Paso 1 de 3 — Seleccionar perfil");
				m_btnBack.Enabled = false;
			}
			else if(current == 2)
			{
				GoToStep(1, "Paso 2 de 3 — Previsualizar credenciales");
				m_btnNext.Text = "Siguiente >";
				m_lblResult.Text = string.Empty;
			}
		}

		// ── lógica de pasos ───────────────────────────────────────────────────────

		private void PopulateStep2(BrowserProfile profile)
		{
			m_grid.Rows.Clear();
			m_preview    = null;
			m_duplicates = null;

			try { m_preview = m_service.PreviewCredentials(profile); }
			catch(Exception ex)
			{
				MessageBox.Show("Error al leer credenciales: " + ex.Message,
					"KeePass", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				m_preview = new List<BrowserCredential>();
			}

			m_duplicates = m_service.DetectDuplicates(m_preview, m_db);
			var dupSet   = new System.Collections.Generic.HashSet<BrowserCredential>(m_duplicates);

			foreach(BrowserCredential cred in m_preview)
			{
				bool isDup = dupSet.Contains(cred);
				m_grid.Rows.Add(!isDup, cred.Title, cred.Username, cred.Url,
					isDup ? "Duplicado" : string.Empty);
			}

			int dupCount = m_duplicates.Count;
			m_lblDupBadge.Text = dupCount > 0
				? string.Format("⚠ {0} entrada{1} ya existe{2} en la base de datos.",
					dupCount, dupCount == 1 ? string.Empty : "s",
					dupCount == 1 ? " " : "n ")
				: string.Empty;
		}

		private void PopulateStep3()
		{
			m_tvGroups.Nodes.Clear();
			if(m_db != null && m_db.IsOpen && m_db.RootGroup != null)
				FillTreeView(m_tvGroups.Nodes, m_db.RootGroup);
			if(m_tvGroups.Nodes.Count > 0)
				m_tvGroups.SelectedNode = m_tvGroups.Nodes[0];
		}

		private void FillTreeView(TreeNodeCollection nodes, PwGroup group)
		{
			var node = new TreeNode(group.Name) { Tag = group };
			nodes.Add(node);
			foreach(PwGroup child in group.Groups)
				FillTreeView(node.Nodes, child);
			node.Expand();
		}

		private void DoImport()
		{
			PwGroup targetGroup = null;
			if(m_tvGroups.SelectedNode != null)
				targetGroup = m_tvGroups.SelectedNode.Tag as PwGroup;
			if(targetGroup == null && m_db != null)
				targetGroup = m_db.RootGroup;
			if(targetGroup == null) return;

			// Recoger solo las filas marcadas para importar.
			var toImport = new List<BrowserCredential>();
			for(int i = 0; i < m_grid.Rows.Count; i++)
			{
				object chk = m_grid.Rows[i].Cells[0].Value;
				if(chk is bool && (bool)chk)
					toImport.Add(m_preview[i]);
			}

			if(toImport.Count == 0)
			{
				MessageBox.Show("No hay entradas seleccionadas para importar.",
					"KeePass", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			m_result = m_service.Import(toImport, targetGroup, m_db);
			m_lblResult.Text = m_result.ToString();
			m_btnNext.Enabled  = false;
			m_btnBack.Enabled  = false;
		}

		private void GoToStep(int index, string subtitle)
		{
			m_tabs.SelectedIndex = index;
			m_lblSubtitle.Text   = subtitle;
		}
	}
}
