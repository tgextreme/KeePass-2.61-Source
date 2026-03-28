// PasswordAnalysisService — Análisis de contraseñas (IPasswordAnalysisService)

using System;
using System.Collections.Generic;

using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Delegates;

namespace KeePass.Services
{
	/// <summary>
	/// Default implementation of <see cref="IPasswordAnalysisService"/>.
	/// Uses <c>QualityEstimation</c> from KeePassLib for quality scoring.
	/// Passwords are compared via their SHA-256 hash to avoid holding them in
	/// plain text any longer than needed.
	/// </summary>
	public sealed class PasswordAnalysisService : IPasswordAnalysisService
	{
		private const uint DefaultWeakThreshold = 50; // bits

		// ── IPasswordAnalysisService ──────────────────────────────────────────

		public IList<PwEntry> GetWeakEntries(PwDatabase db, uint weakThreshold)
		{
			var result = new List<PwEntry>();
			if(db == null || !db.IsOpen) return result;

			EntryHandler eh = delegate(PwEntry pe)
			{
				string pwd = pe.Strings.ReadSafe(PwDefs.PasswordField);
				if(string.IsNullOrEmpty(pwd)) return true;
				uint quality = QualityEstimation.EstimatePasswordBits(pwd.ToCharArray());
				if(quality < weakThreshold) result.Add(pe);
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return result;
		}

		public IList<IList<PwEntry>> GetDuplicateGroups(PwDatabase db)
		{
			if(db == null || !db.IsOpen) return new List<IList<PwEntry>>();

			// Group entries by password hash
			var buckets = new Dictionary<string, List<PwEntry>>(
				StringComparer.Ordinal);

			EntryHandler eh = delegate(PwEntry pe)
			{
				string pwd = pe.Strings.ReadSafe(PwDefs.PasswordField);
				if(string.IsNullOrEmpty(pwd)) return true;
				string hash = ComputeHash(pwd);
				List<PwEntry> bucket;
				if(!buckets.TryGetValue(hash, out bucket))
				{
					bucket = new List<PwEntry>();
					buckets[hash] = bucket;
				}
				bucket.Add(pe);
				return true;
			};
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);

			var result = new List<IList<PwEntry>>();
			foreach(var kvp in buckets)
			{
				if(kvp.Value.Count > 1) result.Add(kvp.Value);
			}
			return result;
		}

		public SecurityReport GetReport(PwDatabase db)
		{
			if(db == null || !db.IsOpen)
				return new SecurityReport
				{
					TotalEntries   = 0,
					WeakEntries    = new List<PwEntry>(),
					DuplicateGroups= new List<IList<PwEntry>>(),
					Score          = 0
				};

			IList<PwEntry>         weak = GetWeakEntries(db, DefaultWeakThreshold);
			IList<IList<PwEntry>>  dups = GetDuplicateGroups(db);

			int total = 0;
			EntryHandler count = delegate(PwEntry pe) { total++; return true; };
			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, count);

			// Simple score: 100 minus penalty for weak/duplicate entries
			int penalties = weak.Count * 3 + dups.Count * 5;
			int score = Math.Max(0, Math.Min(100, 100 - (total > 0
				? (penalties * 100 / total)
				: penalties)));

			return new SecurityReport
			{
				TotalEntries    = total,
				WeakEntries     = weak,
				DuplicateGroups = dups,
				Score           = score
			};
		}

		// ── Helpers ───────────────────────────────────────────────────────────

		private static string ComputeHash(string password)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
			using(var sha = System.Security.Cryptography.SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(bytes);
				return BitConverter.ToString(hash).Replace("-", string.Empty);
			}
		}
	}
}
