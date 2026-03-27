// F5 — Panel de Vista Previa (Detail Sidebar)
// UserControl que muestra detalles de la entrada seleccionada con botones de copia rápida.
// F12 — Toggle de previsualización Markdown integrado.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Util;
using KeePassLib;
using KeePassLib.Utility;

namespace KeePass.UI
{
	/// <summary>Panel lateral de vista previa de una entrada KeePass.</summary>
	public sealed class EntryPreviewPanel : UserControl
	{
		// Controles de visualización
		private Label m_lblTitleValue   = null;
		private Label m_lblUserValue    = null;
		private LinkLabel m_lnkUrl      = null;
		private Label m_lblExpiryValue  = null;
		private RichTextBox m_rtbNotes  = null;

		// Botones de acción rápida
		private Button m_btnCopyUser    = null;
		private Button m_btnCopyPw      = null;
		private Button m_btnOpenUrl     = null;

		// F12 — Previsualización Markdown
		private MarkdownPreviewControl m_mdPreview  = null;
		private Button                 m_btnMdToggle = null;
		private bool                   m_bMarkdownMode = false;

		// Entry actualmente mostrado
		private PwEntry m_entry = null;
		private PwDatabase m_db = null;

		public EntryPreviewPanel()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();

			// ---- Cabecera ----
			Label lblTitle = new Label();
			lblTitle.Text = "Título";
			lblTitle.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
			lblTitle.Location = new Point(6, 8);
			lblTitle.AutoSize = true;

			m_lblTitleValue = new Label();
			m_lblTitleValue.Location = new Point(6, 24);
			m_lblTitleValue.Size = new Size(this.Width - 12, 18);
			m_lblTitleValue.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
			m_lblTitleValue.Font = new Font(SystemFonts.DefaultFont.FontFamily,
				SystemFonts.DefaultFont.Size + 1f, FontStyle.Bold);
			m_lblTitleValue.AutoEllipsis = true;

			// ---- Usuario ----
			Label lblUser = new Label();
			lblUser.Text = "Usuario";
			lblUser.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
			lblUser.Location = new Point(6, 52);
			lblUser.AutoSize = true;

			m_lblUserValue = new Label();
			m_lblUserValue.Location = new Point(6, 68);
			m_lblUserValue.Size = new Size(this.Width - 12, 18);
			m_lblUserValue.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
			m_lblUserValue.AutoEllipsis = true;

			m_btnCopyUser = new Button();
			m_btnCopyUser.Text = "Copiar usuario";
			m_btnCopyUser.Location = new Point(6, 88);
			m_btnCopyUser.Size = new Size(110, 24);
			m_btnCopyUser.Click += OnCopyUser;

			// ---- Contraseña ----
			m_btnCopyPw = new Button();
			m_btnCopyPw.Text = "Copiar contraseña";
			m_btnCopyPw.Location = new Point(6, 118);
			m_btnCopyPw.Size = new Size(130, 24);
			m_btnCopyPw.Click += OnCopyPassword;

			// ---- URL ----
			Label lblUrl = new Label();
			lblUrl.Text = "URL";
			lblUrl.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
			lblUrl.Location = new Point(6, 152);
			lblUrl.AutoSize = true;

			m_lnkUrl = new LinkLabel();
			m_lnkUrl.Location = new Point(6, 168);
			m_lnkUrl.Size = new Size(this.Width - 12, 18);
			m_lnkUrl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
			m_lnkUrl.AutoEllipsis = true;
			m_lnkUrl.LinkClicked += OnUrlClicked;

			m_btnOpenUrl = new Button();
			m_btnOpenUrl.Text = "Abrir URL";
			m_btnOpenUrl.Location = new Point(6, 190);
			m_btnOpenUrl.Size = new Size(85, 24);
			m_btnOpenUrl.Click += OnOpenUrl;

			// ---- Expiración ----
			Label lblExpiry = new Label();
			lblExpiry.Text = "Expira";
			lblExpiry.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
			lblExpiry.Location = new Point(6, 224);
			lblExpiry.AutoSize = true;

			m_lblExpiryValue = new Label();
			m_lblExpiryValue.Location = new Point(6, 240);
			m_lblExpiryValue.Size = new Size(this.Width - 12, 18);
			m_lblExpiryValue.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

			// ---- Notas ----
			Label lblNotes = new Label();
			lblNotes.Text = "Notas";
			lblNotes.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
			lblNotes.Location = new Point(6, 268);
			lblNotes.AutoSize = true;

			m_rtbNotes = new RichTextBox();
			m_rtbNotes.Location = new Point(6, 284);
			m_rtbNotes.Anchor = AnchorStyles.Left | AnchorStyles.Top
				| AnchorStyles.Right | AnchorStyles.Bottom;
			m_rtbNotes.Size = new Size(this.Width - 12, 120);
			m_rtbNotes.ReadOnly = true;
			m_rtbNotes.BackColor = SystemColors.Window;
			m_rtbNotes.BorderStyle = BorderStyle.FixedSingle;
			m_rtbNotes.WordWrap = true;
			m_rtbNotes.ScrollBars = RichTextBoxScrollBars.Vertical;

