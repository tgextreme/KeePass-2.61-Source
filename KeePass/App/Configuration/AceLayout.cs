/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2026 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

// F15-G — Font Scale + Layout persistente
// Configuration class stored under Program.Config.Layout.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace KeePass.App.Configuration
{
	/// <summary>
	/// F15-G: Persists user layout preferences that are not covered by the
	/// existing AceMainWindow config (e.g. font scale factor).
	/// </summary>
	public sealed class AceLayout
	{
		private float m_fontScale = 1.0f;

		/// <summary>
		/// Font magnification multiplier applied to the list font.
		/// 1.0 = system default; valid range 0.5 (50 %) to 3.0 (300 %).
		/// Adjusted with Ctrl+MouseWheel or Ctrl+Plus / Ctrl+Minus.
		/// </summary>
		[DefaultValue(1.0f)]
		public float FontScale
		{
			get { return m_fontScale; }
			set
			{
				// Clamp without throwing; silently ignore bad XML values
				m_fontScale = Math.Max(0.5f, Math.Min(3.0f, value));
			}
		}
	}
}
