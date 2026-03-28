// IPasswordAnalysisService — Análisis de contraseñas débiles y reutilizadas

using System.Collections.Generic;
using KeePassLib;

namespace KeePass.Services
{
	/// <summary>Summary report returned by <see cref="IPasswordAnalysisService.GetReport"/>.</summary>
	public sealed class SecurityReport
	{
		/// <summary>Number of entries scanned.</summary>
		public int TotalEntries { get; set; }
		/// <summary>Entries with estimated quality below the weak threshold.</summary>
		public IList<PwEntry> WeakEntries { get; set; }
		/// <summary>Groups of entries sharing the same password.</summary>
		public IList<IList<PwEntry>> DuplicateGroups { get; set; }
		/// <summary>Overall "score" 0–100 (higher is better).</summary>
		public int Score { get; set; }
	}

	/// <summary>
	/// Analyses a <see cref="PwDatabase"/> for weak and reused passwords.
	/// Passwords are never stored or logged in plain text.
	/// </summary>
	public interface IPasswordAnalysisService
	{
		/// <summary>Returns entries whose estimated quality is below
		/// <paramref name="weakThreshold"/> bits (0–128).</summary>
		IList<PwEntry> GetWeakEntries(PwDatabase db, uint weakThreshold);

		/// <summary>Returns groups of entries that share the same password.</summary>
		IList<IList<PwEntry>> GetDuplicateGroups(PwDatabase db);

		/// <summary>Generates a full security report for the database.</summary>
		SecurityReport GetReport(PwDatabase db);
	}
}
