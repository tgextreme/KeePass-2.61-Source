/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F3 — Favicon Downloader — diálogo de progreso.

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
	/// Dialog de progreso para la descarga masiva de favicons.
	/// Se construye completamente por código (sin .Designer.cs).
	/// </summary>
	public sealed class FaviconDownloadForm : Form
	{
		private readonly PwEntry[]  m_entries;
		private readonly PwDatabase m_db;

		private BackgroundWorker    m_worker;
		private List<FaviconResult> m_results;

		private ProgressBar  m_pb;
		private ListView     m_lv;
		private Button       m_btnClose;
		private Label        m_lblStatus;

		/// <summary>Resultados de todas las entradas procesadas (null si aún no terminó).</summary>
		public List<FaviconResult> Results { get { return m_results; } }

		// ── punto de entrada estático ────────────────────────────────────────────

		/// <summary>
		/// Muestra el diálogo, realiza las descargas y devuelve los resultados.
		/// Llama a este método desde el hilo UI.
		/// </summary>
		public static List<FaviconResult> ShowAndDownload(IWin32Window owner,
			PwEntry[] entries, PwDatabase db)
		{
			using(FaviconDownloadForm f = new FaviconDownloadForm(entries, db))
			{
				f.ShowDialog(owner);
				return f.m_results ?? new List<FaviconResult>();
			}
		}

		// ── constructor ──────────────────────────────────────────────────────────

		public FaviconDownloadForm(PwEntry[] entries, PwDatabase db)
		{
			m_entries = entries;
			m_db = db;
			BuildUI();
		}

		private void BuildUI()
		{
			this.Text = "Descargar iconos del sitio web";
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Size = new Size(520, 380);
			this.MinimumSize = new Size(420, 300);

			m_lblStatus = new Label();
			m_lblStatus.Text = "Iniciando descarga...";
			m_lblStatus.Location = new Point(8, 8);
			m_lblStatus.AutoSize = true;

			m_pb = new ProgressBar();
			m_pb.Location = new Point(8, 32);
			m_pb.Width = 492;
			m_pb.Height = 20;
			m_pb.Minimum = 0;
			m_pb.Maximum = 100;
			m_pb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

			m_lv = new ListView();
			m_lv.Location = new Point(8, 62);
			m_lv.Size = new Size(492, 256);
			m_lv.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			m_lv.View = View.Details;
			m_lv.FullRowSelect = true;
			m_lv.GridLines = true;
			m_lv.Columns.Add("URL/Dominio", 340);
			m_lv.Columns.Add("Estado", 140);

			m_btnClose = new Button();
			m_btnClose.Text = "Cancelar";
			m_btnClose.Width = 90;
			m_btnClose.Height = 26;
			m_btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			m_btnClose.Location = new Point(410, 330);
			m_btnClose.Click += OnBtnClose;

			this.Controls.AddRange(new Control[]
				{ m_lblStatus, m_pb, m_lv, m_btnClose });

			this.Load += OnFormLoad;
		}

		// ── lifecycle ────────────────────────────────────────────────────────────

		private void OnFormLoad(object sender, EventArgs e)
		{
			StartDownload();
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if(m_worker != null && m_worker.IsBusy)
			{
				m_worker.CancelAsync();
				e.Cancel = true; // esperar a que el worker termine antes de cerrar
			}
			else base.OnFormClosing(e);
		}

		// ── descarga ─────────────────────────────────────────────────────────────

		private void StartDownload()
		{
			m_worker = new BackgroundWorker();
			m_worker.WorkerSupportsCancellation = true;
			m_worker.WorkerReportsProgress = true;
			m_worker.DoWork += OnDoWork;
			m_worker.ProgressChanged += OnProgressChanged;
			m_worker.RunWorkerCompleted += OnWorkerCompleted;
			m_worker.RunWorkerAsync();
		}

		private void OnDoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker bw = (BackgroundWorker)sender;

			GAction<int, int> onProgress = delegate(int current, int total)
			{
				int pct = (total > 0) ? ((current * 100) / total) : 100;
				bw.ReportProgress(Math.Min(pct, 100));
			};

			GFunc<bool> onCancel = delegate() { return bw.CancellationPending; };

			List<FaviconResult> results = FaviconService.DownloadForEntries(
				m_entries, m_db, onProgress, onCancel);

			e.Result = results;
		}

		private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			m_pb.Value = Math.Min(e.ProgressPercentage, 100);
		}

		private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			m_pb.Value = 100;
			m_btnClose.Text = "Cerrar";
			m_btnClose.Enabled = true;

			if(e.Error != null)
			{
				m_lblStatus.Text = "Error inesperado: " + e.Error.Message;
				return;
			}

			m_results = (e.Result as List<FaviconResult>) ?? new List<FaviconResult>();

			// Rellenar la lista de resultados
			foreach(FaviconResult r in m_results)
			{
				string url = r.Entry.Strings.ReadSafe(PwDefs.UrlField);
				ListViewItem lvi = new ListViewItem(url ?? string.Empty);
				lvi.SubItems.Add(r.StatusMessage);
				lvi.ForeColor = r.Success ? Color.DarkGreen : Color.DarkRed;
				m_lv.Items.Add(lvi);
			}

			int ok = 0;
			foreach(FaviconResult r in m_results) if(r.Success) ok++;
			m_lblStatus.Text = string.Format(
				"Completado: {0} de {1} iconos descargados.", ok, m_results.Count);

			if(e.Cancelled)
				m_lblStatus.Text = "Cancelado. " + m_lblStatus.Text;
		}

		// ── botón ────────────────────────────────────────────────────────────────

		private void OnBtnClose(object sender, EventArgs e)
		{
			if(m_worker != null && m_worker.IsBusy)
			{
				m_worker.CancelAsync();
				m_btnClose.Enabled = false;
			}
			else this.Close();
		}
	}
}
