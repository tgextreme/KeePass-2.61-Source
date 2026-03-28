// ClipboardService — Portapapeles seguro con auto-limpieza (IClipboardService)

using System;
using System.Windows.Forms;

using KeePass.Util;
using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Default implementation of <see cref="IClipboardService"/>.
	/// Delegates the actual clipboard manipulation to the existing
	/// <c>ClipboardUtil</c> class and adds countdown + auto-clear on top.
	/// </summary>
	public sealed class ClipboardService : IClipboardService, IDisposable
	{
		private readonly System.Windows.Forms.Timer m_timer = new System.Windows.Forms.Timer();
		private int  m_secondsLeft;
		private bool m_disposed;

		public ClipboardService()
		{
			m_timer.Interval = 1000; // 1 s tick
			m_timer.Tick += OnTick;
		}

		// ── IClipboardService ─────────────────────────────────────────────────

		public int SecondsToClear { get { return m_secondsLeft; } }

		public void CopySecure(ProtectedString value, int clearAfterSeconds)
		{
			if(value == null) return;
			ClipboardUtil.Copy(value.ReadString(), false, false, null, null, IntPtr.Zero);

			if(clearAfterSeconds > 0)
			{
				m_secondsLeft = clearAfterSeconds;
				m_timer.Start();
			}
		}

		public void CopyPlain(string value)
		{
			if(value == null) return;
			try { Clipboard.SetText(value); } catch { }
		}

		public void Clear()
		{
			m_timer.Stop();
			m_secondsLeft = 0;
			ClipboardUtil.Clear();
		}

		// ── Timer ─────────────────────────────────────────────────────────────

		private void OnTick(object sender, EventArgs e)
		{
			m_secondsLeft--;
			if(m_secondsLeft <= 0)
			{
				m_timer.Stop();
				m_secondsLeft = 0;
				ClipboardUtil.Clear();
			}
		}

		// ── IDisposable ───────────────────────────────────────────────────────

		public void Dispose()
		{
			if(m_disposed) return;
			m_disposed = true;
			m_timer.Stop();
			m_timer.Dispose();
		}
	}
}
