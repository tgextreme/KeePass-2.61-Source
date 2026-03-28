// F9 — ColoredRowRenderer
// Pinta las filas del ListView de la ventana principal con el color de etiqueta
// almacenado en CustomData por ColorLabelService.  El color se mezcla al 40 %
// sobre el color de fondo nativo para obtener un tono pastel suave.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.Services;
using KeePassLib;

namespace KeePass.UI
{
	/// <summary>
	/// Renderer de fila coloreada para el ListView principal.
	/// Uso:
	///   listView.OwnerDraw = true;
	///   listView.DrawItem += ColoredRowRenderer.DrawItem;
	///   listView.DrawSubItem += ColoredRowRenderer.DrawSubItem;
	///   listView.DrawColumnHeader += ColoredRowRenderer.DrawColumnHeader;
	/// </summary>
	public static class ColoredRowRenderer
	{
		private const float ColorAlpha = 0.40f;   // 40 % de mezcla sobre fondo blanco

		/// <summary>
		/// Maneja el evento DrawItem del ListView.
		/// El Tag de cada ListViewItem debe ser la PwEntry correspondiente.
		/// </summary>
		public static void DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			if(e == null) return;

			PwEntry entry = e.Item.Tag as PwEntry;
			if(entry != null)
			{
				Color? labelColor = ColorLabelService.GetColor(entry);
				if(labelColor.HasValue)
				{
					Color blended = BlendWithBackground(labelColor.Value, GetBackgroundFor(e.Item));
					using(SolidBrush brush = new SolidBrush(blended))
						e.Graphics.FillRectangle(brush, e.Bounds);
				}
				else
				{
					e.DrawBackground();
				}
			}
			else
			{
				e.DrawBackground();
			}

			e.DrawFocusRectangle();
		}

		/// <summary>Maneja el evento DrawSubItem del ListView.</summary>
		public static void DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			if(e == null) return;

			PwEntry entry = e.Item != null ? e.Item.Tag as PwEntry : null;
			if(entry != null)
			{
				Color? labelColor = ColorLabelService.GetColor(entry);
				if(labelColor.HasValue)
				{
					Color blended = BlendWithBackground(labelColor.Value, GetBackgroundFor(e.Item));
					using(SolidBrush brush = new SolidBrush(blended))
						e.Graphics.FillRectangle(brush, e.Bounds);
				}
				else
				{
					e.DrawBackground();
				}
			}
			else
			{
				e.DrawBackground();
			}

			e.DrawText();
		}

		/// <summary>Maneja el evento DrawColumnHeader del ListView.</summary>
		public static void DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			if(e == null) return;
			e.DrawDefault = true;
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		/// <summary>Mezcla el color de etiqueta sobre el fondo al 40 % de opacidad.</summary>
		private static Color BlendWithBackground(Color label, Color background)
		{
			int r = (int)(label.R * ColorAlpha + background.R * (1f - ColorAlpha));
			int g = (int)(label.G * ColorAlpha + background.G * (1f - ColorAlpha));
			int b = (int)(label.B * ColorAlpha + background.B * (1f - ColorAlpha));
			return Color.FromArgb(
				Math.Max(0, Math.Min(255, r)),
				Math.Max(0, Math.Min(255, g)),
				Math.Max(0, Math.Min(255, b)));
		}

		private static Color GetBackgroundFor(ListViewItem item)
		{
			if(item != null && item.ListView != null)
				return ThemeManager.IsDark
					? ThemeManager.GetBackColor()
					: Color.White;
			return Color.White;
		}
	}
}
