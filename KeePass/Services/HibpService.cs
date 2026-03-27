/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F1 — HIBP Checker
// Comprueba si las contraseñas de la base de datos han sido filtradas usando
// el servicio "Have I Been Pwned" (https://haveibeenpwned.com/API/v3#SearchingPwnedPasswordsByRange).
// Se usa el modelo k-anonimato: solo se envían los 5 primeros caracteres del hash SHA-1.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using KeePassLib;
using KeePassLib.Delegates;

namespace KeePass.Services
{
	/// <summary>Entrada cuya contraseña aparece en bases de datos de credenciales filtradas.</summary>
	public struct PwnedEntry
	{
		/// <summary>La entrada de KeePass.</summary>
		public PwEntry Entry;
		/// <summary>Número de veces que la contraseña aparece en la base de datos de HIBP.</summary>
		public long Count;
	}

	/// <summary>
	/// Servicio de comprobación de contraseñas contra la API Have I Been Pwned.
	/// Utiliza k-anonimato: solo el prefijo de 5 caracteres del hash SHA-1 viaja por la red.
	/// </summary>
	public static class HibpService
	{
		private const string ApiBase = "https://api.pwnedpasswords.com/range/";

		/// <summary>
		/// Comprueba todas las entradas de la base de datos contra HIBP.
		/// Devuelve únicamente las entradas cuya contraseña aparece comprometida.
		/// Se ejecuta de forma síncrona; llama a este método desde un hilo de fondo.
		/// </summary>
		/// <param name="db">Base de datos activa (no nula).</param>
		/// <param name="onProgress">Callback (procesadas, total) para reportar avance; puede ser null.</param>
		/// <param name="cancelQuery">Devuelve true si se debe cancelar; puede ser null.</param>
		public static List<PwnedEntry> CheckAll(PwDatabase db,
			GAction<int, int> onProgress, GFunc<bool> cancelQuery)
		{
			if(db == null) throw new ArgumentNullException("db");

			// Recorrer todas las entradas del árbol
			List<PwEntry> allEntries = new List<PwEntry>();
			EntryHandler eh = delegate(PwEntry pe)
			{
				allEntries.Add(pe);
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);

			List<PwnedEntry> results = new List<PwnedEntry>();
			int total = allEntries.Count;

			for(int i = 0; i < total; i++)
			{
				if(cancelQuery != null && cancelQuery()) break;
				if(onProgress != null) onProgress(i, total);

				PwEntry pe = allEntries[i];
				string pwd = pe.Strings.ReadSafe(PwDefs.PasswordField);
				if(string.IsNullOrEmpty(pwd)) continue;

				long count = QueryHibp(pwd);
				if(count > 0)
				{
					PwnedEntry p = new PwnedEntry();
					p.Entry = pe;
					p.Count = count;
					results.Add(p);
				}
			}

			if(onProgress != null) onProgress(total, total);
			return results;
		}

		/// <summary>
		/// Consulta la API HIBP para una contraseña concreta.
		/// Devuelve el número de veces que aparece comprometida, o 0 si no lo está.
		/// </summary>
		private static long QueryHibp(string password)
		{
			try
			{
				// Calcular SHA-1 de la contraseña
				byte[] hash;
				using(SHA1 sha = SHA1.Create())
					hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

				string hex    = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
				string prefix = hex.Substring(0, 5);
				string suffix = hex.Substring(5);

				// Petición k-anónima: solo enviamos el prefijo
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ApiBase + prefix);
				req.Timeout    = 10000;
				req.UserAgent  = "KeePass/2";
				req.Method     = "GET";

				using(WebResponse resp = req.GetResponse())
				using(StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					string line;
					while((line = sr.ReadLine()) != null)
					{
						int colon = line.IndexOf(':');
						if(colon < 0) continue;

						string lineSuffix = line.Substring(0, colon).Trim().ToUpperInvariant();
						if(string.Equals(lineSuffix, suffix, StringComparison.Ordinal))
						{
							long count;
							if(long.TryParse(line.Substring(colon + 1).Trim(), out count))
								return count;
							return 1;
						}
					}
				}
			}
			catch { /* Error de red o de análisis; se ignora silenciosamente */ }

			return 0; // No comprometida (o no se pudo comprobar)
		}
	}
}
