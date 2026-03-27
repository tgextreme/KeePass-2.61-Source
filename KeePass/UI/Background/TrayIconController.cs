using System;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.UI;

namespace KeePass.UI.Background
{
	/// <summary>
	/// Manages dynamic tray-icon tooltip text based on database state.
	/// Wraps the existing NotifyIconEx that is already set up in MainForm.
	/// </summary>
	public static class TrayIconController
	{
		private static NotifyIconEx g_ntf = null;

		public static void Initialize(NotifyIconEx ntf)
		{
			g_ntf = ntf;
		}

		/// <summary>
		/// Called every time the tray context menu opens (via OnCtxTrayOpening)
		/// and whenever the DB state changes. Updates the tooltip text.
		/// </summary>
		public static void UpdateState(MainForm mf)
		{
			if(g_ntf == null || mf == null) return;

			string tip;

			if(!mf.IsAtLeastOneFileOpen())
			{
				tip = "KeePass — Sin base de datos";
			}
			else if(mf.IsFileLocked(null))
			{
				tip = "KeePass — Base de datos bloqueada \U0001F512";
			}
			else
			{
				var db = mf.ActiveDatabase;
				if(db != null && db.IsOpen)
				{
					long count = db.RootGroup.GetEntriesCount(true);
					string name = db.Name;
					if(string.IsNullOrEmpty(name)) name = "Base de datos";
					tip = string.Format("KeePass \u2014 {0} ({1} entradas)", name, count);
				}
				else
					tip = "KeePass";
			}

			// The underlying WinForms NotifyIcon has a tip limit of 63 chars
			if(tip.Length > 63) tip = tip.Substring(0, 60) + "...";
			g_ntf.Text = tip;
		}

		/// <summary>Muestra un globo de notificación en la bandeja del sistema. F4.</summary>
		public static void ShowBalloon(string title, string text,
			ToolTipIcon icon, int timeoutMs)
		{
			if(g_ntf == null) return;
			try { g_ntf.NotifyIcon.ShowBalloonTip(timeoutMs, title, text, icon); }
			catch(Exception) { }
		}
	}
}
