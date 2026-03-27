using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Util;
using KeePassLib;
using KeePassLib.Collections;

namespace KeePass.UI.Background
{
	/// <summary>
	/// Floating borderless search popup accessible via the Ctrl+Alt+K global hotkey.
	/// Appears at the bottom-right of the screen (above the system tray).
	/// </summary>
	public sealed class MiniSearchPopup : Form
	{
		// ── Static toggle ──────────────────────────────────────────────────────

		private static MiniSearchPopup g_instance = null;

		public static void Toggle()
		{
			if(g_instance == null || g_instance.IsDisposed)
			{
				g_instance = new MiniSearchPopup();
				g_instance.Show();
				g_instance.Activate();
			}
			else if(g_instance.Visible)
			{
				g_instance.Close();
			}
			else
			{
				g_instance.Show();
				g_instance.Activate();
			}
		}

		public static void CloseIfOpen()
		{
			if(g_instance != null && !g_instance.IsDisposed)
			{
				g_instance.Close();
				g_instance = null;
			}
		}

		// ── UI controls ────────────────────────────────────────────────────────

		private TextBox       m_txtSearch  = null;
		private ListView      m_listView   = null;
		private Button        m_btnOpen    = null;
		private Button        m_btnLock    = null;
		private PwObjectList<PwEntry> m_results = new PwObjectList<PwEntry>();

		// ── Construction ────────────────────────────────────────────────────────

		public MiniSearchPopup()
		{
			BuildUI();
			PositionNearTray();
		}

