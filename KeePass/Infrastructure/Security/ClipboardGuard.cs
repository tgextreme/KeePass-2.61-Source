// ClipboardGuard — Detección de acceso externo al portapapeles
// Usa AddClipboardFormatListener / RemoveClipboardFormatListener (Win32)
// para monitorizar cambios en el clipboard originados fuera de KeePass.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeePass.Infrastructure.Security
{
	/// <summary>
	/// Monitors the clipboard for external modifications.
	/// Subscribe to <see cref="ExternalAccessDetected"/> to be notified when
	/// another process reads or writes the clipboard while KeePass has a secret
	/// copied there.
	/// </summary>
	public sealed class ClipboardGuard : NativeWindow, IDisposable
	{
		// ── Win32 ─────────────────────────────────────────────────────────────

		private const int WM_CLIPBOARDUPDATE = 0x031D;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool AddClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

		// ── Events ────────────────────────────────────────────────────────────

		/// <summary>Raised on the UI thread when the clipboard content changes
		/// while guard monitoring is active.</summary>
		public event EventHandler ExternalAccessDetected;

		// ── State ─────────────────────────────────────────────────────────────

		private bool m_guarding;
		private bool m_disposed;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		/// <summary>
		/// Creates the hidden listener window and starts receiving clipboard events.
		/// </summary>
		public ClipboardGuard()
		{
			CreateParams cp = new CreateParams();
			// Create a message-only window (no visible UI)
			cp.Parent = (IntPtr)(-3); // HWND_MESSAGE
			CreateHandle(cp);
			AddClipboardFormatListener(this.Handle);
		}

		/// <summary>
		/// Enables change-detection.  Call immediately after your own clipboard write.
		/// </summary>
		public void StartGuarding()
		{
			m_guarding = true;
		}

		/// <summary>
		/// Disables change-detection.  Call before your own clipboard clear/write
		/// to avoid false positives.
		/// </summary>
		public void StopGuarding()
		{
			m_guarding = false;
		}

		/// <summary>Whether this guard is currently monitoring the clipboard.</summary>
		public bool IsGuarding { get { return m_guarding; } }

		// ── NativeWindow ──────────────────────────────────────────────────────

		protected override void WndProc(ref Message m)
		{
			if(m.Msg == WM_CLIPBOARDUPDATE && m_guarding)
			{
				// Raise on the message pump thread (UI thread)
				EventHandler h = ExternalAccessDetected;
				if(h != null) h(this, EventArgs.Empty);
			}
			base.WndProc(ref m);
		}

		// ── IDisposable ───────────────────────────────────────────────────────

		public void Dispose()
		{
			if(m_disposed) return;
			m_disposed = true;
			m_guarding = false;
			try { RemoveClipboardFormatListener(this.Handle); } catch { }
			try { DestroyHandle(); } catch { }
		}
	}
}
