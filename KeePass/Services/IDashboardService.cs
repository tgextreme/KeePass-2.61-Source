// IDashboardService + DashboardService — Dashboard de seguridad

using System.Collections.Generic;
using KeePassLib;

namespace KeePass.Services
{
	/// <summary>Snapshot of database health metrics for the security dashboard.</summary>
	public sealed class DashboardMetrics
	{
		public int TotalEntries        { get; set; }
		public int ExpiredEntries      { get; set; }
		public int ExpiringIn14Days    { get; set; }
		public int WeakPasswords       { get; set; }
		public int DuplicatePasswords  { get; set; }
		public int PwnedPasswords      { get; set; } // filled lazily (requires network)
		public int SecurityScore       { get; set; } // 0-100
		public IList<PwEntry> TopRisks { get; set; } // up to 5 highest-risk entries
	}

	/// <summary>Aggregates metrics from multiple services for the security dashboard.</summary>
	public interface IDashboardService
	{
		/// <summary>Returns a snapshot of database health. Does NOT perform network calls.</summary>
		DashboardMetrics GetMetrics(PwDatabase db);
	}
}
