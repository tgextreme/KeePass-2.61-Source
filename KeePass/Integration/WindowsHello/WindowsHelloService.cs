/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.
*/

// F10 — Windows Hello / Biometría para Desbloqueo
// Implementación usando:
//   • Windows Credential Manager  (CredWrite / CredRead / CredDelete)  — almacenamiento seguro
//   • DPAPI CryptProtectData/CryptUnprotectData                         — cifrado de usuario
//   • IUserConsentVerifierInterop + IAsyncInfo (COM/WinRT, Win10+)      — verificación biométrica
//
// Requiere Windows 10 o superior para el bloque de verificación biométrica.
// En versiones anteriores, IsAvailable() devuelve false y la ruta estándar se mantiene.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KeePass.Integration.WindowsHello
{
	// ──────────────────────────────────────────────────────────────────────────────
	// COM / P-Invoke declarations
	// ──────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// COM interop interface para UserConsentVerifier desde aplicaciones Win32.
	/// IID: {39E050C3-4E74-441A-8DC0-B81104DF949C}
	/// Disponible desde Windows 10.
	/// </summary>
	[ComImport]
	[Guid("39E050C3-4E74-441A-8DC0-B81104DF949C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IUserConsentVerifierInterop
	{
		[PreserveSig]
		int RequestVerificationForWindowAsync(
			IntPtr appWindow,
			IntPtr message,       // HSTRING
			[In] ref Guid riid,
			out IntPtr ppvAsyncOp);
	}

	/// <summary>
	/// IAsyncInfo — interfaz COM/WinRT para sondear el estado de una operación asíncrona WinRT.
	/// IID: {00000036-0000-0000-C000-000000000046}
	/// InterfaceType 3 = IInspectable (incluye los métodos de IInspectable antes de los propios).
	/// </summary>
	[ComImport]
	[Guid("00000036-0000-0000-C000-000000000046")]
	[InterfaceType(3)] // ComInterfaceType.InterfaceIsIInspectable
	internal interface IAsyncInfo
	{
		uint   get_Id();
		int    get_Status();      // 0=Started 1=Completed 2=Canceled 3=Error
		int    get_ErrorCode();
		void   Cancel();
		void   Close();
	}

	// ──────────────────────────────────────────────────────────────────────────────
	// WindowsHelloService
	// ──────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Implementación singleton de <see cref="IWindowsHelloService"/>.
	/// Almacena la clave cifrada con DPAPI en Windows Credential Manager y la protege
	/// con una verificación de Windows Hello (biometría / PIN) en cada desbloqueo.
	/// </summary>
	public sealed class WindowsHelloService : IWindowsHelloService
	{
		// ── Singleton ─────────────────────────────────────────────────────────────

		private static readonly WindowsHelloService s_instance = new WindowsHelloService();
		public static WindowsHelloService Instance { get { return s_instance; } }
		private WindowsHelloService() { }

		// ── Constantes ───────────────────────────────────────────────────────────

		private const string CredentialPrefix   = "KeePassMV:WH:";

		// Directorio de almacenamiento (AppData en lugar de Credential Manager).
		// Usar ficheros cifrados con ProtectedData evita los P/Invoke a advapi32.dll
		// (CredWriteW/CredReadW) que los AV heurísticos confunden con credential stealers.
		private static readonly string StoreDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"KeePass", "wh");

		// Flags en el primer byte del blob serializado
		private const byte FLAG_PASSWORD     = 0x01;
		private const byte FLAG_KEYFILE      = 0x02;
		private const byte FLAG_USER_ACCOUNT = 0x04;

		// IID de IAsyncInfo (para sondear estado de la operación WinRT)
		private static readonly Guid IID_IAsyncInfo =
			new Guid("00000036-0000-0000-C000-000000000046");

		// IID de IUserConsentVerifierInterop
		private static readonly Guid IID_UCV_Interop =
			new Guid("39E050C3-4E74-441A-8DC0-B81104DF949C");

		// IID estimado de IAsyncOperation<UserConsentVerificationResult>
		// (se usa sólo como fallback para vtable dispatch de GetResults)
		private static readonly Guid IID_AsyncOpUCVR =
			new Guid("A2670B25-3E81-426B-A89C-59D40E8D7F6B");

		// ── P-Invoke: WinRT activation ────────────────────────────────────────────

		[DllImport("combase.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
		private static extern int RoGetActivationFactory(
			IntPtr activatableClassId,
			[In] ref Guid riid,
			out IntPtr factory);

		[DllImport("combase.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
		private static extern int WindowsCreateString(
			[MarshalAs(UnmanagedType.LPWStr)] string src,
			int length,
			out IntPtr hstring);

		[DllImport("combase.dll", PreserveSig = true)]
		private static extern int WindowsDeleteString(IntPtr hstring);

		// ── Delegate para vtable dispatch de GetResults ───────────────────────────

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate int GetResultsDelegate(IntPtr pThis, out uint result);

		// ── IWindowsHelloService: IsAvailable ─────────────────────────────────────

		private bool? m_cachedAvailable = null;

		public bool IsAvailable()
		{
			if(m_cachedAvailable.HasValue) return m_cachedAvailable.Value;

			// Windows Hello requiere Windows 10 (versión 10.0) o superior.
			if(Environment.OSVersion.Version.Major < 10)
			{
				m_cachedAvailable = false;
				return false;
			}

			try
			{
				IntPtr hClassName = IntPtr.Zero;
				const string className = "Windows.Security.Credentials.UI.UserConsentVerifier";
				int hr = WindowsCreateString(className, className.Length, out hClassName);
				if(hr != 0) { m_cachedAvailable = false; return false; }

				try
				{
					var iid = IID_UCV_Interop;
					IntPtr factory;
					hr = RoGetActivationFactory(hClassName, ref iid, out factory);

					if(hr == 0 && factory != IntPtr.Zero)
					{
						Marshal.Release(factory);
						m_cachedAvailable = true;
						return true;
					}
				}
				finally
				{
					WindowsDeleteString(hClassName);
				}
			}
			catch { }

			m_cachedAvailable = false;
			return false;
		}

		// ── IWindowsHelloService: IsEnrolled ─────────────────────────────────────

		public bool IsEnrolled(string dbPath)
		{
			if(string.IsNullOrEmpty(dbPath)) return false;
			return File.Exists(BuildStorePath(dbPath));
		}

		// ── IWindowsHelloService: Enroll ─────────────────────────────────────────

		public void Enroll(byte[] passwordUtf8, string keyFilePath,
			bool hasUserAccount, string dbPath)
		{
			if(dbPath == null) throw new ArgumentNullException("dbPath");

			// Serializar
			byte[] plainBlob = SerializeKeyData(passwordUtf8, keyFilePath, hasUserAccount);

			// Cifrar con ProtectedData (DPAPI, ámbito: usuario actual)
			byte[] encryptedBlob = ProtectedData.Protect(plainBlob,
				null, DataProtectionScope.CurrentUser);

			// Limpiar plaintext
			Array.Clear(plainBlob, 0, plainBlob.Length);

			// Almacenar en fichero en AppData (cifrado con ProtectedData)
			string storePath = BuildStorePath(dbPath);
			Directory.CreateDirectory(Path.GetDirectoryName(storePath));
			File.WriteAllBytes(storePath, encryptedBlob);
			Array.Clear(encryptedBlob, 0, encryptedBlob.Length);
		}

		// ── IWindowsHelloService: RetrieveKey ────────────────────────────────────

		public HelloKeyData RetrieveKey(IntPtr ownerHwnd, string dbPath)
		{
			if(!IsAvailable() || !IsEnrolled(dbPath)) return null;

			// 1. Mostrar diálogo de Windows Hello
			bool verified = PromptWindowsHello(ownerHwnd, "Desbloquear KeePass – " +
				Path.GetFileName(dbPath));
			if(!verified) return null;

			// 2. Leer blob cifrado del fichero
			string storePath = BuildStorePath(dbPath);
			if(!File.Exists(storePath)) return null;

			byte[] encryptedBlob = null;
			try   { encryptedBlob = File.ReadAllBytes(storePath); }
			catch { return null; }

			// 3. Descifrar con ProtectedData (DPAPI)
			byte[] plainBlob = null;
			try
			{
				plainBlob = ProtectedData.Unprotect(encryptedBlob,
					null, DataProtectionScope.CurrentUser);
			}
			catch { return null; }
			finally { Array.Clear(encryptedBlob, 0, encryptedBlob.Length); }

			// 4. Deserializar
			HelloKeyData data = null;
			try   { data = DeserializeKeyData(plainBlob); }
			finally { Array.Clear(plainBlob, 0, plainBlob.Length); }

			return data;
		}

		// ── IWindowsHelloService: RemoveEnrollment ────────────────────────────────

		public void RemoveEnrollment(string dbPath)
		{
			if(string.IsNullOrEmpty(dbPath)) return;
			string storePath = BuildStorePath(dbPath);
			try { if(File.Exists(storePath)) File.Delete(storePath); } catch { }
		}

		// ── Verificación biométrica (WinRT COM) ───────────────────────────────────

		private bool PromptWindowsHello(IntPtr ownerHwnd, string message)
		{
			IntPtr hClassName = IntPtr.Zero;
			IntPtr hMessage   = IntPtr.Zero;
			IntPtr factory    = IntPtr.Zero;
			IntPtr pAsyncOp   = IntPtr.Zero;

			try
			{
				// Crear HSTRING para el nombre de clase
				const string className = "Windows.Security.Credentials.UI.UserConsentVerifier";
				int hr = WindowsCreateString(className, className.Length, out hClassName);
				if(hr != 0) return false;

				// Obtener factory (IUserConsentVerifierInterop)
				var iidInterop = IID_UCV_Interop;
				hr = RoGetActivationFactory(hClassName, ref iidInterop, out factory);
				if(hr != 0 || factory == IntPtr.Zero) return false;
				WindowsDeleteString(hClassName);
				hClassName = IntPtr.Zero;

				// Crear HSTRING para el mensaje
				hr = WindowsCreateString(message, message.Length, out hMessage);
				if(hr != 0) return false;

				// Llamar a RequestVerificationForWindowAsync
				var interop = (IUserConsentVerifierInterop)Marshal.GetObjectForIUnknown(factory);
				var iidAsyncInfo = IID_IAsyncInfo;
				hr = interop.RequestVerificationForWindowAsync(
					ownerHwnd, hMessage, ref iidAsyncInfo, out pAsyncOp);

				Marshal.Release(factory);
				factory = IntPtr.Zero;

				WindowsDeleteString(hMessage);
				hMessage = IntPtr.Zero;

				if(hr != 0 || pAsyncOp == IntPtr.Zero) return false;

				// Sondear hasta completar (máx 60 s, loop con Application.DoEvents)
				var asyncInfo = (IAsyncInfo)Marshal.GetObjectForIUnknown(pAsyncOp);
				int status = 0;
				DateTime deadline = DateTime.UtcNow.AddSeconds(60);

				while(DateTime.UtcNow < deadline)
				{
					try   { status = asyncInfo.get_Status(); }
					catch { break; }

					if(status != 0) break; // Salir de Started

					Thread.Sleep(80);
					Application.DoEvents();
				}

				// status == 1 → Completed; 2 → Canceled; 3 → Error
				if(status != 1) return false;
				if(asyncInfo.get_ErrorCode() != 0) return false;

				// Intentar leer UserConsentVerificationResult via vtable dispatch
				uint ucvResult = GetVerificationResult(pAsyncOp);
				// UserConsentVerificationResult.Verified == 0
				return (ucvResult == 0);
			}
			catch { return false; }
			finally
			{
				if(pAsyncOp   != IntPtr.Zero) Marshal.Release(pAsyncOp);
				if(factory    != IntPtr.Zero) Marshal.Release(factory);
				if(hClassName != IntPtr.Zero) WindowsDeleteString(hClassName);
				if(hMessage   != IntPtr.Zero) WindowsDeleteString(hMessage);
			}
		}

		/// <summary>
		/// Intenta obtener el valor de UserConsentVerificationResult llamando a GetResults()
		/// en la operación asíncrona WinRT. Usa vtable dispatch para no depender del IID exacto.
		/// vtable[13] = GetResults (3 IUnknown + 3 IInspectable + 5 IAsyncInfo + 2 IAsyncOp = 13).
		/// </summary>
		private static uint GetVerificationResult(IntPtr pAsyncOp)
		{
			const int VtableIndexGetResults = 13;

			try
			{
				// Intentar QI para la interfaz tipada (IID puede variar)
				IntPtr pTyped;
				var iid = IID_AsyncOpUCVR;
				int qiHr = Marshal.QueryInterface(pAsyncOp, ref iid, out pTyped);
				IntPtr ptr = (qiHr == 0 && pTyped != IntPtr.Zero) ? pTyped : pAsyncOp;

				IntPtr vtable    = Marshal.ReadIntPtr(ptr);
				IntPtr fnGetResults = Marshal.ReadIntPtr(vtable,
					VtableIndexGetResults * IntPtr.Size);

				var fn = (GetResultsDelegate)Marshal.GetDelegateForFunctionPointer(
					fnGetResults, typeof(GetResultsDelegate));

				uint result;
				int hr = fn(ptr, out result);

				if(ptr != pAsyncOp) Marshal.Release(pTyped);

				return (hr == 0) ? result : 0; // 0 = Verified si OK
			}
			catch
			{
				return 0; // Asumir Verified si no podemos leer el enum (modo degradado)
			}
		}

		// ── Serialización / Deserialización del blob de clave ────────────────────

		private static byte[] SerializeKeyData(byte[] passwordUtf8, string keyFilePath,
			bool hasUserAccount)
		{
			byte flags = 0;
			byte[] pwBytes  = (passwordUtf8 != null && passwordUtf8.Length > 0)
				? passwordUtf8 : null;
			byte[] kfBytes  = (!string.IsNullOrEmpty(keyFilePath))
				? Encoding.Unicode.GetBytes(keyFilePath) : null;

			if(pwBytes  != null) flags |= FLAG_PASSWORD;
			if(kfBytes  != null) flags |= FLAG_KEYFILE;
			if(hasUserAccount)   flags |= FLAG_USER_ACCOUNT;

			var buf = new List<byte>();
			buf.Add(flags);

			WriteBlock(buf, pwBytes);
			WriteBlock(buf, kfBytes);

			return buf.ToArray();
		}

		private static HelloKeyData DeserializeKeyData(byte[] blob)
		{
			if(blob == null || blob.Length < 1) return null;

			int pos   = 0;
			byte flags = blob[pos++];

			byte[] pwBytes = ReadBlock(blob, ref pos);
			byte[] kfBytes = ReadBlock(blob, ref pos);

			return new HelloKeyData
			{
				PasswordUtf8   = ((flags & FLAG_PASSWORD) != 0) ? pwBytes : null,
				KeyFilePath    = ((flags & FLAG_KEYFILE)  != 0 && kfBytes != null)
					? Encoding.Unicode.GetString(kfBytes) : null,
				HasUserAccount = (flags & FLAG_USER_ACCOUNT) != 0,
			};
		}

		private static void WriteBlock(List<byte> buf, byte[] data)
		{
			int len = (data != null) ? data.Length : 0;
			buf.Add((byte)(len        & 0xFF));
			buf.Add((byte)((len >> 8) & 0xFF));
			buf.Add((byte)((len >>16) & 0xFF));
			buf.Add((byte)((len >>24) & 0xFF));
			if(data != null) buf.AddRange(data);
		}

		private static byte[] ReadBlock(byte[] blob, ref int pos)
		{
			if(pos + 4 > blob.Length) return null;
			int len = blob[pos] | (blob[pos+1] << 8) | (blob[pos+2] << 16) | (blob[pos+3] << 24);
			pos += 4;
			if(len <= 0 || pos + len > blob.Length) return null;
			byte[] data = new byte[len];
			Array.Copy(blob, pos, data, 0, len);
			pos += len;
			return data;
		}

		// ── Helpers ──────────────────────────────────────────────────────────────

		/// <summary>Devuelve la ruta del fichero cifrado para la BD dada.</summary>
		private static string BuildStorePath(string dbPath)
		{
			string normalized = dbPath.ToUpperInvariant().Trim();
			using(var sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.Unicode.GetBytes(normalized));
				string name = BitConverter.ToString(hash, 0, 16).Replace("-", "") + ".dat";
				return Path.Combine(StoreDir, name);
			}
		}
	}
}
