/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F9 — Color Labels para Entradas
// Almacena el color de etiqueta en CustomData["KPMVibe.RowColor"] (hex RGB sin #).
// La fila se pinta con ese color mezclado al 40 % sobre blanco (pastel suave).

using System;
using System.Diagnostics;
using System.Drawing;

using KeePassLib;

namespace KeePass.Services
{
	public static class ColorLabelService
	{
		private const string CustomDataKey = "KPMVibe.RowColor";

		// 8 colores predefinidos (saturados; se muestran como pastel en la fila)
		public static readonly Color[] PredefinedColors = new Color[]
		{
			Color.FromArgb(228,  87,  86),  // Rojo
			Color.FromArgb(230, 126,  34),  // Naranja
			Color.FromArgb(241, 196,  15),  // Amarillo
			Color.FromArgb( 39, 174,  96),  // Verde
			Color.FromArgb( 41, 128, 185),  // Azul
			Color.FromArgb(142,  68, 173),  // Morado
			Color.FromArgb(231,  76, 137),  // Rosa
			Color.FromArgb(149, 165, 166),  // Gris
		};

		public static readonly string[] PredefinedColorNames = new string[]
		{
			"Rojo", "Naranja", "Amarillo", "Verde", "Azul", "Morado", "Rosa", "Gris"
		};

		/// <summary>Asigna (o elimina si <paramref name="color"/> es null) el color de etiqueta.</summary>
		public static void SetColor(PwEntry pe, Color? color)
		{
			if(pe == null) { Debug.Assert(false); return; }

			if(color.HasValue)
				pe.CustomData.Set(CustomDataKey, ColorToHex(color.Value));
			else
				pe.CustomData.Remove(CustomDataKey);
		}

		/// <summary>Devuelve el color de etiqueta asignado, o null si no hay ninguno.</summary>
		public static Color? GetColor(PwEntry pe)
		{
			if(pe == null) return null;

			string str = pe.CustomData.Get(CustomDataKey);
			if(string.IsNullOrEmpty(str)) return null;

			return HexToColor(str);
		}

		/// <summary>Mezcla el color saturado con blanco al 40 % → tono pastel suave.</summary>
		public static Color Blend(Color src)
		{
			const int alpha = 102; // 40 % de 255
			int r = (src.R * alpha + 255 * (255 - alpha)) / 255;
			int g = (src.G * alpha + 255 * (255 - alpha)) / 255;
			int b = (src.B * alpha + 255 * (255 - alpha)) / 255;
			return Color.FromArgb(r, g, b);
		}

		// ── helpers privados ─────────────────────────────────────────────────────

		private static string ColorToHex(Color c)
		{
			return string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
		}

		private static Color? HexToColor(string hex)
		{
			try
			{
				if(hex == null || hex.Length != 6) return null;
				int r = Convert.ToInt32(hex.Substring(0, 2), 16);
				int g = Convert.ToInt32(hex.Substring(2, 2), 16);
				int b = Convert.ToInt32(hex.Substring(4, 2), 16);
				return Color.FromArgb(r, g, b);
			}
			catch { return null; }
		}
	}
}
