/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F1 — HIBP Checker — formulario de progreso y resultados.
// Escanea todas las entradas de la base activa contra la API Have I Been Pwned.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Services;

using KeePassLib;
using KeePassLib.Delegates;

namespace KeePass.Forms
{
	/// <summary>
	/// Diálogo que escanea las contraseñas contra HIBP y muestra los resultados.
	/// Se construye completamente por código (sin .Designer.cs).
	/// </summary>
	public sealed class HibpCheckForm : Form
	{
		private readonly PwDatabase m_db;

		private BackgroundWorker       m_worker;
		private List<PwnedEntry>       m_results;

		// Controles
		private Label       m_lblStatus;
		private ProgressBar m_pb;
		private ListView    m_lv;
		private Button      m_btnCancel;

		// ── punto de entrada estático ────────────────────────────────────────────

		/// <summary>
		/// Abre el diálogo, escanea la base de datos activa y muestra los resultados.
		/// Llama a este método desde el hilo de la interfaz.
		/// </summary>
		public static void ShowAndCheck(IWin32Window owner, PwDatabase db)
		{
			using(HibpCheckForm f = new HibpCheckForm(db))
				f.ShowDialog(owner);
		}

		// ── constructor ──────────────────────────────────────────────────────────

		private HibpCheckForm(PwDatabase db)
		{
			m_db = db;
			BuildUI();
		}

		private void BuildUI()
		{
			this.Text            = "Comprobar filtraciones (Have I Been Pwned)";
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.StartPosition   = FormStartPosition.CenterParent;
			this.Size            = new Size(620, 440);
			this.MinimumSize     = new Size(480, 340);

			// Etiqueta de estado
			m_lblStatus          = new Label();
			m_lblStatus.Text     = "Iniciando comprobación...";
			m_lblStatus.Location = new Point(8, 8);
			m_lblStatus.AutoSize = true;

			// Barra de progreso
			m_pb          = new ProgressBar();
			m_pb.Location = new Point(8, 32);
			m_pb.Width    = 594;
			m_pb.Height   = 20;
			m_pb.Minimum  = 0;
			m_pb.Maximum  = 100;
			m_pb.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

			// Lista de resultados
			m_lv              = new ListView();
			m_lv.Location     = new Point(8, 62);
			m_lv.Size         = new Size(594, 318);
			m_lv.Anchor       = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			m_lv.View         = View.Details;
			m_lv.FullRowSelect = true;
			m_lv.GridLines    = true;
			m_lv.Columns.Add("Título",              180);
			m_lv.Columns.Add("Usuario",             120);
			m_lv.Columns.Add("URL",                 180);
			m_lv.Columns.Add("Veces filtrada",       90);

			// Botón Cancelar / Cerrar
			m_btnCancel          = new Button();
			m_btnCancel.Text     = "Cancelar";
			m_btnCancel.Width    = 90;
			m_btnCancel.Height   = 26;
			m_btnCancel.Anchor   = AnchorStyles.Bottom | AnchorStyles.Right;
			m_btnCancel.Location = new Point(522, 390);
			m_btnCancel.Click   += OnBtnCancel;

			this.Controls.AddRange(new Control[]
				{ m_lblStatus, m_pb, m_lv, m_btnCancel });

			this.Load += OnFormLoad;
		}

		// ── ciclo de vida ────────────────────────────────────────────────────────

		private void OnFormLoad(object sender, EventArgs e)
		{
			StartScan();
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if(m_worker != null && m_worker.IsBusy)
			{
				m_worker.CancelAsync();
				e.Cancel = true; // esperar confirmación del hilo de fondo
			}
			else base.OnFormClosing(e);
		}

		private void OnBtnCancel(object sender, EventArgs e)
		{
			if(m_worker != null && m_worker.IsBusy)
			{
				m_btnCancel.Enabled = false;
				m_btnCancel.Text    = "Cancelando...";
				m_worker.CancelAsync();
			}
			else
			{
				this.Close();
			}
		}

		// ── escaneo ──────────────────────────────────────────────────────────────

		private void StartScan()
		{
			m_worker = new BackgroundWorker();
			m_worker.WorkerSupportsCancellation = true;
			m_worker.WorkerReportsProgress      = true;
			m_worker.DoWork             += OnDoWork;
			m_worker.ProgressChanged    += OnProgressChanged;
			m_worker.RunWorkerCompleted += OnWorkerCompleted;
			m_worker.RunWorkerAsync();
		}

		private void OnDoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker bw = (BackgroundWorker)sender;

			GAction<int, int> onProgress = delegate(int current, int total)
			{
				int pct = (total > 0) ? ((current * 100) / total) : 100;
				bw.ReportProgress(Math.Min(pct, 100), 
					string.Format("Comprobando entrada {0} de {1}...", current, total));
			};

			GFunc<bool> cancelQuery = delegate() { return bw.CancellationPending; };

			List<PwnedEntry> results = HibpService.CheckAll(m_db, onProgress, cancelQuery);

			if(bw.CancellationPending) e.Cancel = true;
			e.Result = results;
		}

		private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			m_pb.Value = Math.Min(e.ProgressPercentage, 100);

			string msg = e.UserState as string;
			if(!string.IsNullOrEmpty(msg))
				m_lblStatus.Text = msg;
		}

		private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			m_pb.Value          = 100;
			m_btnCancel.Text    = "Cerrar";
			m_btnCancel.Enabled = true;

			if(e.Error != null)
			{
				m_lblStatus.Text = "Error: " + e.Error.Message;
				return;
			}

			m_results = (e.Result as List<PwnedEntry>) ?? new List<PwnedEntry>();

			// Rellenar la lista
			foreach(PwnedEntry p in m_results)
			{
				string title  = p.Entry.Strings.ReadSafe(PwDefs.TitleField);
				string user   = p.Entry.Strings.ReadSafe(PwDefs.UserNameField);
				string url    = p.Entry.Strings.ReadSafe(PwDefs.UrlField);
				string count  = p.Count.ToString("N0");

				ListViewItem lvi = new ListViewItem(title);
				lvi.SubItems.Add(user);
				lvi.SubItems.Add(url);
				lvi.SubItems.Add(count);
				lvi.BackColor = Color.FromArgb(255, 220, 220); // fondo rojo pastel
				m_lv.Items.Add(lvi);
			}

			if(m_results.Count == 0)
			{
				m_lblStatus.Text = e.Cancelled
					? "Cancelado. No se encontraron contraseñas comprometidas hasta el momento."
					: "✓ Ninguna contraseña aparece en la base de datos de filtraciones conocidas.";
			}
			else
			{
				m_lblStatus.Text = e.Cancelled
					? string.Format("Cancelado. {0} contraseña(s) comprometida(s) encontrada(s) hasta ahora.", m_results.Count)
					: string.Format("⚠ {0} contraseña(s) comprometida(s) encontrada(s). ¡Cámbia las cuanto antes!", m_results.Count);
				m_lblStatus.ForeColor = Color.DarkRed;
			}

			// Si fue cancelado pero hay resultados, permitir cerrar
			if(e.Cancelled)
				this.FormClosing -= null; // liberar el bloqueo
		}
	}
}
