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

// F15-C — Columna "Estado" de seguridad para la lista de entradas.
// Muestra expiración, TOTP y calidad de contraseña como indicadores Unicode.

using System;
using System.Text;
using System.Windows.Forms;

using KeePass.Plugins;
using KeePass.Services;

using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Security;

namespace KeePass.UI
{
	/// <summary>
	/// F15-C: ColumnProvider que añade una columna "Estado" a la lista de entradas.
	/// Indicadores: ⚠ expirada · ⏰ expira pronto · ◎ TOTP · ● calidad contraseña.
	/// Registrar con: Program.ColumnProviderPool.Add(new EntryStatusRenderer());
	/// </summary>
	public sealed class EntryStatusRenderer : ColumnProvider
	{
		private static readonly string[] m_colNames = new string[] { "Estado" };

		public override string[] ColumnNames { get { return m_colNames; } }

		public override HorizontalAlignment TextAlign
		{
			get { return HorizontalAlignment.Center; }
		}

		public override string GetCellData(string strColumnName, PwEntry pe)
		{
			if(pe == null) return string.Empty;

			StringBuilder sb = new StringBuilder();

			// ── Expiración ──────────────────────────────────────────────────────────
			if(pe.Expires)
			{
				DateTime now = DateTime.UtcNow;
				if(pe.ExpiryTime <= now)
					sb.Append("\u26A0 "); // ⚠ expirada
				else if((pe.ExpiryTime - now).TotalDays <= 14.0)
					sb.Append("\u23F0 "); // ⏰ expira pronto
			}

			// ── TOTP ────────────────────────────────────────────────────────────────
			if(TotpService.HasTotp(pe))
				sb.Append("\u25CE "); // ◎

			// ── Calidad de contraseña ───────────────────────────────────────────────
			ProtectedString ps = pe.Strings.GetSafe(PwDefs.PasswordField);
			string pwd = ps.ReadString();
			if(pwd.Length > 0)
			{
				uint bits = QualityEstimation.EstimatePasswordBits(pwd.ToCharArray());
				if(bits < 40)
					sb.Append("\u25CF");           // ● débil
				else if(bits < 80)
					sb.Append("\u25CF\u25CF");     // ●● medio
				else
					sb.Append("\u25CF\u25CF\u25CF"); // ●●● fuerte
			}

			return sb.ToString().TrimEnd();
		}
	}
}
