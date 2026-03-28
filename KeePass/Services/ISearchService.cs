// ISearchService — Búsqueda centralizada de entradas

using System.Collections.Generic;
using KeePassLib;

namespace KeePass.Services
{
	/// <summary>
	/// Centralized entry search.  Implementations may build an in-memory index
	/// for performance, but must remain correct after database modifications.
	/// </summary>
	public interface ISearchService
	{
		/// <summary>
		/// Searches all entries in <paramref name="db"/> whose title, username,
		/// URL or notes contain <paramref name="query"/> (case-insensitive).
		/// </summary>
		IList<PwEntry> Search(PwDatabase db, string query);

		/// <summary>
		/// Returns entries whose URL field matches the given domain
		/// (host comparison, ignoring scheme and path).
		/// </summary>
		IList<PwEntry> SearchByDomain(PwDatabase db, string domain);

		/// <summary>Returns the last <paramref name="count"/> accessed entries.</summary>
		IList<PwEntry> GetRecent(PwDatabase db, int count);
	}
}
