/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F13 — Importar desde Chrome / Firefox — lector SQLite sobre copia temporal.
// Chrome/Edge/Brave bloquean el archivo "Login Data" mientras están abiertos.
// Este helper copia el archivo a %TEMP%, lo abre, ejecuta la consulta y borra la copia.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace KeePass.Infrastructure.BrowserImport
{
	/// <summary>
	/// Lee una tabla de un archivo SQLite que puede estar bloqueado por otro proceso,
	/// haciendo primero una copia en la carpeta temporal del sistema.
	/// </summary>
	public static class SqliteTempReader
	{
		/// <summary>
		/// Copia <paramref name="sourcePath"/> a %TEMP%, ejecuta <paramref name="sql"/>
		/// y devuelve todas las filas como diccionarios columna→valor (string).
		/// La copia temporal se borra siempre, incluso si hay error.
		/// </summary>
		/// <param name="sourcePath">Ruta del archivo SQLite (posiblemente bloqueado).</param>
		/// <param name="sql">Sentencia SELECT a ejecutar.</param>
		/// <param name="columns">Nombres de columnas que se quieren extraer.</param>
		/// <returns>Lista de filas; cada fila es un diccionario columna→valor.</returns>
		public static List<Dictionary<string, object>> QueryFile(
			string sourcePath, string sql, string[] columns)
		{
			if(sourcePath == null) throw new ArgumentNullException("sourcePath");
			if(sql        == null) throw new ArgumentNullException("sql");
			if(!File.Exists(sourcePath))
				throw new FileNotFoundException("Archivo SQLite no encontrado.", sourcePath);

			string tmpPath = Path.Combine(Path.GetTempPath(),
				"kp_browser_" + Guid.NewGuid().ToString("N") + ".db");

			try
			{
				File.Copy(sourcePath, tmpPath, overwrite: true);
				return QueryCopy(tmpPath, sql, columns);
			}
			finally
			{
				try { if(File.Exists(tmpPath)) File.Delete(tmpPath); }
				catch { /* Ignorar errores al borrar la copia temporal */ }
			}
		}

		// ── privado ───────────────────────────────────────────────────────────────

		private static List<Dictionary<string, object>> QueryCopy(
			string dbPath, string sql, string[] columns)
		{
			var rows = new List<Dictionary<string, object>>();

			string connStr = string.Format(
				"Data Source={0};Version=3;Read Only=True;", dbPath);

			using(SQLiteConnection conn = new SQLiteConnection(connStr))
			{
				conn.Open();
				using(SQLiteCommand cmd = new SQLiteCommand(sql, conn))
				using(IDataReader reader = cmd.ExecuteReader())
				{
					while(reader.Read())
					{
						var row = new Dictionary<string, object>();
						if(columns != null)
						{
							foreach(string col in columns)
							{
								int ordinal = reader.GetOrdinal(col);
								row[col] = reader.IsDBNull(ordinal)
									? null
									: reader.GetValue(ordinal);
							}
						}
						else
						{
							for(int i = 0; i < reader.FieldCount; i++)
								row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
						}
						rows.Add(row);
					}
				}
			}

			return rows;
		}
	}
}
