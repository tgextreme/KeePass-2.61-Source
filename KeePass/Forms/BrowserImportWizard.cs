/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde CSV de navegador — wizard de 3 pasos.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using KeePass.Integration.BrowserImport;
using KeePass.Services;

using KeePassLib;

namespace KeePass.Forms
{
	public sealed class BrowserImportWizard : Form
	{
		private readonly PwDatabase m_db;
		private readonly IBrowserImportService m_service;

		private List<BrowserCredential> m_preview;
		private List<BrowserCredential> m_duplicates;
		private BrowserImportResult m_result;
		private List<BrowserCsvFormatInfo> m_formats;

		private Panel m_pnlHeader;
		private Label m_lblTitle;
		private Label m_lblSubtitle;
		private TabControl m_tabs;

		// Paso 1
		private ComboBox m_cmbFormat;
		private TextBox m_tbCsvPath;
		private Button m_btnBrowseCsv;
		private Label m_lblHeaderHint;
		private Label m_lblExportHint;

		// Paso 2
		private DataGridView m_grid;
		private Label m_lblDupBadge;
		private Button m_btnSelectAll;
		private Button m_btnSelectNone;
		private Button m_btnExcludeDup;

		// Paso 3
		private TreeView m_tvGroups;
		private Label m_lblResult;

		private Button m_btnBack;
		private Button m_btnNext;
		private Button m_btnCancel;

		public static void ShowWizard(IWin32Window owner, PwDatabase db)
		{
			if(db == null) throw new ArgumentNullException("db");
			using(BrowserImportWizard w = new BrowserImportWizard(db))
				w.ShowDialog(owner);
		}

		private BrowserImportWizard(PwDatabase db)
		{
			m_db = db;
			m_service = new BrowserImportService();
			BuildUi();
		}

		private void BuildUi()
		{
			Text = "Importar desde CSV de navegador";
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;
			ClientSize = new Size(700, 540);

			m_pnlHeader = new Panel
			{
				Dock = DockStyle.Top,
				Height = 70,
				BackColor = Color.FromArgb(0x1E, 0x1E, 0x2E)
			};
			m_lblTitle = new Label
			{
				Text = "Importar contraseñas (CSV)",
				ForeColor = Color.White,
				Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
				AutoSize = false,
				Bounds = new Rectangle(16, 9, 620, 24)
			};
			m_lblSubtitle = new Label
			{
				Text = "Paso 1 de 3 - Seleccionar archivo CSV",
				ForeColor = Color.FromArgb(180, 180, 200),
				AutoSize = false,
				Bounds = new Rectangle(16, 36, 620, 20)
			};
			m_pnlHeader.Controls.AddRange(new Control[] { m_lblTitle, m_lblSubtitle });

			m_tabs = new TabControl
			{
				Appearance = TabAppearance.FlatButtons,
				ItemSize = new Size(0, 1),
				SizeMode = TabSizeMode.Fixed,
				Bounds = new Rectangle(0, 70, 700, 415)
			};
			m_tabs.TabPages.Add(BuildStep1());
			m_tabs.TabPages.Add(BuildStep2());
			m_tabs.TabPages.Add(BuildStep3());

			m_btnBack = new Button
			{
				Text = "< Atrás",
				Enabled = false,
				Bounds = new Rectangle(10, 500, 90, 28)
			};
			m_btnNext = new Button
			{
				Text = "Siguiente >",
				Bounds = new Rectangle(106, 500, 110, 28)
			};
			m_btnCancel = new Button
			{
				Text = "Cancelar",
				DialogResult = DialogResult.Cancel,
				Bounds = new Rectangle(598, 500, 90, 28)
			};
			CancelButton = m_btnCancel;

			m_btnBack.Click += OnBack;
			m_btnNext.Click += OnNext;

			Controls.AddRange(new Control[]
			{
				m_pnlHeader,
				m_tabs,
				m_btnBack,
				m_btnNext,
				m_btnCancel
			});

			Load += OnLoad;
		}

