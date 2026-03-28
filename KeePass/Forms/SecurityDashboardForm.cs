// F16 — Security Dashboard Form
// Muestra el resumen de seguridad de la base de datos activa:
//   · Puntuación global, entradas expiradas, por expirar, débiles, duplicadas.
//   · Lista de entradas de mayor riesgo.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Services;
using KeePassLib;

namespace KeePass.Forms
{
	/// <summary>
	/// Panel de seguridad global de la base de datos.
	/// Muestra métricas obtenidas de <see cref="IDashboardService"/>.
	/// </summary>
	public sealed class SecurityDashboardForm : Form
	{
		// ── controles principales ─────────────────────────────────────────────────
		private Label      m_lblTitle;
		private Panel      m_pnlScore;
		private Label      m_lblScoreValue;
		private Label      m_lblScoreLabel;
		private TableLayoutPanel m_tblMetrics;
		private ListView   m_lvRisks;
		private Button     m_btnRefresh;
		private Button     m_btnClose;
		private Label      m_lblLoading;

		// ── estado ────────────────────────────────────────────────────────────────
		private readonly PwDatabase        m_db;
		private readonly IDashboardService m_dashboard;

		// ── constructor ───────────────────────────────────────────────────────────

		/// <param name="db">Base de datos activa.</param>
		/// <param name="dashboardService">Servicio de métricas (AppServices.Dashboard).</param>
		public SecurityDashboardForm(PwDatabase db, IDashboardService dashboardService)
		{
			m_db        = db ?? throw new ArgumentNullException("db");
			m_dashboard = dashboardService ?? throw new ArgumentNullException("dashboardService");
			BuildUI();
		}

		// ── construcción UI ───────────────────────────────────────────────────────

		private void BuildUI()
		{
			this.Text            = "Panel de Seguridad";
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.StartPosition   = FormStartPosition.CenterParent;
			this.MinimumSize     = new Size(560, 500);
			this.Size            = new Size(680, 560);
			this.MaximizeBox     = true;

			// --- Título ---
			m_lblTitle = new Label {
				Text     = "Panel de Seguridad de la Base de Datos",
				Font     = new Font("Segoe UI", 14f, FontStyle.Bold),
				AutoSize = false,
				Size     = new Size(640, 32),
				Location = new Point(16, 16),
			};

			// --- Puntuación (círculo) ---
			m_pnlScore = new Panel {
				Size     = new Size(100, 100),
				Location = new Point(16, 60),
			};
			m_pnlScore.Paint += OnScorePanelPaint;

			m_lblScoreValue = new Label {
				Text      = "--",
				Font      = new Font("Segoe UI", 26f, FontStyle.Bold),
				AutoSize  = true,
				ForeColor = Color.White,
			};

			m_lblScoreLabel = new Label {
				Text     = "Puntuación",
				Font     = new Font("Segoe UI", 8f),
				AutoSize = true,
				Location = new Point(16, 170),
			};

			// --- Métricas ---
			m_tblMetrics = new TableLayoutPanel {
				ColumnCount = 2,
				RowCount    = 5,
				Size        = new Size(420, 130),
				Location    = new Point(130, 60),
				CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
				Font        = new Font("Segoe UI", 10f),
			};
			m_tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
			m_tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

			// --- Lista entradas de riesgo ---
			var lblRisks = new Label {
				Text     = "Entradas con mayor riesgo",
				Font     = new Font("Segoe UI", 10f, FontStyle.Bold),
				AutoSize = true,
				Location = new Point(16, 200),
			};

			m_lvRisks = new ListView {
				View          = View.Details,
				FullRowSelect = true,
				GridLines     = true,
				Size          = new Size(632, 200),
				Location      = new Point(16, 224),
				Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
			};
			m_lvRisks.Columns.Add("Título",       220);
			m_lvRisks.Columns.Add("Motivo",        180);
			m_lvRisks.Columns.Add("Expiración",    130);
			m_lvRisks.Columns.Add("Grupo",         100);

			// --- Label cargando ---
			m_lblLoading = new Label {
				Text      = "Calculando métricas…",
				AutoSize  = true,
				ForeColor = SystemColors.GrayText,
				Location  = new Point(16, 440),
				Visible   = false,
			};

			// --- Botones ---
			m_btnRefresh = new Button {
				Text      = "Actualizar",
				Size      = new Size(100, 28),
				Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
				FlatStyle = FlatStyle.System,
			};
			m_btnRefresh.Click += (s, e) => LoadMetrics();

			m_btnClose = new Button {
				Text         = "Cerrar",
				Size         = new Size(80, 28),
				Anchor       = AnchorStyles.Bottom | AnchorStyles.Right,
				FlatStyle    = FlatStyle.System,
				DialogResult = DialogResult.Cancel,
			};
			this.CancelButton = m_btnClose;

			// Anclar botones al fondo
			var pnlButtons = new Panel {
				Dock   = DockStyle.Bottom,
				Height = 44,
			};
			m_btnClose.Location   = new Point(  4, 8);
			m_btnRefresh.Location = new Point(90, 8);
			pnlButtons.Controls.Add(m_btnClose);
			pnlButtons.Controls.Add(m_btnRefresh);

			this.Controls.AddRange(new Control[] {
				m_lblTitle, m_pnlScore, m_lblScoreLabel,
				m_tblMetrics,
				lblRisks, m_lvRisks,
				m_lblLoading,
				pnlButtons,
			});

			this.Shown += (s, e) => LoadMetrics();
		}

