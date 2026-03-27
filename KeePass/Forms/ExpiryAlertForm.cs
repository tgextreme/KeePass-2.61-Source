// F4 — Alertas de Expiración
// Muestra un resumen de entradas expiradas o próximas a expirar.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Services;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;

namespace KeePass.Forms
{
	/// <summary>Formulario de alerta de expiración de entradas.</summary>
	public sealed class ExpiryAlertForm : Form
	{
		private ListView m_lvEntries = null;
		private Button m_btnOpen = null;
		private Button m_btnClose = null;
		private Label m_lblInfo = null;

		private readonly List<ExpiryItem> m_items;
		private readonly Action<PwEntry> m_openEntryCallback;

		private ExpiryAlertForm(List<ExpiryItem> items, Action<PwEntry> openEntryCallback)
		{
			m_items = items;
			m_openEntryCallback = openEntryCallback;
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();

			// ListView
			m_lvEntries = new ListView();
			m_lvEntries.Columns.Add("Título", 200);
			m_lvEntries.Columns.Add("Usuario", 130);
			m_lvEntries.Columns.Add("URL", 170);
			m_lvEntries.Columns.Add("Fecha Expiración", 120);
			m_lvEntries.Columns.Add("Días", 60);
			m_lvEntries.FullRowSelect = true;
			m_lvEntries.GridLines = true;
			m_lvEntries.MultiSelect = false;
			m_lvEntries.View = View.Details;
			m_lvEntries.Location = new Point(8, 40);
			m_lvEntries.Size = new Size(716, 280);
			m_lvEntries.SelectedIndexChanged += OnSelectionChanged;

			// Información
			m_lblInfo = new Label();
			m_lblInfo.Location = new Point(8, 10);
			m_lblInfo.Size = new Size(716, 24);
			m_lblInfo.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);

			// Botón Abrir
			m_btnOpen = new Button();
			m_btnOpen.Text = "&Abrir entrada";
			m_btnOpen.Size = new Size(110, 26);
			m_btnOpen.Location = new Point(8, 330);
			m_btnOpen.Enabled = false;
			m_btnOpen.Click += OnOpenEntry;

			// Botón Cerrar
			m_btnClose = new Button();
			m_btnClose.Text = "&Cerrar";
			m_btnClose.Size = new Size(80, 26);
			m_btnClose.Location = new Point(644, 330);
			m_btnClose.Click += delegate(object s, EventArgs e) { this.Close(); };
			this.CancelButton = m_btnClose;

			this.Controls.Add(m_lblInfo);
			this.Controls.Add(m_lvEntries);
			this.Controls.Add(m_btnOpen);
			this.Controls.Add(m_btnClose);

			this.Text = "KeePass — Alertas de Expiración";
			this.ClientSize = new Size(732, 366);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = FormStartPosition.CenterParent;

			this.ResumeLayout(false);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			int expired = 0;
			int expiring = 0;

			foreach(ExpiryItem item in m_items)
			{
				PwEntry pe = item.Entry;

				string strTitle = pe.Strings.ReadSafe(KeePassLib.PwDefs.TitleField);
				string strUser  = pe.Strings.ReadSafe(KeePassLib.PwDefs.UserNameField);
				string strUrl   = pe.Strings.ReadSafe(KeePassLib.PwDefs.UrlField);
				string strDate  = TimeUtil.ToDisplayString(pe.ExpiryTime);

				string strDays;
				if(item.DaysRemaining < 0)
				{
					strDays = item.DaysRemaining.ToString();
					expired++;
				}
				else
				{
					strDays = "+" + item.DaysRemaining.ToString();
					expiring++;
				}

				ListViewItem lvi = new ListViewItem(strTitle);
				lvi.SubItems.Add(strUser);
				lvi.SubItems.Add(strUrl);
				lvi.SubItems.Add(strDate);
				lvi.SubItems.Add(strDays);
				lvi.Tag = pe;

				if(item.DaysRemaining < 0)
					lvi.ForeColor = Color.Red;
				else if(item.DaysRemaining <= 7)
					lvi.ForeColor = Color.OrangeRed;
				else
					lvi.ForeColor = Color.DarkGoldenrod;

				m_lvEntries.Items.Add(lvi);
			}

			// Construir mensaje informativo
			string strMsg = string.Empty;
			if(expired > 0 && expiring > 0)
				strMsg = expired + " entrada(s) expirada(s), " + expiring + " próxima(s) a expirar.";
			else if(expired > 0)
				strMsg = expired + " entrada(s) expirada(s).";
			else
				strMsg = expiring + " entrada(s) próxima(s) a expirar.";

			m_lblInfo.Text = strMsg;
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			m_btnOpen.Enabled = (m_lvEntries.SelectedItems.Count > 0)
				&& (m_openEntryCallback != null);
		}

		private void OnOpenEntry(object sender, EventArgs e)
		{
			if(m_lvEntries.SelectedItems.Count == 0) return;
			PwEntry pe = m_lvEntries.SelectedItems[0].Tag as PwEntry;
			if(pe == null) return;

			this.Close();

			if(m_openEntryCallback != null)
				m_openEntryCallback(pe);
		}

		/// <summary>Muestra el formulario si hay entradas expiradas o próximas a expirar.
		/// <paramref name="openEntryCallback"/> puede ser null.</summary>
		public static void ShowIfNeeded(MainForm mf, PwDatabase db,
			Action<PwEntry> openEntryCallback)
		{
			if(db == null || !db.IsOpen) return;

			int warningDays = Program.Config.Security.ExpiryWarningDays;

			List<ExpiryItem> items = new List<ExpiryItem>();
			items.AddRange(ExpiryService.GetExpiredEntries(db));
			items.AddRange(ExpiryService.GetExpiringSoon(db, warningDays));

			if(items.Count == 0) return;

			UIUtil.ShowDialogAndDestroy(new ExpiryAlertForm(items, openEntryCallback));
		}
	}
}