		private TabPage BuildStep1()
		{
			var page = new TabPage { Text = "Paso1" };

			var lblInfo = new Label
			{
				Text = "1) Exporta contraseñas desde tu navegador a CSV. 2) Selecciona formato y archivo.",
				AutoSize = false,
				Bounds = new Rectangle(12, 12, 670, 20)
			};

			var lblFormat = new Label
			{
				Text = "Formato del CSV:",
				AutoSize = true,
				Bounds = new Rectangle(12, 46, 140, 18)
			};
			m_cmbFormat = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Bounds = new Rectangle(12, 68, 360, 25)
			};
			m_cmbFormat.SelectedIndexChanged += OnFormatChanged;

			var lblPath = new Label
			{
				Text = "Archivo CSV:",
				AutoSize = true,
				Bounds = new Rectangle(12, 108, 120, 18)
			};
			m_tbCsvPath = new TextBox
			{
				Bounds = new Rectangle(12, 130, 560, 25)
			};
			m_tbCsvPath.TextChanged += delegate { UpdateStep1State(); };

			m_btnBrowseCsv = new Button
			{
				Text = "Examinar...",
				Bounds = new Rectangle(580, 129, 100, 27)
			};
			m_btnBrowseCsv.Click += OnBrowseCsv;

			m_lblHeaderHint = new Label
			{
				Text = string.Empty,
				ForeColor = Color.FromArgb(30, 30, 30),
				AutoSize = false,
				Bounds = new Rectangle(12, 172, 670, 32)
			};

			m_lblExportHint = new Label
			{
				Text = string.Empty,
				ForeColor = Color.DimGray,
				AutoSize = false,
				Bounds = new Rectangle(12, 210, 670, 46)
			};

			var lblTip = new Label
			{
				Text = "Tip: si no sabes el formato exacto, prueba primero con 'CSV genérico'.",
				ForeColor = Color.SteelBlue,
				AutoSize = false,
				Bounds = new Rectangle(12, 266, 670, 22)
			};

			page.Controls.AddRange(new Control[]
			{
				lblInfo, lblFormat, m_cmbFormat, lblPath, m_tbCsvPath, m_btnBrowseCsv,
				m_lblHeaderHint, m_lblExportHint, lblTip
			});

			return page;
		}

