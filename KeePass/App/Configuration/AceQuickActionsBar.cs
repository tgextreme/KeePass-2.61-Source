// AceQuickActionsBar.cs
// F14 — Menús más Accesibles y Reorganizados
// Configuración de la barra de acciones rápidas.

using System.Collections.Generic;
using System.ComponentModel;

namespace KeePass.App.Configuration
{
	public enum QuickActionsBarPosition
	{
		Top = 0,
		Bottom = 1
	}

	public sealed class AceQuickActionsBar
	{
		private bool m_bVisible = true;
		[DefaultValue(true)]
		public bool Visible
		{
			get { return m_bVisible; }
			set { m_bVisible = value; }
		}

		private QuickActionsBarPosition m_pos = QuickActionsBarPosition.Top;
		public QuickActionsBarPosition Position
		{
			get { return m_pos; }
			set { m_pos = value; }
		}

		// IDs de los botones visibles, en orden.
		// Valores posibles: "NewEntry","Search","CopyPW","CopyUser","OpenURL","Favorite","Lock","HIBP"
		private List<string> m_lButtonIds = null;
		public List<string> ButtonIds
		{
			get
			{
				if(m_lButtonIds == null)
					m_lButtonIds = new List<string>(new string[] {
						"NewEntry", "Search", "CopyPW", "CopyUser", "OpenURL", "Favorite", "Lock"
					});
				return m_lButtonIds;
			}
			set { m_lButtonIds = value; }
		}
	}
}
