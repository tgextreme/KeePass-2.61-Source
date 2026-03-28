/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F7 — QR Code para Móvil — formulario de visualización.
// Muestra el QR de usuario+contraseña de la entrada seleccionada con:
//  · Cierre automático a los 30 segundos (cuenta atrás visible).
//  · Protección anti-captura de pantalla (SetWindowDisplayAffinity, Windows 10+).
//  · Imagen generada en memoria; nunca se escribe en disco.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using KeePass.Infrastructure;

using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Forms
{
	/// <summary>
	/// Ventana modal que muestra el código QR de una entrada KeePass.
	/// Se cierra automáticamente transcurridos <see cref="AutoCloseSeconds"/> segundos.
	/// Construida completamente por código (sin .Designer.cs).
	/// </summary>
	public sealed class QrCodeForm : Form
	{
		// ── constantes ────────────────────────────────────────────────────────────
		/// <summary>Tiempo en segundos hasta el cierre automático.</summary>
		private const int AutoCloseSeconds = 30;

		/// <summary>
		/// Valor para <c>SetWindowDisplayAffinity</c> que excluye la ventana de capturas
		/// de pantalla y grabación (disponible desde Windows 10 build 2004).
		/// </summary>
		private const uint WdaExcludeFromCapture = 0x00000011;

		// ── campos ────────────────────────────────────────────────────────────────
		private PictureBox m_pbQr;
		private Label      m_lblTitle;
		private Label      m_lblCountdown;
		private Button     m_btnClose;
		private Timer      m_timer;
		private int        m_secondsLeft = AutoCloseSeconds;
		private Bitmap     m_qrBitmap;

		// ── P/Invoke ──────────────────────────────────────────────────────────────
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

		// ── punto de entrada estático ─────────────────────────────────────────────

		/// <summary>
		/// Genera el QR de la entrada <paramref name="entry"/> y muestra el diálogo modal.
		/// </summary>
		/// <param name="owner">Ventana padre (puede ser null).</param>
		/// <param name="entry">Entrada cuyas credenciales se representarán en el QR.</param>
		public static void ShowQr(IWin32Window owner, PwEntry entry)
		{
			if(entry == null) throw new ArgumentNullException("entry");

			using(QrCodeForm f = new QrCodeForm(entry))
				f.ShowDialog(owner);
		}

		// ── constructor ───────────────────────────────────────────────────────────

		private QrCodeForm(PwEntry entry)
		{
			string userName = entry.Strings.ReadSafe(PwDefs.UserNameField);
			ProtectedString ps = entry.Strings.Get(PwDefs.PasswordField);
			string password = (ps != null) ? ps.ReadString() : string.Empty;

			// Formato compacto, legible por la mayoría de gestores de contraseñas móviles.
			string qrText = string.Format("user:{0}\npass:{1}", userName, password);

			try
			{
				m_qrBitmap = QrGenerator.GenerateQr(qrText);
			}
			catch(Exception ex)
			{
				MessageBox.Show("No se pudo generar el QR: " + ex.Message,
					"KeePass — QR Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			finally
			{
				// Borrar la contraseña de la cadena en memoria lo antes posible.
				// (La protección total se haría con SecureString/ProtectedString, pero
				//  QRCoder necesita string normal.)
				password = null;
				qrText   = null;
			}

			BuildUi(entry.Strings.ReadSafe(PwDefs.TitleField));
		}

		// ── construcción de la UI ─────────────────────────────────────────────────

		private void BuildUi(string entryTitle)
		{
			// — Ventana principal —
			this.Text            = "QR Code — " + entryTitle;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox     = false;
			this.MinimizeBox     = false;
			this.StartPosition   = FormStartPosition.CenterParent;
			this.BackColor       = Color.White;
			this.Padding         = new Padding(16);

			int qrSize = (m_qrBitmap != null) ? m_qrBitmap.Width : 300;
			this.ClientSize = new Size(qrSize + 32, qrSize + 88);

			// — Título —
			m_lblTitle = new Label
			{
				Text      = entryTitle,
				Font      = new Font(this.Font.FontFamily, 11f, FontStyle.Bold),
				AutoSize  = false,
				TextAlign = ContentAlignment.MiddleCenter,
				Bounds    = new Rectangle(0, 8, this.ClientSize.Width, 26)
			};

			// — Imagen QR —
			m_pbQr = new PictureBox
			{
				Bounds      = new Rectangle(16, 40, qrSize, qrSize),
				SizeMode    = PictureBoxSizeMode.Zoom,
				BackColor   = Color.White,
				BorderStyle = BorderStyle.None
			};
			if(m_qrBitmap != null) m_pbQr.Image = m_qrBitmap;

			// — Etiqueta cuenta atrás —
			m_lblCountdown = new Label
			{
				Text      = CountdownText(),
				AutoSize  = false,
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = Color.Gray,
				Bounds    = new Rectangle(0, qrSize + 44, this.ClientSize.Width, 20)
			};

			// — Botón cerrar manual —
			m_btnClose = new Button
			{
				Text   = "Cerrar",
				Bounds = new Rectangle((this.ClientSize.Width - 90) / 2, qrSize + 64, 90, 26)
			};
			m_btnClose.Click += (s, e) => Close();

			this.Controls.AddRange(new Control[] {
				m_lblTitle, m_pbQr, m_lblCountdown, m_btnClose
			});

			this.Load   += OnLoad;
			this.Closed += OnClosed;
		}

		// ── eventos ───────────────────────────────────────────────────────────────

		private void OnLoad(object sender, EventArgs e)
		{
			// Anti-captura de pantalla (Windows 10 build 2004+; ignorado silenciosamente en versiones anteriores).
			try { SetWindowDisplayAffinity(this.Handle, WdaExcludeFromCapture); }
			catch { /* no crítico */ }

			// Temporizador de cierre automático.
			m_timer = new Timer { Interval = 1000 };
			m_timer.Tick += OnTimerTick;
			m_timer.Start();
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			m_secondsLeft--;
			m_lblCountdown.Text = CountdownText();

			if(m_secondsLeft <= 0)
			{
				m_timer.Stop();
				Close();
			}
		}

		private void OnClosed(object sender, EventArgs e)
		{
			if(m_timer != null) { m_timer.Stop(); m_timer.Dispose(); m_timer = null; }
			if(m_qrBitmap != null) { m_qrBitmap.Dispose(); m_qrBitmap = null; }
		}

		// ── helpers ───────────────────────────────────────────────────────────────

		private string CountdownText()
		{
			return string.Format("Cierre automático en {0} segundo{1}",
				m_secondsLeft, m_secondsLeft == 1 ? string.Empty : "s");
		}
	}
}
