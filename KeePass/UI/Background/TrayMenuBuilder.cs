using System;
using System.Collections.Generic;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.UI.Accessibility;

namespace KeePass.UI.Background
{
	/// <summary>
	/// Builds the F16 section at the top of the existing tray context menu:
	///   [DB status label]
	///   [🔍 Buscar credencial...]
	///   [separator]
	///   ...existing items...
	/// Items are tagged so they can be removed and re-inserted on each open.
	/// </summary>
	public static class TrayMenuBuilder
	{
		private const string F16Tag = "F16_TrayItem";

		public static void EnrichTrayMenu(ContextMenuStrip ctxTray, MainForm mf)
		{
			if(ctxTray == null || mf == null) return;
			if(!Program.Config.BackgroundMode.RunInBackground) return;

			// Remove old F16 items
			RemoveF16Items(ctxTray);

			// Build status text
			string status;
			if(!mf.IsAtLeastOneFileOpen())
				status = "\u26AB  Sin base de datos";
			else if(mf.IsFileLocked(null))
				status = "\U0001F512  Base de datos bloqueada";
			else
			{
				var db = mf.ActiveDatabase;
				string dbName = (db != null && db.IsOpen && !string.IsNullOrEmpty(db.Name))
					? db.Name : "Base de datos";
				status = "\U0001F7E2  " + dbName + " \u2014 Abierta";
			}

			var items = new List<ToolStripItem>();

			// Status label (non-clickable)
			var lblStatus = new ToolStripMenuItem(status) { Tag = F16Tag, Enabled = false };
			items.Add(lblStatus);

			// Search item (only when DB is open and unlocked)
			bool canSearch = mf.IsAtLeastOneFileOpen() && !mf.IsFileLocked(null);
			var itemSearch = new ToolStripMenuItem(
				"\U0001F50D  Buscar credencial...\tCtrl+Alt+K",
				null,
				(s, e) => MiniSearchPopup.Toggle())
			{
				Tag     = F16Tag,
				Enabled = canSearch
			};
			items.Add(itemSearch);

			// Separator
			var sep = new ToolStripSeparator { Tag = F16Tag };
			items.Add(sep);

			// Insert at position 0 (top of menu)
			for(int i = items.Count - 1; i >= 0; i--)
				ctxTray.Items.Insert(0, items[i]);

			// F14 — entradas recientes en el tray
			RecentEntriesNotifyMenu.EnrichTrayMenu(ctxTray, mf);
		}

		private static void RemoveF16Items(ContextMenuStrip ctxTray)
		{
			for(int i = ctxTray.Items.Count - 1; i >= 0; i--)
			{
				if(F16Tag.Equals(ctxTray.Items[i].Tag as string, StringComparison.Ordinal))
					ctxTray.Items.RemoveAt(i);
			}
		}
	}
}
