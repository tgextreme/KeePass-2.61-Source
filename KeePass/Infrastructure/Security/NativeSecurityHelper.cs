// F2 Streamer / F7 QR anti-screenshot — NativeSecurityHelper
// Envuelve SetWindowDisplayAffinity (Win32) para impedir capturas de pantalla.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeePass.Infrastructure.Security
{
	/// <summary>
	/// Provides Win32 helpers for window display security.
	/// </summary>
	public static class NativeSecurityHelper
	{
		// dwAffinity constants (dwmapi / user32 union)
		private const uint WDA_NONE                 = 0x00000000;
		private const uint WDA_MONITOR              = 0x00000001;
		private const uint WDA_EXCLUDEFROMCAPTURE   = 0x00000011;  // Win10 2004+

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		// ── Public API ────────────────────────────────────────────────────────

		/// <summary>
		/// Prevents the given window from appearing in screenshots and screen-share tools.
		/// Falls back gracefully on OS versions that don't support WDA_EXCLUDEFROMCAPTURE.
		/// </summary>
		public static void EnableAntiScreenshot(Form form)
		{
			if(form == null) return;
			try
			{
				// Try the stronger constant first (Win10 2004+)
				if(!SetWindowDisplayAffinity(form.Handle, WDA_EXCLUDEFROMCAPTURE))
					SetWindowDisplayAffinity(form.Handle, WDA_MONITOR);
			}
			catch { /* not supported on this OS — silently ignore */ }
		}

		/// <summary>Removes the anti-screenshot protection from the given window.</summary>
		public static void DisableAntiScreenshot(Form form)
		{
			if(form == null) return;
			try { SetWindowDisplayAffinity(form.Handle, WDA_NONE); }
			catch { }
		}

		/// <summary>
		/// Returns true if the OS supports SetWindowDisplayAffinity
		/// (Windows Vista or later).
		/// </summary>
		public static bool IsAntiScreenshotSupported
		{
			get
			{
				return (Environment.OSVersion.Platform == PlatformID.Win32NT &&
				        Environment.OSVersion.Version >= new Version(6, 0));
			}
		}
	}
}
