// RecentEntriesNotifyMenu.cs
// F14 — Menús más Accesibles y Reorganizados
// Submenú en la bandeja con las últimas N entradas usadas.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Utility;

namespace KeePass.UI.Accessibility
{
	/// <summary>
	/// Construye el submenú "Últimas usadas" para el menú de bandeja.
	/// Lee las entradas con mayor UsageCount / LastAccessTime del DB activo.
	/// </summary>
	public static class RecentEntriesNotifyMenu
	{
		private const string F14Tag = "F14_RecentEntry";
		private const int MaxRecent = 5;

		/// <summary>
		/// Inserta (o actualiza) el submenú de entradas recientes en el menú de bandeja.
		/// Llamar cada vez que se abra el menú contextual del tray.
		/// </summary>
		public static void EnrichTrayMenu(ContextMenuStrip ctxTray, MainForm mf)
		{
			if(ctxTray == null || mf == null) return;

			// Eliminar items F14 anteriores
			for(int i = ctxTray.Items.Count - 1; i >= 0; i--)
			{
				if(F14Tag.Equals(ctxTray.Items[i].Tag as string, StringComparison.Ordinal))
					ctxTray.Items.RemoveAt(i);
			}

			if(!mf.IsAtLeastOneFileOpen()) return;
			if(mf.IsFileLocked(null)) return;

			var db = mf.ActiveDatabase;
			if(db == null || !db.IsOpen) return;

			List<PwEntry> recent = GetRecentEntries(db, MaxRecent);
			if(recent.Count == 0) return;

			// Submenú "Últimas usadas ▶"
			var tsmiRecent = new ToolStripMenuItem("\U0001F4CB  Últimas usadas")
			{
				Tag = F14Tag
			};

			foreach(PwEntry pe in recent)
			{
				PwEntry captured = pe;
				string title = pe.Strings.ReadSafe(KeePassLib.PwDefs.TitleField);
				string user  = pe.Strings.ReadSafe(KeePassLib.PwDefs.UserNameField);
				string label = string.IsNullOrEmpty(title) ? user : title;
				if(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(title))
					label = title + " \u2014 " + user;

				var tsmi = new ToolStripMenuItem(label, null,
					(s, e) => CopyPassword(captured, mf));
				tsmiRecent.DropDownItems.Add(tsmi);
			}

			// Insertar después del separador F16 (índice 3) o al final si no hay
			// Para simplificar, insertar en posición 3 si el menú tiene suficientes items
			int insertAt = Math.Min(3, ctxTray.Items.Count);
			ctxTray.Items.Insert(insertAt, tsmiRecent);

			// Separador después del submenú
			var sep = new ToolStripSeparator { Tag = F14Tag };
			ctxTray.Items.Insert(insertAt + 1, sep);
		}

		private static List<PwEntry> GetRecentEntries(PwDatabase db, int count)
		{
			List<PwEntry> all = new List<PwEntry>();
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null,
				pe => { all.Add(pe); return true; });

			// Ordenar por LastAccessTime descendente (más reciente primero)
			all.Sort((a, b) => b.LastAccessTime.CompareTo(a.LastAccessTime));

			int n = Math.Min(count, all.Count);
			return all.GetRange(0, n);
		}

		private static void CopyPassword(PwEntry pe, MainForm mf)
		{
			if(pe == null || mf == null) return;
			try
			{
				var ps = pe.Strings.GetSafe(KeePassLib.PwDefs.PasswordField);
				ClipboardUtil.CopyAndMinimize(ps, true, mf,
					pe, mf.ActiveDatabase);
			}
			catch(Exception) { }
		}
	}
}
