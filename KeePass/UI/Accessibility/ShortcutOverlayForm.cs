// ShortcutOverlayForm.cs
// F14 — Menús más Accesibles y Reorganizados
// Overlay semitransparente con todos los atajos de teclado.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.UI;

namespace KeePass.UI.Accessibility
{
	/// <summary>
	/// Formulario overlay semitransparente que muestra todos los atajos de teclado.
	/// Se abre con F1 y se cierra con Escape o cualquier tecla.
	/// Uso: <see cref="Toggle"/> desde la ventana principal.
	/// </summary>
	public sealed class ShortcutOverlayForm : Form
	{
		private static ShortcutOverlayForm g_instance = null;
		private static Form g_parent = null;

		private static readonly KeyValuePair<string, string>[] g_shortcuts = new KeyValuePair<string, string>[]
		{
			new KeyValuePair<string, string>("Ctrl+C",        "Copiar contraseña"),
			new KeyValuePair<string, string>("Ctrl+B",        "Copiar usuario"),
			new KeyValuePair<string, string>("Ctrl+U",        "Copiar URL al portapapeles"),
			new KeyValuePair<string, string>("Ctrl+P",        "Command Palette"),
			new KeyValuePair<string, string>("Ctrl+D",        "Marcar/desmarcar favorito"),
			new KeyValuePair<string, string>("Ctrl+Alt+K",    "Mini búsqueda (desde cualquier app)"),
			new KeyValuePair<string, string>("F1",            "Mostrar/ocultar este overlay"),
			new KeyValuePair<string, string>("F2",            "Editar entrada seleccionada"),
			new KeyValuePair<string, string>("F3 / Ctrl+F",   "Búsqueda global"),
			new KeyValuePair<string, string>("Del",           "Eliminar entrada"),
			new KeyValuePair<string, string>("Alt+Space",     "Quick Access Overlay"),
			new KeyValuePair<string, string>("Escape",        "Cerrar este overlay"),
		};

		private ShortcutOverlayForm()
		{
			this.FormBorderStyle = FormBorderStyle.None;
			this.ShowInTaskbar = false;
			this.TopMost = true;
			this.Opacity = 0.93;
			this.BackColor = Color.FromArgb(30, 30, 35);
			this.KeyPreview = true;
			this.StartPosition = FormStartPosition.Manual;

			BuildUI();
		}

		private void BuildUI()
		{
			int padH = 20, padV = 16;
			int colKey = 180, colDesc = 290;
			int rowHeight = 22;
			int titleHeight = 36;

			int totalW = padH * 2 + colKey + colDesc;
			int totalH = padV * 2 + titleHeight + g_shortcuts.Length * rowHeight + 12;

			this.ClientSize = new Size(totalW, totalH);

			// Título
			Label lblTitle = new Label
			{
				Text = "⌨  Atajos de teclado",
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold),
				Location = new Point(padH, padV),
				AutoSize = true
			};
			this.Controls.Add(lblTitle);

			// Separador
			Panel sep = new Panel
			{
				BackColor = Color.FromArgb(80, 80, 90),
				Location = new Point(padH, padV + titleHeight - 4),
				Size = new Size(totalW - padH * 2, 1)
			};
			this.Controls.Add(sep);

			// Filas de atajos
			for(int i = 0; i < g_shortcuts.Length; i++)
			{
				int y = padV + titleHeight + i * rowHeight;

				Color rowBg = (i % 2 == 0)
					? Color.FromArgb(38, 38, 45)
					: Color.FromArgb(30, 30, 35);

				Panel row = new Panel
				{
					BackColor = rowBg,
					Location = new Point(padH, y),
					Size = new Size(colKey + colDesc + 8, rowHeight)
				};

				Label lblKey = new Label
				{
					Text = g_shortcuts[i].Key,
					ForeColor = Color.FromArgb(180, 220, 255),
					BackColor = Color.Transparent,
					Font = new Font("Consolas", 8.5f),
					Location = new Point(4, 3),
					Size = new Size(colKey - 8, rowHeight - 2),
					TextAlign = ContentAlignment.MiddleLeft
				};

				Label lblDesc = new Label
				{
					Text = g_shortcuts[i].Value,
					ForeColor = Color.FromArgb(210, 210, 220),
					BackColor = Color.Transparent,
					Font = new Font("Segoe UI", 8.5f),
					Location = new Point(colKey, 3),
					Size = new Size(colDesc, rowHeight - 2),
					TextAlign = ContentAlignment.MiddleLeft
				};

				row.Controls.Add(lblKey);
				row.Controls.Add(lblDesc);
				this.Controls.Add(row);
			}

			// Pie
			Label lblFoot = new Label
			{
				Text = "Presiona cualquier tecla para cerrar",
				ForeColor = Color.FromArgb(120, 120, 130),
				BackColor = Color.Transparent,
				Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
				Location = new Point(padH, totalH - padV - 14),
				AutoSize = true
			};
			this.Controls.Add(lblFoot);
		}

		/// <summary>
		/// Alterna la visibilidad del overlay. Usar desde la ventana principal.
		/// </summary>
		public static void Toggle(Form parent)
		{
			if(g_instance != null && !g_instance.IsDisposed && g_instance.Visible)
			{
				g_instance.Close();
				return;
			}

			g_parent = parent;
			g_instance = new ShortcutOverlayForm();
			GlobalWindowManager.AddWindow(g_instance);

			// Centrar sobre la ventana padre
			if(parent != null && !parent.IsDisposed)
			{
				Rectangle r = parent.Bounds;
				int x = r.Left + (r.Width - g_instance.Width) / 2;
				int y = r.Top + (r.Height - g_instance.Height) / 2;
				g_instance.Location = new Point(x, y);
			}

			g_instance.Show(parent);
			g_instance.BringToFront();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			this.Close();
			e.Handled = true;
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			this.Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			GlobalWindowManager.RemoveWindow(this);
			base.OnClosed(e);
			g_instance = null;
		}
	}
}
