// F15-A — Gestor de Temas (Light / Dark / Custom)
// Aplica colores a un Form y notifica cambios via ThemeChanged.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeePass.UI
{
	/// <summary>Temas de interfaz disponibles.</summary>
	public enum KeePassTheme
	{
		Light  = 0,
		Dark   = 1,
		Custom = 2,
	}

	/// <summary>
	/// Gestor central de temas.  Aplica colores a formularios y controles;
	/// emite ThemeChanged cuando el tema cambia.
	/// </summary>
	public static class ThemeManager
	{
		// ── Colores ───────────────────────────────────────────────────────────────

		private static readonly Color DarkBack    = Color.FromArgb( 30,  30,  30);
		private static readonly Color DarkFore    = Color.FromArgb(220, 220, 220);
		private static readonly Color DarkControl = Color.FromArgb( 45,  45,  48);
		private static readonly Color DarkBorder  = Color.FromArgb( 63,  63,  70);

		private static readonly Color LightBack    = SystemColors.Window;
		private static readonly Color LightFore    = SystemColors.WindowText;
		private static readonly Color LightControl = SystemColors.Control;

		// ── Estado ────────────────────────────────────────────────────────────────

		private static KeePassTheme s_theme = KeePassTheme.Light;

		/// <summary>Tema actualmente activo.</summary>
		public static KeePassTheme CurrentTheme
		{
			get { return s_theme; }
		}

		/// <summary>Se emite cuando el tema cambia.  El argumento es el nuevo tema.</summary>
		public static event EventHandler<KeePassTheme> ThemeChanged;

		// ── API pública ───────────────────────────────────────────────────────────

		/// <summary>Establece el tema y aplica los cambios al formulario indicado.</summary>
		public static void SetTheme(KeePassTheme theme, Form form = null)
		{
			if(s_theme == theme && form == null) return;
			s_theme = theme;

			if(form != null) ApplyTheme(form);

			EventHandler<KeePassTheme> h = ThemeChanged;
			if(h != null) h(null, s_theme);
		}

		/// <summary>Aplica el tema activo al formulario y todos sus controles hijos.</summary>
		public static void ApplyTheme(Form form)
		{
			if(form == null) return;

			Color back    = GetBackColor();
			Color fore    = GetForeColor();
			Color control = GetControlColor();

			form.BackColor = back;
			form.ForeColor = fore;

			ApplyToControls(form.Controls, back, fore, control);
		}

		/// <summary>Color de fondo según el tema activo.</summary>
		public static Color GetBackColor()
		{
			return (s_theme == KeePassTheme.Dark) ? DarkBack : LightBack;
		}

		/// <summary>Color de texto según el tema activo.</summary>
		public static Color GetForeColor()
		{
			return (s_theme == KeePassTheme.Dark) ? DarkFore : LightFore;
		}

		/// <summary>Color de controles según el tema activo.</summary>
		public static Color GetControlColor()
		{
			return (s_theme == KeePassTheme.Dark) ? DarkControl : LightControl;
		}

		/// <summary>Color de borde según el tema activo.</summary>
		public static Color GetBorderColor()
		{
			return (s_theme == KeePassTheme.Dark) ? DarkBorder : SystemColors.ControlDark;
		}

		/// <summary>True si el tema activo es oscuro.</summary>
		public static bool IsDark
		{
			get { return s_theme == KeePassTheme.Dark; }
		}

		// ── Helpers privados ──────────────────────────────────────────────────────

		private static void ApplyToControls(Control.ControlCollection controls,
			Color back, Color fore, Color control)
		{
			if(controls == null) return;
			foreach(Control c in controls)
			{
				if(c is Button || c is CheckBox || c is RadioButton)
				{
					c.BackColor = control;
					c.ForeColor = fore;
				}
				else if(c is TextBox || c is RichTextBox || c is ComboBox || c is ListBox)
				{
					c.BackColor = back;
					c.ForeColor = fore;
				}
				else if(c is Panel || c is GroupBox || c is TabControl || c is TabPage)
				{
					c.BackColor = control;
					c.ForeColor = fore;
				}
				else if(c is Label)
				{
					c.BackColor = Color.Transparent;
					c.ForeColor = fore;
				}
				else
				{
					c.BackColor = back;
					c.ForeColor = fore;
				}

				if(c.Controls.Count > 0)
					ApplyToControls(c.Controls, back, fore, control);
			}
		}
	}
}
