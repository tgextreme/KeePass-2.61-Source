/*
  KeePass Modern Vibe — Favorite Column Provider (F2)
  Shows a star symbol in a custom "Favorito" column.
  Register via: Program.ColumnProviderPool.Add(new FavoriteColumnProvider());
  The user can then enable the column from View > Configure Columns.
*/

using System.Windows.Forms;

using KeePass.Services;

using KeePassLib;

namespace KeePass.UI
{
	/// <summary>
	/// Adds a "Favorito" column to the entry list that shows ★ for starred entries.
	/// </summary>
	public sealed class FavoriteColumnProvider : ColumnProvider
	{
		private static readonly string[] m_colNames = new string[] { "Favorito" };

		public override string[] ColumnNames
		{
			get { return m_colNames; }
		}

		public override HorizontalAlignment TextAlign
		{
			get { return HorizontalAlignment.Center; }
		}

		public override string GetCellData(string strColumnName, PwEntry pe)
		{
			return FavoritesService.IsFavorite(pe) ? "\u2605" : string.Empty; // ★ or empty
		}
	}
}
