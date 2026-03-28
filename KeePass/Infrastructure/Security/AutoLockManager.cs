// F AutoLock — Bloqueo automático por inactividad y pérdida de foco
// Usa System.Windows.Forms.Timer (hilo UI) + Session-lock events.

using System;
using System.Windows.Forms;

using KeePass.App.Configuration;

namespace KeePass.Infrastructure.Security
{
	/// <summary>
	/// Monitors user activity and requests a database lock when the idle threshold
	/// is exceeded or when the OS session is locked.
	/// Attach to <see cref="LockRequested"/> from MainForm to perform the actual lock.
	/// </summary>
	public sealed class AutoLockManager : IDisposable
	{
		// ── Events ────────────────────────────────────────────────────────────

		/// <summary>Fired on the UI thread when the database should be locked.</summary>
		public event EventHandler LockRequested;

		// ── State ─────────────────────────────────────────────────────────────

		private readonly Timer    m_timer = new Timer();
		private bool              m_disposed;

		/// <summary>Gets whether the inactivity timer is currently running.</summary>
		public bool IsRunning { get; private set; }

		// ── Construction ──────────────────────────────────────────────────────

		public AutoLockManager()
		{
			m_timer.Tick += OnTimerTick;
		}

		// ── Lifecycle ─────────────────────────────────────────────────────────

		/// <summary>Starts monitoring. Call after opening a database.</summary>
		public void Start()
		{
			if(m_disposed) return;

			AceSecurity ace = (Program.Config != null) ? Program.Config.Security : null;
			if(ace == null) return;

			// Timer-based idle lock
			if(ace.WorkspaceLocking.LockAfterTime > 0)
			{
				m_timer.Interval = (int)(ace.WorkspaceLocking.LockAfterTime * 1000);
				if(m_timer.Interval < 1000) m_timer.Interval = 1000;
				m_timer.Start();
			}

			// OS session-lock event (already handled by MainForm via SessionLockNotifier)
			IsRunning = true;
		}

		/// <summary>Stops monitoring. Call when the database is closed or locked.</summary>
		public void Stop()
		{
			m_timer.Stop();
			IsRunning = false;
		}

		/// <summary>
		/// Must be called on any user activity (key press, mouse move) to reset the
		/// inactivity timer.
		/// </summary>
		public void NotifyActivity()
		{
			if(!IsRunning) return;

			// Restart the interval from now
			m_timer.Stop();

			AceSecurity ace = (Program.Config != null) ? Program.Config.Security : null;
			if(ace != null && ace.WorkspaceLocking.LockAfterTime > 0)
			{
				m_timer.Interval = (int)(ace.WorkspaceLocking.LockAfterTime * 1000);
				if(m_timer.Interval < 1000) m_timer.Interval = 1000;
				m_timer.Start();
			}
		}

		// ── Timer ─────────────────────────────────────────────────────────────

		private void OnTimerTick(object sender, EventArgs e)
		{
			m_timer.Stop(); // one-shot
			RequestLock();
		}

		// ── Internal ──────────────────────────────────────────────────────────

		private void RequestLock()
		{
			EventHandler h = LockRequested;
			if(h != null) h(this, EventArgs.Empty);
		}

		// ── IDisposable ───────────────────────────────────────────────────────

		public void Dispose()
		{
			if(m_disposed) return;
			m_disposed = true;
			m_timer.Stop();
			m_timer.Dispose();
			IsRunning = false;
		}
	}
}
