// F4 — Alertas de Expiración
// Timer que comprueba expiraciones cada hora y muestra un globo de notificación.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

using KeePass.Services;
using KeePass.UI.Background;
using KeePass.Forms;
using KeePassLib;

namespace KeePass.Infrastructure.Security
{
	/// <summary>
	/// Comprueba cada hora si hay entradas próximas a expirar o ya expiradas
	/// y muestra un globo de notificación en la bandeja del sistema.
	/// </summary>
	public sealed class ExpiryWatcher : IDisposable
	{
		private readonly Timer m_timer;
		private MainForm m_mainForm = null;
		private bool m_disposed = false;

		private const int CheckIntervalMs = 60 * 60 * 1000; // 1 hora

		public ExpiryWatcher()
		{
			m_timer = new Timer();
			m_timer.Interval = CheckIntervalMs;
			m_timer.Tick += OnTimerTick;
		}

		/// <summary>Inicia el temporizador horario.</summary>
		public void Start(MainForm mf)
		{
			m_mainForm = mf;
			m_timer.Start();
		}

		/// <summary>Detiene el temporizador.</summary>
		public void Stop()
		{
			m_timer.Stop();
			m_mainForm = null;
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if(m_mainForm == null) return;

			PwDatabase db = m_mainForm.ActiveDatabase;
			if(db == null || !db.IsOpen) return;

			int warningDays = Program.Config.Security.ExpiryWarningDays;

			List<ExpiryItem> expired  = ExpiryService.GetExpiredEntries(db);
			List<ExpiryItem> expiring = ExpiryService.GetExpiringSoon(db, warningDays);

			int totalCount = expired.Count + expiring.Count;
			if(totalCount == 0) return;

			string strTitle = "KeePass — Alertas de Expiración";
			string strMsg;

			if(expired.Count > 0 && expiring.Count > 0)
				strMsg = expired.Count + " entrada(s) expirada(s), "
					+ expiring.Count + " próxima(s) a expirar.";
			else if(expired.Count > 0)
				strMsg = expired.Count + " entrada(s) ha(n) expirado.";
			else
				strMsg = expiring.Count + " entrada(s) expiran en menos de "
					+ warningDays + " días.";

			TrayIconController.ShowBalloon(strTitle, strMsg, ToolTipIcon.Warning, 6000);
		}

		public void Dispose()
		{
			if(!m_disposed)
			{
				m_timer.Dispose();
				m_disposed = true;
			}
		}
	}
}
