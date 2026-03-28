/*
  KeePass Modern Vibe — Registro central de servicios (AppServices)

  Punto único de acceso a todos los servicios de la capa Application.
  Los servicios estáticos (HibpService, BackupService, etc.) son clases estáticas
  y no necesitan instancia; se exponen aquí para mantener un API consistente.
  BrowserImportService es el único no-estático y se crea en Initialize().

  Uso:
      AppServices.Initialize();          // al arrancar / al abrir DB
      AppServices.BrowserImport.Import(…)
      AppServices.Shutdown();            // al cerrar DB o la app
*/

using KeePassLib;

namespace KeePass.Services
{
	/// <summary>
	/// Registro central de servicios de KeePass Modern Vibe.
	/// Initialize() debe llamarse desde MainForm después de cargar la DB;
	/// Shutdown() debe llamarse al cerrar la DB o salir de la aplicación.
	/// </summary>
	public static class AppServices
	{
		// ── Servicio no-estático ──────────────────────────────────────────────────

		/// <summary>Servicio de importación desde navegadores (F13).</summary>
		public static IBrowserImportService BrowserImport { get; private set; }

		// ── Estado ────────────────────────────────────────────────────────────────

		/// <summary>Devuelve true si AppServices ha sido inicializado.</summary>
		public static bool IsInitialized { get; private set; }

		// ── Ciclo de vida ────────────────────────────────────────────────────────

		/// <summary>
		/// Inicializa todos los servicios.
		/// Puede llamarse varias veces (al abrir otra DB); los servicios estáticos
		/// son idempotentes, sólo se recrea BrowserImportService.
		/// </summary>
		/// <param name="db">Base de datos activa (puede ser null si no hay ninguna abierta).</param>
		public static void Initialize(PwDatabase db = null)
		{
			BrowserImport = new BrowserImportService();
			IsInitialized = true;
		}

		/// <summary>
		/// Libera recursos y limpia el estado.
		/// Llamar al cerrar la DB o al salir de la aplicación.
		/// </summary>
		public static void Shutdown()
		{
			BrowserImport  = null;
			IsInitialized  = false;
		}
	}
}
