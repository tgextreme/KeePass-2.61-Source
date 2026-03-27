/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F6 — Columna TOTP
// Añade una columna "TOTP" a la lista de entradas que muestra el código
// de un solo uso actual (6 dígitos, actualizado cada vez que se refresca la lista).
// El usuario activa la columna desde Ver > Configurar columnas.
// Se registra en: Program.ColumnProviderPool.Add(new TotpColumnProvider());

using System.Windows.Forms;

using KeePass.Services;

using KeePassLib;

namespace KeePass.UI
{
	/// <summary>
	/// Proveedor de columna personalizada que muestra el código TOTP actual
	/// para entradas que tienen configurado un secreto TimeOtp-Secret-Base32 (u otro).
	/// </summary>
	public sealed class TotpColumnProvider : ColumnProvider
	{
		private static readonly string[] m_colNames = new string[] { "TOTP" };

		public override string[] ColumnNames
		{
			get { return m_colNames; }
		}

		public override HorizontalAlignment TextAlign
		{
			get { return HorizontalAlignment.Center; }
		}

		public override string GetCellData(string strColumnName, PwEntry pe)
		{
			// Solo actúa sobre la columna "TOTP"
			if(strColumnName != "TOTP") return string.Empty;
			return TotpService.GetTotp(pe);
		}

		/// <summary>
		/// Solicita a KeePass que refresque la columna periódicamente
		/// (cada vez que el usuario hace clic o refresca la lista).
		/// </summary>
		public override bool SupportsCellAction(string strColumnName)
		{
			return false; // sin acción al hacer doble clic
		}
	}
}