		// ── carga de métricas ─────────────────────────────────────────────────────

		private void LoadMetrics()
		{
			m_btnRefresh.Enabled = false;
			m_lblLoading.Visible  = true;

			try
			{
				DashboardMetrics metrics = m_dashboard.GetMetrics(m_db);
				UpdateUI(metrics);
			}
			catch(Exception ex)
			{
				m_lblLoading.Text    = "Error: " + ex.Message;
				m_lblLoading.Visible = true;
			}
			finally
			{
				m_btnRefresh.Enabled = true;
			}
		}

		private void UpdateUI(DashboardMetrics m)
		{
			if(m == null) return;

			// Score
			m_lblScoreValue.Text = m.SecurityScore.ToString();
			m_pnlScore.Invalidate();

			// Métricas en tabla
			m_tblMetrics.Controls.Clear();
			AddMetricRow("Total de entradas",         m.TotalEntries);
			AddMetricRow("Entradas expiradas",         m.ExpiredEntries,        color: m.ExpiredEntries > 0 ? Color.Firebrick : (Color?)null);
			AddMetricRow("Expiran en 14 días",         m.ExpiringIn14Days,      color: m.ExpiringIn14Days > 0 ? Color.DarkOrange : (Color?)null);
			AddMetricRow("Contraseñas débiles",        m.WeakPasswords,         color: m.WeakPasswords > 0 ? Color.DarkOrange : (Color?)null);
			AddMetricRow("Contraseñas duplicadas",     m.DuplicatePasswords,    color: m.DuplicatePasswords > 0 ? Color.DarkOrange : (Color?)null);

			// Top riesgos
			m_lvRisks.BeginUpdate();
			m_lvRisks.Items.Clear();
			foreach(PwEntry pe in m.TopRisks)
			{
				string title   = pe.Strings.ReadSafe(PwDefs.TitleField);
				string motivo  = BuildReason(pe, m);
				string expiry  = pe.Expires ? pe.ExpiryTime.ToLocalTime().ToShortDateString() : "-";
				string group   = pe.ParentGroup != null ? pe.ParentGroup.Name : "-";

				var item = new ListViewItem(new[] { title, motivo, expiry, group });
				item.Tag = pe;
				m_lvRisks.Items.Add(item);
			}
			m_lvRisks.EndUpdate();

			m_lblLoading.Visible = false;
		}

		private void AddMetricRow(string label, int value, Color? color = null)
		{
			var lblLabel = new Label {
				Text     = label,
				AutoSize = true,
				Dock     = DockStyle.Fill,
				Padding  = new Padding(0, 3, 0, 3),
			};
			var lblValue = new Label {
				Text      = value.ToString(),
				AutoSize  = true,
				Dock      = DockStyle.Fill,
				Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleRight,
				Padding   = new Padding(0, 3, 8, 3),
			};
			if(color.HasValue) lblValue.ForeColor = color.Value;

			m_tblMetrics.Controls.Add(lblLabel);
			m_tblMetrics.Controls.Add(lblValue);
		}

		// ── Paint del score ───────────────────────────────────────────────────────

		private int m_paintedScore = 0;

		private void OnScorePanelPaint(object sender, PaintEventArgs e)
		{
			int score = m_paintedScore;
			Color scoreColor = score >= 80 ? Color.ForestGreen
			                 : score >= 50 ? Color.DarkOrange
			                 :               Color.Firebrick;

			Rectangle rect = new Rectangle(5, 5, 90, 90);
			using(SolidBrush bg   = new SolidBrush(scoreColor))
			using(Pen       border = new Pen(Color.FromArgb(40, 0, 0, 0), 2))
			{
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				e.Graphics.FillEllipse(bg, rect);
				e.Graphics.DrawEllipse(border, rect);
			}

			// Centrar texto de puntuación dentro del círculo
			string text = score.ToString();
			using(Font f = new Font("Segoe UI", 22f, FontStyle.Bold))
			using(SolidBrush fg = new SolidBrush(Color.White))
			{
				SizeF sz     = e.Graphics.MeasureString(text, f);
				float tx     = rect.X + (rect.Width  - sz.Width)  / 2f;
				float ty     = rect.Y + (rect.Height - sz.Height) / 2f;
				e.Graphics.DrawString(text, f, fg, tx, ty);
			}

			m_lblScoreValue.Visible = false; // ocultamos el label; el panel pinta el número
		}

		private void UpdateUI_SetScore(int score)
		{
			m_paintedScore = score;
			m_pnlScore.Invalidate();
		}

		// ── helper reason ─────────────────────────────────────────────────────────

		private static string BuildReason(PwEntry pe, DashboardMetrics m)
		{
			var reasons = new System.Text.StringBuilder();
			if(pe.Expires && pe.ExpiryTime <= DateTime.UtcNow)
				reasons.Append("Expirada ");
			if(reasons.Length == 0)
				reasons.Append("Débil / duplicada");
			return reasons.ToString().Trim();
		}

		// ── limpieza ──────────────────────────────────────────────────────────────

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				m_lblTitle?.Dispose();
				m_pnlScore?.Dispose();
				m_lblScoreValue?.Dispose();
				m_lblScoreLabel?.Dispose();
				m_tblMetrics?.Dispose();
				m_lvRisks?.Dispose();
				m_btnRefresh?.Dispose();
				m_btnClose?.Dispose();
				m_lblLoading?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
