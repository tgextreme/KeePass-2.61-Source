// SearchService — Búsqueda centralizada de entradas (ISearchService)

using System;
using System.Collections.Generic;

using KeePassLib;
using KeePassLib.Delegates;

namespace KeePass.Services
{
	/// <summary>
	/// Default implementation of <see cref="ISearchService"/>.
	/// All searches are synchronous and operate directly on <c>PwDatabase</c>
	/// without caching, so they are always up-to-date after any modification.
	/// </summary>
	public sealed class SearchService : ISearchService
	{
		// ── ISearchService ────────────────────────────────────────────────────

		public IList<PwEntry> Search(PwDatabase db, string query)
		{
			if(db == null || !db.IsOpen || string.IsNullOrEmpty(query))
				return new List<PwEntry>();

			string q = query.ToLowerInvariant();
			var results = new List<PwEntry>();

			EntryHandler eh = delegate(PwEntry pe)
			{
				if(EntryMatchesText(pe, q)) results.Add(pe);
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return results;
		}

		public IList<PwEntry> SearchByDomain(PwDatabase db, string domain)
		{
			if(db == null || !db.IsOpen || string.IsNullOrEmpty(domain))
				return new List<PwEntry>();

			string domLower = ExtractHost(domain).ToLowerInvariant();
			var results = new List<PwEntry>();

			EntryHandler eh = delegate(PwEntry pe)
			{
				string url = pe.Strings.ReadSafe(PwDefs.UrlField);
				if(!string.IsNullOrEmpty(url))
				{
					string host = ExtractHost(url).ToLowerInvariant();
					if(host == domLower || host.EndsWith("." + domLower))
						results.Add(pe);
				}
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return results;
		}

		public IList<PwEntry> GetRecent(PwDatabase db, int count)
		{
			if(db == null || !db.IsOpen || count <= 0) return new List<PwEntry>();

			var all = new List<PwEntry>();
			EntryHandler eh = delegate(PwEntry pe) { all.Add(pe); return true; };
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);

			// Sort descending by last-access time
			all.Sort((a, b) => b.LastAccessTime.CompareTo(a.LastAccessTime));

			return all.GetRange(0, Math.Min(count, all.Count));
		}

		// ── Helpers ───────────────────────────────────────────────────────────

		private static bool EntryMatchesText(PwEntry pe, string queryLower)
		{
			return ContainsLower(pe.Strings.ReadSafe(PwDefs.TitleField),    queryLower)
			    || ContainsLower(pe.Strings.ReadSafe(PwDefs.UserNameField), queryLower)
			    || ContainsLower(pe.Strings.ReadSafe(PwDefs.UrlField),      queryLower)
			    || ContainsLower(pe.Strings.ReadSafe(PwDefs.NotesField),    queryLower);
		}

		private static bool ContainsLower(string source, string queryLower)
		{
			if(string.IsNullOrEmpty(source)) return false;
			return source.ToLowerInvariant().Contains(queryLower);
		}

		/// <summary>Extracts the host from a URL string; returns the original string on failure.</summary>
		private static string ExtractHost(string url)
		{
			if(string.IsNullOrEmpty(url)) return url ?? string.Empty;
			try
			{
				if(!url.Contains("://")) url = "https://" + url;
				return new Uri(url).Host;
			}
			catch { return url; }
		}
	}
}
