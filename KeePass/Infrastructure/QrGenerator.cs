/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F7 — QR Code para Móvil — generador de imágenes QR.
// Usa la librería QRCoder (NuGet, MIT) para producir Bitmaps listos para mostrar en WinForms.
// No escribe nada en disco; la imagen vive únicamente en memoria durante el tiempo que
// el QrCodeForm esté abierto.

using System;
using System.Drawing;

using QRCoder;

namespace KeePass.Infrastructure
{
	/// <summary>
	/// Genera imágenes QR en memoria a partir de texto arbitrario.
	/// Thread-safe (instancia sin estado).
	/// </summary>
	public static class QrGenerator
	{
		/// <summary>Píxeles por módulo del QR (tamaño de cada "cuadrado" negro/blanco).</summary>
		private const int PixelsPerModule = 10;

		/// <summary>
		/// Convierte <paramref name="text"/> en un Bitmap con el código QR correspondiente.
		/// El llamador es responsable de desechar (<c>Dispose</c>) el Bitmap.
		/// </summary>
		/// <param name="text">Texto que codificará el QR (UTF-8). No puede ser nulo ni vacío.</param>
		/// <returns>Bitmap cuadrado con el QR generado.</returns>
		/// <exception cref="ArgumentNullException">Si <paramref name="text"/> es nulo.</exception>
		/// <exception cref="ArgumentException">Si <paramref name="text"/> está vacío.</exception>
		public static Bitmap GenerateQr(string text)
		{
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("El texto no puede estar vacío.", "text");

			using(QRCodeGenerator generator = new QRCodeGenerator())
			using(QRCodeData data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M))
			using(QRCode qrCode = new QRCode(data))
			{
				// GetGraphic devuelve un Bitmap; aquí creamos una copia para que el
				// using-block pueda disponer qrCode sin afectar al Bitmap devuelto.
				Bitmap original = qrCode.GetGraphic(PixelsPerModule);
				Bitmap copy = new Bitmap(original);
				original.Dispose();
				return copy;
			}
		}
	}
}