		private void BuildUI()
		{
			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition   = FormStartPosition.Manual;
			this.Size            = new Size(440, 340);
			this.TopMost         = true;
			this.ShowInTaskbar   = false;
			this.BackColor       = Color.FromArgb(30, 30, 30);
			this.ForeColor       = Color.FromArgb(220, 220, 220);
			this.Font            = new Font("Segoe UI", 10f);

			// Thin border
			this.Padding = new Padding(1);

			// ── Title bar with close button ────────────────────────────
			Panel pnlHeader = new Panel
			{
				Dock      = DockStyle.Top,
				Height    = 32,
				BackColor = Color.FromArgb(45, 45, 48),
				Padding   = new Padding(8, 0, 4, 0)
			};

			Label lblTitle = new Label
			{
				Text      = "\U0001F50D  Buscar credencial",
				AutoSize  = false,
				Dock      = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				ForeColor = Color.FromArgb(200, 200, 200),
				Font      = new Font("Segoe UI", 9f)
			};

			Button btnClose = new Button
			{
				Text      = "\u2715",
				Size      = new Size(28, 28),
				Dock      = DockStyle.Right,
				FlatStyle = FlatStyle.Flat,
				ForeColor = Color.FromArgb(200, 200, 200),
				BackColor = Color.Transparent,
				TabStop   = false
			};
			btnClose.FlatAppearance.BorderSize = 0;
			btnClose.Click += (s, e) => Close();

			pnlHeader.Controls.Add(lblTitle);
			pnlHeader.Controls.Add(btnClose);

			// Drag by header — wire up pnlHeader and lblTitle to same handlers
			bool dragging = false;
			Point dragStart = Point.Empty;
			Point formStart = Point.Empty;

			MouseEventHandler onMouseDown = (s, ev) => {
				if(ev.Button == MouseButtons.Left)
				{ dragging = true; dragStart = Control.MousePosition; formStart = this.Location; }
			};
			MouseEventHandler onMouseMove = (s, ev) => {
				if(dragging)
				{
					Point cur = Control.MousePosition;
					this.Location = new Point(
						formStart.X + cur.X - dragStart.X,
						formStart.Y + cur.Y - dragStart.Y);
				}
			};
			MouseEventHandler onMouseUp = (s, ev) => dragging = false;

			pnlHeader.MouseDown += onMouseDown;
			pnlHeader.MouseMove += onMouseMove;
			pnlHeader.MouseUp   += onMouseUp;
			lblTitle.MouseDown  += onMouseDown;
			lblTitle.MouseMove  += onMouseMove;
			lblTitle.MouseUp    += onMouseUp;

			// ── Search text box ────────────────────────────────────────
			m_txtSearch = new TextBox
			{
				Dock        = DockStyle.Top,
				Height      = 36,
				Font        = new Font("Segoe UI", 11f),
				BackColor   = Color.FromArgb(50, 50, 55),
				ForeColor   = Color.FromArgb(230, 230, 230),
				BorderStyle = BorderStyle.FixedSingle
			};
			m_txtSearch.TextChanged += OnSearchTextChanged;
			m_txtSearch.KeyDown     += OnSearchKeyDown;

			// ── Results list ────────────────────────────────────────────
			m_listView = new ListView
			{
				Dock          = DockStyle.Fill,
				View          = View.Details,
				FullRowSelect = true,
				GridLines     = false,
				HeaderStyle   = ColumnHeaderStyle.None,
				BackColor     = Color.FromArgb(37, 37, 40),
				ForeColor     = Color.FromArgb(220, 220, 220),
				BorderStyle   = BorderStyle.None,
				Font          = new Font("Segoe UI", 9.5f),
				MultiSelect   = false,
				HideSelection = false
			};
			m_listView.Columns.Add("Título",   220);
			m_listView.Columns.Add("Usuario",  140);
			m_listView.Columns.Add("",          44);
			m_listView.Columns.Add("",          28);
			m_listView.DoubleClick += OnResultDoubleClick;
			m_listView.KeyDown     += OnListKeyDown;

			// ── Bottom buttons ──────────────────────────────────────────
			Panel pnlBottom = new Panel
			{
				Dock      = DockStyle.Bottom,
				Height    = 42,
				BackColor = Color.FromArgb(45, 45, 48),
				Padding   = new Padding(6, 4, 6, 4)
			};

			m_btnOpen = new Button
			{
				Text      = "Abrir KeePass",
				Dock      = DockStyle.Left,
				Width     = 130,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(0, 122, 204),
				ForeColor = Color.White,
				Font      = new Font("Segoe UI", 9f)
			};
			m_btnOpen.FlatAppearance.BorderSize = 0;
			m_btnOpen.Click += OnOpenKeePass;

			m_btnLock = new Button
			{
				Text      = "\U0001F512  Bloquear",
				Dock      = DockStyle.Right,
				Width     = 110,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(68, 68, 68),
				ForeColor = Color.White,
				Font      = new Font("Segoe UI", 9f)
			};
			m_btnLock.FlatAppearance.BorderSize = 0;
			m_btnLock.Click += OnLockDatabase;

			pnlBottom.Controls.Add(m_btnOpen);
			pnlBottom.Controls.Add(m_btnLock);

			// ── Layout ────────────────────────────────────────────────
			this.Controls.Add(m_listView);
			this.Controls.Add(m_txtSearch);
			this.Controls.Add(pnlHeader);
			this.Controls.Add(pnlBottom);

			// Close when focus is lost
			this.Deactivate += (s, e) => Close();
		}

		private void PositionNearTray()
		{
			Rectangle wa = Screen.PrimaryScreen.WorkingArea;
			this.Location = new Point(
				wa.Right  - this.Width  - 8,
				wa.Bottom - this.Height - 8);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			m_txtSearch.Focus();
			DoSearch(string.Empty);
		}

		// ── Keyboard handling ──────────────────────────────────────────────────

