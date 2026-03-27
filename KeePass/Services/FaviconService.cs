/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F3 — Favicon Downloader
// Descarga favicon.ico del dominio de cada entrada y lo asigna como PwCustomIcon.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

using KeePassLib;
using KeePassLib.Delegates;
using KeePassLib.Utility;

namespace KeePass.Services
{
	/// <summary>Resultado de la descarga de favicon de una entrada.</summary>
	public struct FaviconResult
	{
		public PwEntry Entry;
		public bool Success;
		public string StatusMessage;
	}

	public static class FaviconService
	{
		private const int TimeoutMs = 8000;
		private const int IconSize = 16;

		/// <summary>
		/// Descarga el favicon de cada entrada con URL y lo asigna como icono personalizado.
		/// Se ejecuta de forma síncrona; úsalo desde un BackgroundWorker.
		/// </summary>
		/// <param name="onProgress">Callback (procesadas, total).</param>
		/// <param name="cancelQuery">Devuelve true para cancelar.</param>
		public static List<FaviconResult> DownloadForEntries(
			PwEntry[] entries, PwDatabase db,
			GAction<int, int> onProgress,
			GFunc<bool> cancelQuery)
		{
			List<FaviconResult> results = new List<FaviconResult>();
			if(entries == null || db == null || !db.IsOpen) return results;

			int total = entries.Length;
			for(int i = 0; i < total; i++)
			{
				if(cancelQuery != null && cancelQuery()) break;
				if(onProgress != null) onProgress(i, total);

				results.Add(DownloadOne(entries[i], db));
			}

			if(onProgress != null) onProgress(total, total);
			return results;
		}

		// ── descarga individual ──────────────────────────────────────────────────

		private static FaviconResult DownloadOne(PwEntry pe, PwDatabase db)
		{
			FaviconResult r;
			r.Entry = pe;
			r.Success = false;
			r.StatusMessage = string.Empty;

			string rawUrl = pe.Strings.ReadSafe(PwDefs.UrlField);
			if(string.IsNullOrEmpty(rawUrl)) { r.StatusMessage = "Sin URL"; return r; }

			// Construir URL del favicon
			string faviconUrl;
			try
			{
				string full = rawUrl.Contains("://") ? rawUrl : ("https://" + rawUrl);
				Uri uri = new Uri(full);
				faviconUrl = uri.Scheme + "://" + uri.Host + "/favicon.ico";
			}
			catch { r.StatusMessage = "URL inválida"; return r; }

			// Descargar bytes
			byte[] pb;
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(faviconUrl);
				req.Timeout = TimeoutMs;
				req.UserAgent = "KeePass/2";
				req.AllowAutoRedirect = true;

				using(WebResponse resp = req.GetResponse())
				using(Stream s = resp.GetResponseStream())
				using(MemoryStream ms = new MemoryStream())
				{
					byte[] buf = new byte[4096];
					int nRead;
					while((nRead = s.Read(buf, 0, buf.Length)) > 0)
						ms.Write(buf, 0, nRead);
					pb = ms.ToArray();
				}
			}
			catch(Exception ex) { r.StatusMessage = "Error red: " + ex.Message; return r; }

			if(pb == null || pb.Length == 0)
			{
				r.StatusMessage = "Respuesta vacía";
				return r;
			}

			// Convertir a PNG 16×16
			byte[] pbPng;
			try
			{
				Image img = GfxUtil.LoadImage(pb);
				Image scaled = GfxUtil.ScaleImage(img, IconSize, IconSize);
				using(MemoryStream ms = new MemoryStream())
				{
					scaled.Save(ms, ImageFormat.Png);
					pbPng = ms.ToArray();
				}
				img.Dispose();
				scaled.Dispose();
			}
			catch(Exception ex) { r.StatusMessage = "Error imagen: " + ex.Message; return r; }

			// Reusar icono idéntico existente o agregar uno nuevo
			lock(db.CustomIcons) // protección básica de la lista
			{
				int iExist = db.GetCustomIconIndex(pbPng);
				PwUuid uuid;
				if(iExist >= 0)
					uuid = db.CustomIcons[iExist].Uuid;
				else
				{
					uuid = new PwUuid(true);
					db.CustomIcons.Add(new PwCustomIcon(uuid, pbPng));
				}

				pe.CustomIconUuid = uuid;
			}

			r.Success = true;
			r.StatusMessage = "OK";
			return r;
		}
	}
}
