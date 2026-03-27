// QuickActionsBar.cs
// F14 — Menús más Accesibles y Reorganizados
// Barra de acciones rápidas configurable debajo de la toolbar principal.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePass.App.Configuration;

namespace KeePass.UI.Accessibility
{
	// Par (texto, tooltip) — reemplaza Tuple<,> para compatibilidad con .NET 3.5
	internal struct BtnDef
	{
		internal readonly string Text;
		internal readonly string Tip;
		internal BtnDef(string text, string tip) { Text = text; Tip = tip; }
	}
	/// <summary>
	/// Agrupa las acciones disponibles en la QuickActionsBar.
	/// Cada Action puede ser null si la operación no está disponible.
	/// </summary>
	public sealed class QuickActionsBarActions
	{
		public Action NewEntry    { get; set; }
		public Action Search      { get; set; }
		public Action CopyPW      { get; set; }
		public Action CopyUser    { get; set; }
		public Action OpenURL     { get; set; }
		public Action Favorite    { get; set; }
		public Action Lock        { get; set; }
		public Action HIBP        { get; set; }
	}

	/// <summary>
	/// Barra de acciones rápidas: FlowLayoutPanel con botones configurables.
	/// Se sitúa entre la toolbar principal y el splitter de contenido.
	/// </summary>
	public sealed class QuickActionsBar : UserControl
	{
		private readonly FlowLayoutPanel m_panel;
		private readonly ToolTip m_toolTip;
		private AceQuickActionsBar m_config;
		private QuickActionsBarActions m_actions;

		private static readonly Dictionary<string, BtnDef> ButtonDefs =
			new Dictionary<string, BtnDef>(StringComparer.OrdinalIgnoreCase)
		{
			// id → (texto, tooltip)
			{ "NewEntry",  new BtnDef("+ Nueva",                 "Crear nueva entrada (Ctrl+I)") },
			{ "Search",    new BtnDef("\U0001F50D Buscar",        "Buscar en la base de datos (Ctrl+F)") },
			{ "CopyPW",    new BtnDef("\U0001F4CB PW",            "Copiar contrase\u00f1a (Ctrl+C)") },
			{ "CopyUser",  new BtnDef("\U0001F464 User",          "Copiar usuario (Ctrl+B)") },
			{ "OpenURL",   new BtnDef("\U0001F310 URL",           "Abrir URL en el navegador (Ctrl+Enter)") },
			{ "Favorite",  new BtnDef("\u2605 Fav",               "Marcar/desmarcar favorito (Ctrl+D)") },
			{ "Lock",      new BtnDef("\U0001F512 Bloquear",      "Bloquear base de datos") },
			{ "HIBP",      new BtnDef("\U0001F6E1\uFE0F HIBP",   "Comprobar filtraciones (HaveIBeenPwned)") },
		};

		public QuickActionsBar()
		{
			m_toolTip = new ToolTip { ShowAlways = true };

			m_panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				AutoScroll = false,
				AutoSize = false,
				BackColor = SystemColors.Control,
				Padding = new Padding(2, 2, 2, 2)
			};

			this.Controls.Add(m_panel);
			this.Height = 28;
			this.Dock = DockStyle.Top;
			this.BackColor = SystemColors.Control;
			this.BorderStyle = BorderStyle.None;
		}

		/// <summary>
		/// Inicializa la barra con la config y las acciones proporcionadas por MainForm.
		/// </summary>
		public void Initialize(AceQuickActionsBar config, QuickActionsBarActions actions)
		{
			m_config  = config ?? new AceQuickActionsBar();
			m_actions = actions ?? new QuickActionsBarActions();

			this.Visible = m_config.Visible;
			Rebuild();
		}

		private void Rebuild()
		{
			m_panel.SuspendLayout();
			m_panel.Controls.Clear();

			foreach(string id in m_config.ButtonIds)
			{
				BtnDef def;
				if(!ButtonDefs.TryGetValue(id, out def)) continue;

				Action action = GetAction(id);

				Button btn = new Button
				{
					Text      = def.Text,
					AutoSize  = true,
					Height    = 22,
					Margin    = new Padding(1, 1, 1, 1),
					UseVisualStyleBackColor = true,
					FlatStyle = FlatStyle.System
				};
				btn.Tag = id;
				m_toolTip.SetToolTip(btn, def.Tip);

				if(action != null)
				{
					Action capturedAction = action;
					btn.Click += (s, e) => capturedAction();
				}
				else
				{
					btn.Enabled = false;
				}

				m_panel.Controls.Add(btn);
			}

			// Botón "Personalizar" siempre al final
			Button btnCustomize = new Button
			{
				Text      = "\u2699",
				AutoSize  = true,
				Height    = 22,
				Margin    = new Padding(4, 1, 1, 1),
				UseVisualStyleBackColor = true,
				FlatStyle = FlatStyle.System
			};
			m_toolTip.SetToolTip(btnCustomize, "Personalizar barra de acciones");
			btnCustomize.Click += OnCustomize;
			m_panel.Controls.Add(btnCustomize);

			m_panel.ResumeLayout(true);
		}

		private Action GetAction(string id)
		{
			if(m_actions == null) return null;
			switch(id)
			{
				case "NewEntry":  return m_actions.NewEntry;
				case "Search":    return m_actions.Search;
				case "CopyPW":    return m_actions.CopyPW;
				case "CopyUser":  return m_actions.CopyUser;
				case "OpenURL":   return m_actions.OpenURL;
				case "Favorite":  return m_actions.Favorite;
				case "Lock":      return m_actions.Lock;
				case "HIBP":      return m_actions.HIBP;
				default:          return null;
			}
		}

		private void OnCustomize(object sender, EventArgs e)
		{
			// Por ahora mostrar un mensaje; en el futuro abrir QuickActionsConfigForm
			MessageBox.Show(
				"Arrastre y suelte los botones para reordenarlos.\n(Funcionalidad de personalización próximamente)",
				"Personalizar barra de acciones",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
	}
}
