/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

// F15-B — Dark Menu Renderer.
// ToolStripProfessionalRenderer con paleta oscura estilo VS Code Dark+.
// Aplica a: MenuStrip, ToolStrip, ContextMenuStrip.
//
// Uso:
//   DarkMenuRenderer.ApplyToForm(this);      // tematiza todos los ToolStrips de un Form
//   DarkMenuRenderer.ApplyToStrip(ctxMenu);  // tematiza un único ToolStrip

using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeePass.UI
{
	/// <summary>
	/// F15-B: Renderer oscuro para ToolStrip / MenuStrip / ContextMenuStrip.
	/// Paleta VS Code Dark+ — sin dependencias externas.
	/// </summary>
	public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
	{
		// ── Paleta ───────────────────────────────────────────────────────────────

		internal static readonly Color ClrBackDark     = Color.FromArgb(0x1E, 0x1E, 0x1E);
		internal static readonly Color ClrBackMenu     = Color.FromArgb(0x25, 0x25, 0x26);
		internal static readonly Color ClrBackToolBar  = Color.FromArgb(0x2D, 0x2D, 0x30);
		internal static readonly Color ClrBackHover    = Color.FromArgb(0x3F, 0x3F, 0x46);
		internal static readonly Color ClrBackPressed  = Color.FromArgb(0x00, 0x7A, 0xCC);
		internal static readonly Color ClrBorder       = Color.FromArgb(0x3F, 0x3F, 0x46);
		internal static readonly Color ClrText         = Color.FromArgb(0xD4, 0xD4, 0xD4);
		internal static readonly Color ClrTextDisabled = Color.FromArgb(0x66, 0x66, 0x66);
		internal static readonly Color ClrSeparator    = Color.FromArgb(0x3F, 0x3F, 0x46);

		// ── Constructor ──────────────────────────────────────────────────────────

		public DarkMenuRenderer() : base(new DarkColorTable())
		{
			this.RoundedEdges = false;
		}

		// ── Aplicación estática ──────────────────────────────────────────────────

		/// <summary>
		/// Aplica el renderer oscuro a todos los ToolStrips (incluidos anidados)
		/// de un formulario. Llama en el evento Load del Form.
		/// </summary>
		public static void ApplyToForm(Form form)
		{
			if(form == null) return;
			ApplyToControl(form);
		}

		/// <summary>
		/// Aplica el renderer oscuro a un único ToolStrip / MenuStrip / ContextMenuStrip.
		/// </summary>
		public static void ApplyToStrip(ToolStrip ts)
		{
			if(ts == null) return;
			ts.Renderer = new DarkMenuRenderer();
		}

		private static void ApplyToControl(Control c)
		{
			ToolStrip ts = c as ToolStrip;
			if(ts != null) ts.Renderer = new DarkMenuRenderer();

			foreach(Control child in c.Controls)
				ApplyToControl(child);
		}

		// ── OnRender overrides ───────────────────────────────────────────────────

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			bool isDropDown = (e.ToolStrip is ToolStripDropDown);
			bool isMenu     = (e.ToolStrip is MenuStrip);
			Color fill = isDropDown ? ClrBackMenu :
			             isMenu     ? ClrBackToolBar :
			                         ClrBackToolBar;

			using(SolidBrush b = new SolidBrush(fill))
				e.Graphics.FillRectangle(b, e.AffectedBounds);
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			if(e.ToolStrip is ToolStripDropDown)
			{
				using(Pen p = new Pen(ClrBorder))
					e.Graphics.DrawRectangle(p, 0, 0,
						e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
			}
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			Color fill;
			if(!e.Item.Enabled)
				fill = ClrBackMenu;
			else if(e.Item.Pressed)
				fill = ClrBackPressed;
			else if(e.Item.Selected)
				fill = ClrBackHover;
			else
				fill = ClrBackMenu;

			using(SolidBrush b = new SolidBrush(fill))
				e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
		}

		protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
		{
			ToolStripButton btn = e.Item as ToolStripButton;
			bool pressed = (btn != null) && (btn.Pressed || btn.Checked);
			bool hovered = e.Item.Selected;

			if(pressed)
			{
				using(SolidBrush b = new SolidBrush(ClrBackPressed))
					e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
			}
			else if(hovered)
			{
				using(SolidBrush b = new SolidBrush(ClrBackHover))
					e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
			}
			// Otherwise draw nothing — let toolbar background show through
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			if(e.Item != null)
				e.TextColor = e.Item.Enabled ? ClrText : ClrTextDisabled;
			base.OnRenderItemText(e);
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			using(Pen p = new Pen(ClrSeparator))
			{
				if(e.Vertical)
					e.Graphics.DrawLine(p, e.Item.Width / 2, 2,
						e.Item.Width / 2, e.Item.Height - 2);
				else
					e.Graphics.DrawLine(p, 4, e.Item.Height / 2,
						e.Item.Width - 4, e.Item.Height / 2);
			}
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
		{
			using(SolidBrush b = new SolidBrush(ClrBackDark))
				e.Graphics.FillRectangle(b, e.AffectedBounds);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			e.ArrowColor = e.Item.Enabled ? ClrText : ClrTextDisabled;
			base.OnRenderArrow(e);
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			// Draw a highlighted background for checked items
			ToolStripMenuItem tsmi = e.Item as ToolStripMenuItem;
			if(tsmi != null && tsmi.Checked)
			{
				Rectangle r = e.ImageRectangle;
				r.Inflate(2, 2);
				using(SolidBrush b = new SolidBrush(ClrBackPressed))
					e.Graphics.FillRectangle(b, r);
				using(Pen p = new Pen(ClrBorder))
					e.Graphics.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
			}
			base.OnRenderItemCheck(e);
		}

		// ── Inner colour table ────────────────────────────────────────────────────

		private sealed class DarkColorTable : ProfessionalColorTable
		{
			public override Color ToolStripDropDownBackground
			{ get { return Color.FromArgb(0x25, 0x25, 0x26); } }

			public override Color ImageMarginGradientBegin
			{ get { return Color.FromArgb(0x1E, 0x1E, 0x1E); } }
			public override Color ImageMarginGradientMiddle
			{ get { return Color.FromArgb(0x1E, 0x1E, 0x1E); } }
			public override Color ImageMarginGradientEnd
			{ get { return Color.FromArgb(0x1E, 0x1E, 0x1E); } }

			public override Color MenuBorder
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color MenuItemBorder
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color MenuItemSelected
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color MenuItemSelectedGradientBegin
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color MenuItemSelectedGradientEnd
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color MenuItemPressedGradientBegin
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color MenuItemPressedGradientEnd
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }

			public override Color MenuStripGradientBegin
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color MenuStripGradientEnd
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }

			public override Color ToolStripGradientBegin
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color ToolStripGradientMiddle
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color ToolStripGradientEnd
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color ToolStripBorder
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color ToolStripContentPanelGradientBegin
			{ get { return Color.FromArgb(0x25, 0x25, 0x26); } }
			public override Color ToolStripContentPanelGradientEnd
			{ get { return Color.FromArgb(0x25, 0x25, 0x26); } }

			public override Color ButtonSelectedHighlight
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color ButtonSelectedHighlightBorder
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color ButtonPressedHighlight
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color ButtonPressedHighlightBorder
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color ButtonPressedBorder
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color ButtonSelectedBorder
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }

			public override Color CheckBackground
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color CheckSelectedBackground
			{ get { return Color.FromArgb(0x00, 0x7A, 0xCC); } }
			public override Color CheckPressedBackground
			{ get { return Color.FromArgb(0x00, 0x5A, 0x9E); } }

			public override Color SeparatorDark
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color SeparatorLight
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }

			public override Color GripDark
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }
			public override Color GripLight
			{ get { return Color.FromArgb(0x3F, 0x3F, 0x46); } }

			public override Color OverflowButtonGradientBegin
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color OverflowButtonGradientMiddle
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
			public override Color OverflowButtonGradientEnd
			{ get { return Color.FromArgb(0x2D, 0x2D, 0x30); } }
		}
	}
}
