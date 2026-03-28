/*
  KeePass Modern Vibe — Registro central de servicios (AppServices)

  Punto único de acceso a todos los servicios de la capa Application.
  Los servicios estáticos (HibpService, BackupService, etc.) son clases estáticas
  y no necesitan instancia; se exponen aquí para mantener un API consistente.
  BrowserImportService y los servicios de arquitectura base son instancias
  creadas en Initialize().

  Uso:
      AppServices.Initialize();          // al arrancar / al abrir DB
      AppServices.BrowserImport.Import(…)
      AppServices.Search.Search(db, q)
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
		// ── Servicios F13 ─────────────────────────────────────────────────────────

		/// <summary>Servicio de importación desde navegadores (F13).</summary>
		public static IBrowserImportService BrowserImport { get; private set; }

		// ── Servicios de arquitectura base ────────────────────────────────────────

		/// <summary>Acceso uniforme a los campos de una entrada (título, usuario, contraseña…).</summary>
		public static ICredentialService Credentials { get; private set; }

		/// <summary>Búsqueda de entradas por texto y dominio.</summary>
		public static ISearchService Search { get; private set; }

		/// <summary>Copia segura al portapapeles con auto-borrado.</summary>
		public static IClipboardService Clipboard { get; private set; }

		/// <summary>Anti-captura de pantalla y modo streamer.</summary>
		public static ISecurityService Security { get; private set; }

		/// <summary>Análisis de contraseñas débiles y duplicadas.</summary>
		public static IPasswordAnalysisService Analysis { get; private set; }

		/// <summary>Métricas de seguridad globales de la base de datos.</summary>
		public static IDashboardService Dashboard { get; private set; }

		// ── Estado ────────────────────────────────────────────────────────────────

		/// <summary>Devuelve true si AppServices ha sido inicializado.</summary>
		public static bool IsInitialized { get; private set; }

		// ── Ciclo de vida ────────────────────────────────────────────────────────

		/// <summary>
		/// Inicializa todos los servicios.
		/// Puede llamarse varias veces (al abrir otra DB); los servicios son
		/// idempotentes y se recrean en cada llamada.
		/// </summary>
		/// <param name="db">Base de datos activa (puede ser null si no hay ninguna abierta).</param>
		public static void Initialize(PwDatabase db = null)
		{
			BrowserImport = new BrowserImportService();
			Credentials   = new CredentialService();
			Search        = new SearchService();
			Clipboard     = new ClipboardService();
			Security      = new SecurityService();
			Analysis      = new PasswordAnalysisService();
			Dashboard     = new DashboardService(Analysis);
			IsInitialized = true;
		}

		/// <summary>
		/// Libera recursos y limpia el estado.
		/// Llamar al cerrar la DB o al salir de la aplicación.
		/// </summary>
		public static void Shutdown()
		{
			// Liberar servicios que implementan IDisposable
			if(Clipboard is System.IDisposable cd) cd.Dispose();

			BrowserImport = null;
			Credentials   = null;
			Search        = null;
			Clipboard     = null;
			Security      = null;
			Analysis      = null;
			Dashboard     = null;
			IsInitialized = false;
		}
	}
}
