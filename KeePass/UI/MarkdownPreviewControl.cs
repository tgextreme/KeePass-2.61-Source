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

// F12 — Control de previsualización Markdown.
// Envuelve WebBrowser para mostrar Markdown renderizado como HTML.

using System;
using System.Windows.Forms;

using KeePass.Infrastructure;

namespace KeePass.UI
{
	/// <summary>
	/// F12: Control de previsualización Markdown embebible.
	/// Llama a Render(markdown) para mostrar el contenido convertido a HTML.
	/// </summary>
	internal sealed class MarkdownPreviewControl : UserControl
	{
		private readonly WebBrowser m_browser;

		public MarkdownPreviewControl()
		{
			m_browser = new WebBrowser
			{
				Dock                             = DockStyle.Fill,
				ScrollBarsEnabled                = true,
				IsWebBrowserContextMenuEnabled   = false,
				AllowWebBrowserDrop              = false,
			};

			this.Controls.Add(m_browser);
		}

		/// <summary>
		/// Renderiza el texto Markdown y lo muestra en el control.
		/// </summary>
		/// <param name="markdown">Texto en formato Markdown.</param>
		public void Render(string markdown)
		{
			string body  = MarkdownRenderer.ToHtml(markdown ?? string.Empty);
			string style = "<style>"
				+ "body{font-family:Segoe UI,Arial,sans-serif;font-size:9pt;"
				+ "background:#ffffff;color:#1a1a1a;margin:6px;word-break:break-word;}"
				+ "h1,h2,h3,h4{margin:6px 0 3px 0;}"
				+ "p{margin:2px 0;}"
				+ "code{background:#f0f0f0;padding:1px 4px;border-radius:2px;font-family:Consolas,monospace;}"
				+ "ul,ol{margin:2px 0 2px 18px;}"
				+ "hr{border:none;border-top:1px solid #cccccc;}"
				+ "a{color:#0066cc;}"
				+ "</style>";

			string full = "<!DOCTYPE html><html><head><meta charset='utf-8'/>"
				+ style + "</head><body>"
				+ body
				+ "</body></html>";

			m_browser.DocumentText = full;
		}

		/// <summary>Limpia el contenido del visor.</summary>
		public void Clear()
		{
			m_browser.DocumentText = "<html><body></body></html>";
		}
	}
}
