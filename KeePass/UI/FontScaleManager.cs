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

// F15-G — Font Scale Manager
// Handles Ctrl+MouseWheel and Ctrl+Plus/Minus zoom, persisting the scale
// factor in Program.Config.Layout.FontScale across sessions.

using System;
using System.Drawing;
using System.Windows.Forms;

using KeePass.App;
using KeePass.App.Configuration;
using KeePass.Forms;

namespace KeePass.UI
{
	/// <summary>
	/// F15-G: Manages the font magnification level for the main entry list,
	/// group tree and entry viewer.
	/// </summary>
	internal static class FontScaleManager
	{
		private const float ZoomStep = 0.1f;
		private const float ZoomMin  = 0.5f;
		private const float ZoomMax  = 3.0f;

		private static MainForm m_mf;
		private static WheelFilter m_filter;

		// ----------------------------------------------------------------
		// Public API
		// ----------------------------------------------------------------

		/// <summary>
		/// Install the Ctrl+Wheel message filter and apply the stored
		/// font scale.  Call once from MainForm.OnFormLoad.
		/// </summary>
		public static void Attach(MainForm mf)
		{
			if(mf == null) return;
			if(m_mf != null) Detach(); // re-entrant safety

			m_mf = mf;
			m_filter = new WheelFilter();
			Application.AddMessageFilter(m_filter);

			// Restore persisted scale on startup
			float s = Program.Config.Layout.FontScale;
			if(Math.Abs(s - 1.0f) > 0.005f)
				m_mf.SetFontScale(s);
		}

		/// <summary>
		/// Remove the message filter.  Call from MainForm.OnFormClosed.
		/// </summary>
		public static void Detach()
		{
			if(m_filter != null)
			{
				Application.RemoveMessageFilter(m_filter);
				m_filter = null;
			}
			m_mf = null;
		}

		/// <summary>Increase font scale by one step (Ctrl++ / Ctrl+Wheel up).</summary>
		public static void ZoomIn()  { AdjustScale(+ZoomStep); }

		/// <summary>Decrease font scale by one step (Ctrl+- / Ctrl+Wheel down).</summary>
		public static void ZoomOut() { AdjustScale(-ZoomStep); }

		/// <summary>Reset font scale to 1.0 (system default).</summary>
		public static void ResetZoom() { SetScale(1.0f); }

		// ----------------------------------------------------------------
		// Private helpers
		// ----------------------------------------------------------------

		private static void AdjustScale(float delta)
		{
			SetScale(Program.Config.Layout.FontScale + delta);
		}

		private static void SetScale(float s)
		{
			s = Math.Max(ZoomMin, Math.Min(ZoomMax, s));
			// Round to one decimal place to avoid float drift
			s = (float)Math.Round((double)s, 1);

			Program.Config.Layout.FontScale = s;

			if(m_mf != null) m_mf.SetFontScale(s);
		}

		// ----------------------------------------------------------------
		// Inner message filter — intercepts WM_MOUSEWHEEL when Ctrl held
		// ----------------------------------------------------------------

		private sealed class WheelFilter : IMessageFilter
		{
			private const int WM_MOUSEWHEEL = 0x020A;

			public bool PreFilterMessage(ref Message m)
			{
				if(m.Msg != WM_MOUSEWHEEL) return false;
				if((Control.ModifierKeys & Keys.Control) == 0) return false;

				// High word of wParam is the signed wheel delta
				int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
				if(delta > 0) ZoomIn();
				else if(delta < 0) ZoomOut();

				return true; // Do not pass on to the focused control
			}
		}
	}
}