		private TabPage BuildStep2()
		{
			var page = new TabPage { Text = "Paso2" };

			var lblInfo = new Label
			{
				Text = "Previsualización: desmarca lo que NO quieras importar.",
				AutoSize = false,
				Bounds = new Rectangle(12, 12, 500, 20)
			};

			m_lblDupBadge = new Label
			{
				Text = string.Empty,
				ForeColor = Color.OrangeRed,
				AutoSize = false,
				Bounds = new Rectangle(12, 36, 660, 20)
			};

			m_btnSelectAll = new Button
			{
				Text = "Marcar todo",
				Bounds = new Rectangle(12, 60, 90, 25)
			};
			m_btnSelectAll.Click += delegate { SetAllImportChecks(true); };

			m_btnSelectNone = new Button
			{
				Text = "Desmarcar todo",
				Bounds = new Rectangle(108, 60, 105, 25)
			};
			m_btnSelectNone.Click += delegate { SetAllImportChecks(false); };

			m_btnExcludeDup = new Button
			{
				Text = "Quitar duplicados",
				Bounds = new Rectangle(219, 60, 115, 25)
			};
			m_btnExcludeDup.Click += OnExcludeDuplicates;

			m_grid = new DataGridView
			{
				Bounds = new Rectangle(12, 92, 670, 290),
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				RowHeadersVisible = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				ReadOnly = false,
				AllowUserToResizeRows = false
			};

			var colImport = new DataGridViewCheckBoxColumn
			{
				HeaderText = "✓",
				Width = 34,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			var colTitle = new DataGridViewTextBoxColumn { HeaderText = "Título", ReadOnly = true };
			var colUser = new DataGridViewTextBoxColumn { HeaderText = "Usuario", ReadOnly = true };
			var colUrl = new DataGridViewTextBoxColumn { HeaderText = "URL", ReadOnly = true };
			var colDup = new DataGridViewTextBoxColumn
			{
				HeaderText = "Duplicado",
				ReadOnly = true,
				Width = 86,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			m_grid.Columns.AddRange(colImport, colTitle, colUser, colUrl, colDup);

			page.Controls.AddRange(new Control[]
			{
				lblInfo, m_lblDupBadge, m_btnSelectAll, m_btnSelectNone, m_btnExcludeDup, m_grid
			});
			return page;
		}

		private TabPage BuildStep3()
		{
			var page = new TabPage { Text = "Paso3" };

			var lblInfo = new Label
			{
				Text = "Selecciona el grupo de destino donde se añadirán las entradas:",
				AutoSize = false,
				Bounds = new Rectangle(12, 12, 670, 20)
			};

			m_tvGroups = new TreeView
			{
				Bounds = new Rectangle(12, 36, 670, 300),
				HideSelection = false
			};

			m_lblResult = new Label
			{
				Text = string.Empty,
				AutoSize = false,
				Bounds = new Rectangle(12, 344, 670, 40),
				ForeColor = Color.DarkGreen,
				TextAlign = ContentAlignment.MiddleCenter
			};

			page.Controls.AddRange(new Control[] { lblInfo, m_tvGroups, m_lblResult });
			return page;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			try { m_formats = m_service.GetSupportedFormats(); }
			catch { m_formats = new List<BrowserCsvFormatInfo>(); }

			m_cmbFormat.Items.Clear();
			for(int i = 0; i < m_formats.Count; ++i)
				m_cmbFormat.Items.Add(m_formats[i]);

			if(m_cmbFormat.Items.Count > 0)
				m_cmbFormat.SelectedIndex = 0;

			UpdateStep1State();
		}

		private void OnFormatChanged(object sender, EventArgs e)
		{
			BrowserCsvFormatInfo fi = GetSelectedFormatInfo();
			if(fi == null)
			{
				m_lblHeaderHint.Text = string.Empty;
				m_lblExportHint.Text = string.Empty;
				return;
			}

			m_lblHeaderHint.Text = "Cabecera esperada: " + fi.HeaderHint;
			m_lblExportHint.Text = "Cómo exportar: " + fi.ExportHint;
			UpdateStep1State();
		}

		private void OnBrowseCsv(object sender, EventArgs e)
		{
			using(OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*";
				ofd.Title = "Seleccionar archivo CSV exportado";
				ofd.CheckFileExists = true;
				if(ofd.ShowDialog(this) == DialogResult.OK)
					m_tbCsvPath.Text = ofd.FileName;
			}
		}

		private void OnNext(object sender, EventArgs e)
		{
			int current = m_tabs.SelectedIndex;

			if(current == 0)
			{
				if(!ValidateStep1()) return;

				BrowserCsvFormatInfo fi = GetSelectedFormatInfo();
				PopulateStep2(m_tbCsvPath.Text.Trim(), fi.Format);
				GoToStep(1, "Paso 2 de 3 - Revisar y seleccionar credenciales");
				m_btnBack.Enabled = true;
			}
			else if(current == 1)
			{
				PopulateStep3();
				GoToStep(2, "Paso 3 de 3 - Importar al grupo destino");
				m_btnNext.Text = "Importar";
			}
			else if(current == 2)
			{
				DoImport();
			}
		}

		private void OnBack(object sender, EventArgs e)
		{
			int current = m_tabs.SelectedIndex;
			if(current == 1)
			{
				GoToStep(0, "Paso 1 de 3 - Seleccionar archivo CSV");
				m_btnBack.Enabled = false;
			}
			else if(current == 2)
			{
				GoToStep(1, "Paso 2 de 3 - Revisar y seleccionar credenciales");
				m_btnNext.Text = "Siguiente >";
				m_lblResult.Text = string.Empty;
			}
		}

		private bool ValidateStep1()
		{
			if(GetSelectedFormatInfo() == null)
			{
				MessageBox.Show("Selecciona un formato de CSV.", "KeePass",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			string path = (m_tbCsvPath.Text ?? string.Empty).Trim();
			if(string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				MessageBox.Show("Selecciona un archivo CSV válido.", "KeePass",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			return true;
		}

		private void PopulateStep2(string csvPath, BrowserCsvFormat format)
		{
			m_grid.Rows.Clear();
			m_preview = null;
			m_duplicates = null;

			try { m_preview = m_service.PreviewCredentialsFromCsv(csvPath, format); }
			catch(Exception ex)
			{
				MessageBox.Show("Error leyendo CSV: " + ex.Message, "KeePass",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				m_preview = new List<BrowserCredential>();
			}

			m_duplicates = m_service.DetectDuplicates(m_preview, m_db);
			var dupSet = new HashSet<BrowserCredential>(m_duplicates);

			for(int i = 0; i < m_preview.Count; ++i)
			{
				BrowserCredential cred = m_preview[i];
				bool isDup = dupSet.Contains(cred);
				m_grid.Rows.Add(!isDup, cred.Title, cred.Username, cred.Url,
					isDup ? "Duplicado" : string.Empty);
			}

			int dupCount = m_duplicates.Count;
			m_lblDupBadge.Text = (dupCount > 0)
				? string.Format("{0} entrada(s) ya existen en la base de datos.", dupCount)
				: string.Empty;
		}

		private void OnExcludeDuplicates(object sender, EventArgs e)
		{
			for(int i = 0; i < m_grid.Rows.Count; ++i)
			{
				string dupText = Convert.ToString(m_grid.Rows[i].Cells[4].Value);
				if(string.Equals(dupText, "Duplicado", StringComparison.OrdinalIgnoreCase))
					m_grid.Rows[i].Cells[0].Value = false;
			}
		}

		private void SetAllImportChecks(bool isChecked)
		{
			for(int i = 0; i < m_grid.Rows.Count; ++i)
				m_grid.Rows[i].Cells[0].Value = isChecked;
		}

		private void PopulateStep3()
		{
			m_tvGroups.Nodes.Clear();
			if((m_db != null) && m_db.IsOpen && (m_db.RootGroup != null))
				FillTreeView(m_tvGroups.Nodes, m_db.RootGroup);
			if(m_tvGroups.Nodes.Count > 0)
				m_tvGroups.SelectedNode = m_tvGroups.Nodes[0];
		}

		private void FillTreeView(TreeNodeCollection nodes, PwGroup group)
		{
			TreeNode node = new TreeNode(group.Name);
			node.Tag = group;
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
			if((targetGroup == null) && (m_db != null))
				targetGroup = m_db.RootGroup;
			if(targetGroup == null) return;

			var toImport = new List<BrowserCredential>();
			for(int i = 0; i < m_grid.Rows.Count; ++i)
			{
				object chk = m_grid.Rows[i].Cells[0].Value;
				if((chk is bool) && ((bool)chk))
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
			m_btnNext.Enabled = false;
			m_btnBack.Enabled = false;
		}

		private void GoToStep(int index, string subtitle)
		{
			m_tabs.SelectedIndex = index;
			m_lblSubtitle.Text = subtitle;
		}

		private void UpdateStep1State()
		{
			if(m_tabs == null || m_btnNext == null || m_cmbFormat == null || m_tbCsvPath == null)
				return;

			if(m_tabs.SelectedIndex != 0) return;

			bool hasFormat = (GetSelectedFormatInfo() != null);
			bool hasPath = !string.IsNullOrEmpty((m_tbCsvPath.Text ?? string.Empty).Trim());
			m_btnNext.Enabled = hasFormat && hasPath;
		}

		private BrowserCsvFormatInfo GetSelectedFormatInfo()
		{
			return m_cmbFormat.SelectedItem as BrowserCsvFormatInfo;
		}
	}
}
