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

// F15-D — Rich Tooltip Provider.
// Muestra tooltips enriquecidos al pasar el ratón sobre entradas en la lista.

using System;
using System.Text;
using System.Windows.Forms;

using KeePass.Services;

using KeePassLib;
using KeePassLib.Cryptography;

namespace KeePass.UI
{
	/// <summary>
	/// F15-D: Adjunta tooltips enriquecidos al CustomListViewEx de entradas.
	/// Muestra: título, usuario, URL, calidad de contraseña, TOTP y expiración.
	/// Uso: RichTooltipProvider.Attach(m_lvEntries);
	/// </summary>
	internal static class RichTooltipProvider
	{
		private static ListView       m_lv      = null;
		private static ToolTip        m_tooltip  = null;
		private static ListViewItem   m_lastItem = null;

		// ────────────────────────────────────────────────────────────────────────

		public static void Attach(ListView lv)
		{
			if(lv == null) return;

			m_lv = lv;
			m_tooltip = new ToolTip
			{
				AutomaticDelay  = 700,
				AutoPopDelay    = 8000,
				InitialDelay    = 700,
				ReshowDelay     = 400,
				ShowAlways      = true,
				IsBalloon       = false,
			};

			m_lv.MouseMove  += OnMouseMove;
			m_lv.MouseLeave += OnMouseLeave;
		}

		public static void Detach()
		{
			if(m_lv != null)
			{
				m_lv.MouseMove  -= OnMouseMove;
				m_lv.MouseLeave -= OnMouseLeave;
			}

			if(m_tooltip != null) { m_tooltip.Dispose(); m_tooltip = null; }

			m_lv       = null;
			m_lastItem = null;
		}

		// ── Eventos ──────────────────────────────────────────────────────────────

		private static void OnMouseMove(object sender, MouseEventArgs e)
		{
			ListViewHitTestInfo hti  = m_lv.HitTest(e.Location);
			ListViewItem        item = (hti != null ? hti.Item : null);

			if(item == m_lastItem) return;
			m_lastItem = item;

			if(item == null) { m_tooltip.SetToolTip(m_lv, null); return; }

			PwEntry pe = item.Tag as PwEntry;
			if(pe == null) { m_tooltip.SetToolTip(m_lv, null); return; }

			m_tooltip.SetToolTip(m_lv, BuildTooltip(pe));
		}

		private static void OnMouseLeave(object sender, EventArgs e)
		{
			m_lastItem = null;
			if(m_tooltip != null) m_tooltip.SetToolTip(m_lv, null);
		}

		// ── Construcción del tooltip ──────────────────────────────────────────────

		private static string BuildTooltip(PwEntry pe)
		{
			StringBuilder sb = new StringBuilder();

			// Título
			string title = pe.Strings.ReadSafe(PwDefs.TitleField);
			if(title.Length > 0) sb.AppendLine(title);

			// Usuario
			string user = pe.Strings.ReadSafe(PwDefs.UserNameField);
			if(user.Length > 0) sb.AppendLine("Usuario: " + user);

			// URL
			string url = pe.Strings.ReadSafe(PwDefs.UrlField);
			if(url.Length > 0) sb.AppendLine("URL: " + Truncate(url, 70));

			// Calidad de contraseña
			string pwd = pe.Strings.ReadSafe(PwDefs.PasswordField);
			if(pwd.Length > 0)
			{
				uint bits = QualityEstimation.EstimatePasswordBits(pwd.ToCharArray());
				string quality;
				if(bits < 40)      quality = "D\u00E9bil (" + bits + " bits)";
				else if(bits < 80) quality = "Media (" + bits + " bits)";
				else               quality = "Fuerte (" + bits + " bits)";
				sb.AppendLine("Contrase\u00F1a: " + quality);
			}

			// TOTP
			if(TotpService.HasTotp(pe))
				sb.AppendLine("TOTP: activado");

			// Expiración
			if(pe.Expires)
			{
				DateTime expiry  = pe.ExpiryTime.ToLocalTime();
				bool     expired = (pe.ExpiryTime <= DateTime.UtcNow);
				string   tag     = expired ? " \u26A0 EXPIRADA" : string.Empty;
				sb.AppendLine("Expira: " + expiry.ToShortDateString() + tag);
			}

			// Grupo padre
			if(pe.ParentGroup != null)
				sb.Append("Grupo: " + pe.ParentGroup.Name);

			return sb.ToString().TrimEnd();
		}

		private static string Truncate(string s, int maxLen)
		{
			return (s.Length <= maxLen) ? s : s.Substring(0, maxLen - 3) + "\u2026";
		}
	}
}