		private void OnSearchKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Close();
			}
			else if(e.KeyCode == Keys.Down && m_listView.Items.Count > 0)
			{
				e.Handled = true;
				m_listView.Focus();
				m_listView.Items[0].Selected = true;
				m_listView.Items[0].Focused  = true;
			}
			else if(e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				CopySelectedPassword();
			}
		}

		private void OnListKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Close();
			}
			else if(e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				if(e.Control) OpenSelectedUrl();
				else          CopySelectedPassword();
			}
			else if(e.KeyCode == Keys.C && e.Control && !e.Shift && !e.Alt)
			{
				e.Handled = true;
				CopySelectedUserName();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if(keyData == Keys.Escape) { Close(); return true; }
			return base.ProcessCmdKey(ref msg, keyData);
		}

		// ── Search logic ───────────────────────────────────────────────────────

		private void OnSearchTextChanged(object sender, EventArgs e)
		{
			DoSearch(m_txtSearch.Text);
		}

		private void DoSearch(string query)
		{
			m_listView.Items.Clear();
			m_results.Clear();

			var mf = Program.MainForm;
			if(mf == null || mf.IsFileLocked(null)) return;

			PwDatabase db = mf.ActiveDatabase;
			if(db == null || !db.IsOpen) return;

			var sp = new SearchParameters();
			sp.SearchString  = query ?? string.Empty;
			sp.SearchInTitles    = true;
			sp.SearchInUserNames = true;
			sp.SearchInUrls      = true;
			sp.SearchInNotes     = false;
			sp.SearchInPasswords = false;

			db.RootGroup.SearchEntries(sp, m_results);

			int max = Program.Config.BackgroundMode.ShowRecentCount * 3; // up to 15
			int count = Math.Min((int)m_results.UCount, max);

			m_listView.BeginUpdate();
			for(int i = 0; i < count; i++)
			{
				PwEntry pe = m_results.GetAt((uint)i);
				string title = pe.Strings.ReadSafe(KeePassLib.PwDefs.TitleField);
				string user  = pe.Strings.ReadSafe(KeePassLib.PwDefs.UserNameField);
				string url   = pe.Strings.ReadSafe(KeePassLib.PwDefs.UrlField);

				var item = new ListViewItem(title);
				item.SubItems.Add(user);
				item.SubItems.Add("\U0001F4CB");  // copy pw
				item.SubItems.Add(string.IsNullOrEmpty(url) ? "" : "\U0001F310"); // open url
				item.Tag = pe;
				m_listView.Items.Add(item);
			}
			m_listView.EndUpdate();

			UpdateButtonState(mf);
		}

		private void UpdateButtonState(Forms.MainForm mf)
		{
			bool canLock = mf != null && mf.IsAtLeastOneFileOpen() && !mf.IsFileLocked(null);
			m_btnLock.Enabled = canLock;
		}

		// ── List actions ───────────────────────────────────────────────────────

		private void OnResultDoubleClick(object sender, EventArgs e)
		{
			CopySelectedPassword();
		}

		private void CopySelectedPassword()
		{
			PwEntry pe = GetSelectedEntry();
			if(pe == null) return;

			ClipboardUtil.CopyAndMinimize(pe.Strings.Get(KeePassLib.PwDefs.PasswordField),
				true, Program.MainForm, pe, Program.MainForm?.ActiveDatabase);
			Close();
		}

		private void CopySelectedUserName()
		{
			PwEntry pe = GetSelectedEntry();
			if(pe == null) return;

			ClipboardUtil.CopyAndMinimize(pe.Strings.ReadSafe(KeePassLib.PwDefs.UserNameField),
				true, Program.MainForm, pe, Program.MainForm?.ActiveDatabase);
			Close();
		}

		private void OpenSelectedUrl()
		{
			PwEntry pe = GetSelectedEntry();
			if(pe == null) return;

			string url = pe.Strings.ReadSafe(KeePassLib.PwDefs.UrlField);
			if(!string.IsNullOrEmpty(url))
			{
				WinUtil.OpenUrl(url, pe);
				Close();
			}
		}

		private PwEntry GetSelectedEntry()
		{
			if(m_listView.SelectedItems.Count == 0) return null;
			return m_listView.SelectedItems[0].Tag as PwEntry;
		}

		// ── Bottom button handlers ─────────────────────────────────────────────

		private void OnOpenKeePass(object sender, EventArgs e)
		{
			Close();
			var mf = Program.MainForm;
			if(mf != null)
			{
				mf.Visible = true;
				if(mf.WindowState == FormWindowState.Minimized)
					mf.WindowState = FormWindowState.Normal;
				mf.Activate();
			}
		}

		private void OnLockDatabase(object sender, EventArgs e)
		{
			Close();
			var mf = Program.MainForm;
			if(mf != null) mf.LockAllDocuments();
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);
			if(g_instance == this) g_instance = null;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				m_txtSearch?.Dispose();
				m_listView?.Dispose();
				m_btnOpen?.Dispose();
				m_btnLock?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
