// F4 — Alertas de Expiración
// Servicio que compara PwEntry.ExpiryTime con DateTime.UtcNow
// sin modificar KeePassLib.

using System;
using System.Collections.Generic;
using KeePassLib;
using KeePassLib.Delegates;

namespace KeePass.Services
{
	/// <summary>Información de una entrada con vencimiento.</summary>
	public sealed class ExpiryItem
	{
		public PwEntry Entry { get; private set; }
		/// <summary>Días que faltan para expirar (negativo = ya expiró).</summary>
		public int DaysRemaining { get; private set; }

		public ExpiryItem(PwEntry entry, int daysRemaining)
		{
			Entry = entry;
			DaysRemaining = daysRemaining;
		}
	}

	/// <summary>Consultas de expiración sobre una base de datos KeePass.</summary>
	public static class ExpiryService
	{
		/// <summary>Devuelve todas las entradas que ya han expirado.</summary>
		public static List<ExpiryItem> GetExpiredEntries(PwDatabase db)
		{
			if(db == null || !db.IsOpen || db.RootGroup == null)
				return new List<ExpiryItem>();

			DateTime utcNow = DateTime.UtcNow;
			List<ExpiryItem> results = new List<ExpiryItem>();

			EntryHandler eh = delegate(PwEntry pe)
			{
				if(pe.Expires && pe.ExpiryTime <= utcNow)
				{
					TimeSpan diff = utcNow - pe.ExpiryTime;
					results.Add(new ExpiryItem(pe, -(int)diff.TotalDays));
				}
				return true;
			};

			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return results;
		}

		/// <summary>Devuelve entradas que expiran dentro de <paramref name="days"/> días
		/// (excluyendo las ya expiradas).</summary>
		public static List<ExpiryItem> GetExpiringSoon(PwDatabase db, int days)
		{
			if(db == null || !db.IsOpen || db.RootGroup == null || days <= 0)
				return new List<ExpiryItem>();

			DateTime utcNow   = DateTime.UtcNow;
			DateTime utcLimit = utcNow.AddDays(days);
			List<ExpiryItem> results = new List<ExpiryItem>();

			EntryHandler eh = delegate(PwEntry pe)
			{
				if(pe.Expires && pe.ExpiryTime > utcNow && pe.ExpiryTime <= utcLimit)
				{
					TimeSpan diff = pe.ExpiryTime - utcNow;
					results.Add(new ExpiryItem(pe, (int)diff.TotalDays));
				}
				return true;
			};

			db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
			return results;
		}
	}
}
