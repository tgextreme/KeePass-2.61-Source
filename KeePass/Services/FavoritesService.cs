/*
  KeePass Modern Vibe — Favorites Service (F2)
  Stores the starred/favorite state in PwEntry.CustomData
  under the key "KPMVibe.Starred". No modification to KeePassLib.
*/

using System.Collections.Generic;

using KeePassLib;
using KeePassLib.Collections;

namespace KeePass.Services
{
	/// <summary>
	/// Manages the "Favorite / Starred" state of entries.
	/// State is persisted in <c>PwEntry.CustomData["KPMVibe.Starred"]</c>.
	/// </summary>
	public static class FavoritesService
	{
		public const string CustomDataKey = "KPMVibe.Starred";
		public const string StarredValue   = "1";

		// ── Query ────────────────────────────────────────────────────

		public static bool IsFavorite(PwEntry pe)
		{
			if(pe == null) return false;
			return (pe.CustomData.Get(CustomDataKey) == StarredValue);
		}

		// ── Toggle ───────────────────────────────────────────────────

		/// <summary>Toggles the favorite state and marks the entry as modified.</summary>
		public static void Toggle(PwEntry pe)
		{
			if(pe == null) return;

			if(IsFavorite(pe))
				pe.CustomData.Remove(CustomDataKey);
			else
				pe.CustomData.Set(CustomDataKey, StarredValue, null);

			pe.Touch(true); // update LastModificationTime
		}

		// ── Bulk query ───────────────────────────────────────────────

		public static List<PwEntry> GetAll(PwDatabase db)
		{
			var list = new List<PwEntry>();
			if(db == null || !db.IsOpen) return list;

			foreach(PwEntry pe in db.RootGroup.GetEntries(true))
			{
				if(IsFavorite(pe)) list.Add(pe);
			}
			return list;
		}
	}
}