			// F12 — toggle de previsualización Markdown
			m_btnMdToggle = new Button();
			m_btnMdToggle.Text = "Vista MD";
			m_btnMdToggle.Size = new Size(74, 17);
			m_btnMdToggle.Location = new Point(this.Width - 80, 265);
			m_btnMdToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			m_btnMdToggle.FlatStyle = FlatStyle.Flat;
			m_btnMdToggle.Font = new Font(SystemFonts.DefaultFont.FontFamily, 7f);
			m_btnMdToggle.Click += OnToggleMarkdown;

			m_mdPreview = new MarkdownPreviewControl();
			m_mdPreview.Location = new Point(6, 284);
			m_mdPreview.Anchor = AnchorStyles.Left | AnchorStyles.Top
				| AnchorStyles.Right | AnchorStyles.Bottom;
			m_mdPreview.Size = new Size(this.Width - 12, 120);
			m_mdPreview.Visible = false;

			this.Controls.AddRange(new Control[]
			{
				lblTitle, m_lblTitleValue,
				lblUser, m_lblUserValue, m_btnCopyUser,
				m_btnCopyPw,
				lblUrl, m_lnkUrl, m_btnOpenUrl,
				lblExpiry, m_lblExpiryValue,
				lblNotes, m_btnMdToggle, m_rtbNotes, m_mdPreview
			});

			this.Dock = DockStyle.Right;
			this.Width = 220;
			this.MinimumSize = new Size(150, 80);
			this.BackColor = SystemColors.Control;
			this.Padding = new Padding(0);

			this.ResumeLayout(false);

			ClearDisplay();
		}

		/// <summary>Carga y muestra los datos de una entrada.</summary>
		public void LoadEntry(PwEntry pe, PwDatabase db)
		{
			m_entry = pe;
			m_db = db;

			if(pe == null) { ClearDisplay(); return; }

			m_lblTitleValue.Text = pe.Strings.ReadSafe(PwDefs.TitleField);
			m_lblUserValue.Text  = pe.Strings.ReadSafe(PwDefs.UserNameField);

			string strUrl = pe.Strings.ReadSafe(PwDefs.UrlField);
			m_lnkUrl.Text = strUrl;
			m_lnkUrl.Enabled = strUrl.Length > 0;

			if(pe.Expires)
				m_lblExpiryValue.Text = TimeUtil.ToDisplayString(pe.ExpiryTime);
			else
				m_lblExpiryValue.Text = "(nunca)";

			string strNotes = pe.Strings.ReadSafe(PwDefs.NotesField);
			m_rtbNotes.Text = strNotes;
			if(m_bMarkdownMode) m_mdPreview.Render(strNotes);

			bool hasContent = pe.Strings.ReadSafe(PwDefs.PasswordField).Length > 0;
			m_btnCopyPw.Enabled = hasContent;
			m_btnOpenUrl.Enabled = strUrl.Length > 0;
		}

		private void ClearDisplay()
		{
			m_lblTitleValue.Text  = string.Empty;
			m_lblUserValue.Text   = string.Empty;
			m_lnkUrl.Text         = string.Empty;
			m_lblExpiryValue.Text = string.Empty;
			m_rtbNotes.Text       = string.Empty;
			if(m_mdPreview != null) m_mdPreview.Clear();

			m_btnCopyUser.Enabled = false;
			m_btnCopyPw.Enabled   = false;
			m_btnOpenUrl.Enabled  = false;
			m_lnkUrl.Enabled      = false;
		}

		private void OnCopyUser(object sender, EventArgs e)
		{
			if(m_entry == null) return;
			ClipboardUtil.Copy(m_entry.Strings.GetSafe(PwDefs.UserNameField).ReadString(),
				true, true, m_entry, m_db, IntPtr.Zero);
		}

		private void OnCopyPassword(object sender, EventArgs e)
		{
			if(m_entry == null) return;
			ClipboardUtil.Copy(m_entry.Strings.GetSafe(PwDefs.PasswordField).ReadString(),
				true, true, m_entry, m_db, IntPtr.Zero);
		}

		private void OnUrlClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			OnOpenUrl(sender, e);
		}

		private void OnOpenUrl(object sender, EventArgs e)
		{
			if(m_entry == null) return;
			string strUrl = m_entry.Strings.ReadSafe(PwDefs.UrlField);
			if(strUrl.Length == 0) return;
			WinUtil.OpenUrl(strUrl, m_entry);
		}

		// F12 — Toggle de previsualización Markdown
		private void OnToggleMarkdown(object sender, EventArgs e)
		{
			m_bMarkdownMode         = !m_bMarkdownMode;
			m_btnMdToggle.Text      = m_bMarkdownMode ? "Ver texto" : "Vista MD";
			m_rtbNotes.Visible      = !m_bMarkdownMode;
			m_mdPreview.Visible     = m_bMarkdownMode;

			if(m_bMarkdownMode && m_entry != null)
				m_mdPreview.Render(m_entry.Strings.ReadSafe(PwDefs.NotesField));
		}
	}
}
