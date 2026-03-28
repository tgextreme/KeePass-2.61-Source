/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F10 — Windows Hello / Biometría para Desbloqueo
// Interfaz del servicio y DTO de clave recuperada.

using System;

namespace KeePass.Integration.WindowsHello
{
	/// <summary>Datos de clave almacenados y recuperados de Windows Hello.</summary>
	public sealed class HelloKeyData
	{
		/// <summary>Bytes UTF-8 de la contraseña maestra, o null si no había contraseña.</summary>
		public byte[] PasswordUtf8 { get; set; }

		/// <summary>Ruta al archivo de clave, o null/vacío si no se usa.</summary>
		public string KeyFilePath { get; set; }

		/// <summary>True si la clave incluía el componente de cuenta de usuario Windows.</summary>
		public bool HasUserAccount { get; set; }
	}

	/// <summary>
	/// Servicio de Windows Hello: disponibilidad, inscripción, verificación biométrica
	/// y recuperación de la clave maestra cifrada.
	/// </summary>
	public interface IWindowsHelloService
	{
		/// <summary>True si Windows Hello está disponible y configurado en este equipo (Win10+).</summary>
		bool IsAvailable();

		/// <summary>True si hay una clave inscrita en Windows Credential Manager para <paramref name="dbPath"/>.</summary>
		bool IsEnrolled(string dbPath);

		/// <summary>
		/// Cifra los datos de clave con DPAPI y los almacena en Windows Credential Manager.
		/// Se llama tras un desbloqueo exitoso con contraseña si el usuario opta por Hello.
		/// </summary>
		void Enroll(byte[] passwordUtf8, string keyFilePath, bool hasUserAccount, string dbPath);

		/// <summary>
		/// Muestra el diálogo de Windows Hello (biometría/PIN). Si el usuario se verifica,
		/// recupera y descifra los datos de clave almacenados.
		/// </summary>
		/// <returns><see cref="HelloKeyData"/> en caso de éxito, o null si se cancela/falla.</returns>
		HelloKeyData RetrieveKey(IntPtr ownerHwnd, string dbPath);

		/// <summary>Elimina la inscripción almacenada para <paramref name="dbPath"/>.</summary>
		void RemoveEnrollment(string dbPath);
	}
}
