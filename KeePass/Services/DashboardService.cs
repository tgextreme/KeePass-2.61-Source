// DashboardService — Implementación de IDashboardService

using System.Collections.Generic;
using KeePassLib;

namespace KeePass.Services
{
	/// <summary>
	/// Aggregates <see cref="ExpiryService"/>, <see cref="PasswordAnalysisService"/>
	/// and a simple entry count to produce a <see cref="DashboardMetrics"/> snapshot.
	/// No network calls are made; HIBP data must be injected separately.
	/// </summary>
	public sealed class DashboardService : IDashboardService
	{
		private readonly IPasswordAnalysisService m_analysis;

		public DashboardService() : this(new PasswordAnalysisService()) { }

		public DashboardService(IPasswordAnalysisService analysis)
		{
			m_analysis = analysis;
		}

		public DashboardMetrics GetMetrics(PwDatabase db)
		{
			if(db == null || !db.IsOpen)
				return new DashboardMetrics
				{
					TopRisks = new List<PwEntry>()
				};

			var expired    = ExpiryService.GetExpiredEntries(db);
			var expireSoon = ExpiryService.GetExpiringSoon(db, 14);
			var report     = m_analysis.GetReport(db);

			// Collect top-risk entries (expired first, then weak)
			var topRisks = new List<PwEntry>();
			foreach(var item in expired)
			{
				if(topRisks.Count >= 5) break;
				topRisks.Add(item.Entry);
			}
			foreach(PwEntry pe in report.WeakEntries)
			{
				if(topRisks.Count >= 5) break;
				if(!topRisks.Contains(pe)) topRisks.Add(pe);
			}

			return new DashboardMetrics
			{
				TotalEntries       = report.TotalEntries,
				ExpiredEntries     = expired.Count,
				ExpiringIn14Days   = expireSoon.Count,
				WeakPasswords      = report.WeakEntries.Count,
				DuplicatePasswords = report.DuplicateGroups.Count,
				PwnedPasswords     = 0, // requires async HIBP check
				SecurityScore      = report.Score,
				TopRisks           = topRisks
			};
		}
	}
}
