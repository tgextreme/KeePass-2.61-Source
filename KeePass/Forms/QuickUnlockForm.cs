// F10 — Quick Unlock Form
// Formulario modal que permite desbloquear la base de datos usando
// Windows Hello (si está disponible) o contraseña maestra como fallback.
// Construido completamente por código (sin .Designer.cs).

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Integration.WindowsHello;

namespace KeePass.Forms
{
	/// <summary>
	/// Diálogo de desbloqueo rápido con Windows Hello y fallback a contraseña.
	/// Muestra el botón de Windows Hello si el servicio está disponible; si no,
	/// sólo se muestra el campo de contraseña.
	/// </summary>
	public sealed class QuickUnlockForm : Form
	{
		// ── controles ─────────────────────────────────────────────────────────────
		private Label      m_lblTitle;
		private Label      m_lblSubtitle;
		private Button     m_btnWindowsHello;
		private Label      m_lblOr;
		private Label      m_lblPassword;
		private TextBox    m_txtPassword;
		private Button     m_btnUnlock;
		private Button     m_btnCancel;
		private Label      m_lblError;

		// ── estado ────────────────────────────────────────────────────────────────
		private readonly string m_dbPath;
		private bool            m_helloAvailable;

		/// <summary>True si el desbloqueo fue exitoso.</summary>
		public bool UnlockSucceeded { get; private set; }

		/// <summary>Contraseña introducida (sólo válida si UnlockSucceeded = true y no usó Hello).</summary>
		public string EnteredPassword { get; private set; }

		// ── constructor ───────────────────────────────────────────────────────────

		/// <param name="dbPath">Ruta de la base de datos (para Windows Hello).</param>
		public QuickUnlockForm(string dbPath)
		{
			m_dbPath = dbPath ?? string.Empty;
			BuildUI();
			CheckHelloAvailability();
		}

		// ── construcción UI ───────────────────────────────────────────────────────

		private void BuildUI()
		{
			this.Text            = "Desbloquear KeePass";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.StartPosition   = FormStartPosition.CenterParent;
			this.MaximizeBox     = false;
			this.MinimizeBox     = false;
			this.Size            = new Size(380, 320);
			this.KeyPreview      = true;
			this.AcceptButton    = null; // se asignará más tarde

			// Título
			m_lblTitle = new Label {
				Text      = "Desbloqueo Rápido",
				Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
				AutoSize  = true,
				Location  = new Point(20, 20),
			};

			// Subtítulo
			m_lblSubtitle = new Label {
				Text      = "Use Windows Hello o introduzca su contraseña maestra.",
				Font      = new Font("Segoe UI", 9f),
				AutoSize  = false,
				Size      = new Size(340, 34),
				Location  = new Point(20, 52),
			};

			// Botón Windows Hello
			m_btnWindowsHello = new Button {
				Text      = "🪟  Desbloquear con Windows Hello",
				Font      = new Font("Segoe UI", 10f),
				Size      = new Size(340, 42),
				Location  = new Point(20, 100),
				FlatStyle = FlatStyle.System,
			};
			m_btnWindowsHello.Click += OnWindowsHelloClick;

			// Separador "o"
			m_lblOr = new Label {
				Text      = "── o ──",
				AutoSize  = true,
				Location  = new Point(160, 152),
				ForeColor = SystemColors.GrayText,
			};

			// Etiqueta contraseña
			m_lblPassword = new Label {
				Text      = "Contraseña maestra:",
				AutoSize  = true,
				Location  = new Point(20, 175),
			};

			// TextBox contraseña
			m_txtPassword = new TextBox {
				PasswordChar = '●',
				Size         = new Size(340, 24),
				Location     = new Point(20, 195),
			};
			m_txtPassword.KeyDown += (s, e) => {
				if(e.KeyCode == Keys.Enter) OnUnlockClick(s, e);
			};

			// Error label
			m_lblError = new Label {
				Text      = string.Empty,
				ForeColor = Color.Firebrick,
				AutoSize  = false,
				Size      = new Size(340, 20),
				Location  = new Point(20, 224),
			};

			// Botón Desbloquear
			m_btnUnlock = new Button {
				Text      = "Desbloquear",
				Size      = new Size(120, 30),
				Location  = new Point(145, 248),
				FlatStyle = FlatStyle.System,
			};
			m_btnUnlock.Click += OnUnlockClick;
			this.AcceptButton = m_btnUnlock;

			// Botón Cancelar
			m_btnCancel = new Button {
				Text         = "Cancelar",
				Size         = new Size(90, 30),
				Location     = new Point(272, 248),
				FlatStyle    = FlatStyle.System,
				DialogResult = DialogResult.Cancel,
			};
			this.CancelButton = m_btnCancel;

			this.Controls.AddRange(new Control[] {
				m_lblTitle, m_lblSubtitle,
				m_btnWindowsHello, m_lblOr,
				m_lblPassword, m_txtPassword, m_lblError,
				m_btnUnlock, m_btnCancel,
			});
		}

		private void CheckHelloAvailability()
		{
			try
			{
				m_helloAvailable = WindowsHelloService.Instance.IsAvailable();
			}
			catch
			{
				m_helloAvailable = false;
			}

			m_btnWindowsHello.Enabled = m_helloAvailable;
			m_btnWindowsHello.Visible = true;
			m_lblOr.Visible           = m_helloAvailable;
		}

		// ── eventos ───────────────────────────────────────────────────────────────

		private void OnWindowsHelloClick(object sender, EventArgs e)
		{
			m_lblError.Text           = string.Empty;
			m_btnWindowsHello.Enabled = false;

			try
			{
				HelloKeyData keyData = WindowsHelloService.Instance.RetrieveKey(this.Handle, m_dbPath);
				if(keyData != null)
				{
					UnlockSucceeded   = true;
					this.DialogResult = DialogResult.OK;
					this.Close();
				}
				else
				{
					m_lblError.Text = "Windows Hello no pudo verificar la identidad.";
				}
			}
			catch(Exception ex)
			{
				m_lblError.Text = "Error de Windows Hello: " + ex.Message;
			}
			finally
			{
				m_btnWindowsHello.Enabled = m_helloAvailable;
			}
		}

		private void OnUnlockClick(object sender, EventArgs e)
		{
			string pwd = m_txtPassword.Text;
			if(string.IsNullOrEmpty(pwd))
			{
				m_lblError.Text = "Introduzca la contraseña maestra.";
				return;
			}

			EnteredPassword   = pwd;
			UnlockSucceeded   = true;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// ── limpieza ──────────────────────────────────────────────────────────────

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				m_lblTitle?.Dispose();
				m_lblSubtitle?.Dispose();
				m_btnWindowsHello?.Dispose();
				m_lblOr?.Dispose();
				m_lblPassword?.Dispose();
				m_txtPassword?.Dispose();
				m_btnUnlock?.Dispose();
				m_btnCancel?.Dispose();
				m_lblError?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
