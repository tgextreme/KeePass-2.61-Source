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

// F12 — Markdown Renderer (sin dependencias externas).
// Convierte un subconjunto de Markdown a HTML para su previsualización.

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace KeePass.Infrastructure
{
	/// <summary>
	/// F12: Conversor de Markdown a HTML.
	/// Soporta: encabezados (#), negrita (**), cursiva (*), código inline (`),
	/// listas (-/*), separadores (---), tachado (~~), enlaces ([text](url)).
	/// Sin dependencias externas — solo BCL.
	/// </summary>
	internal static class MarkdownRenderer
	{
		/// <summary>Convierte texto Markdown a un fragmento HTML.</summary>
		public static string ToHtml(string markdown)
		{
			if(string.IsNullOrEmpty(markdown)) return string.Empty;

			// Normalizar saltos de línea
			string[] lines = markdown.Replace("\r\n", "\n")
			                         .Replace("\r",   "\n")
			                         .Split('\n');

			StringBuilder html  = new StringBuilder(markdown.Length * 2);
			bool          inUl  = false;
			bool          inOl  = false;
			int           olIdx = 1;

			foreach(string rawLine in lines)
			{
				string line = rawLine;

				bool isBullet   = IsBulletLine(line);
				bool isOrdered  = IsOrderedLine(line);

				// Cerrar listas abiertas si la línea actual no pertenece a ellas
				if(inUl && !isBullet)   { html.AppendLine("</ul>"); inUl  = false; }
				if(inOl && !isOrdered)  { html.AppendLine("</ol>"); inOl  = false; olIdx = 1; }

				// ── Separador horizontal ─────────────────────────────────────────
				if(Regex.IsMatch(line, @"^(-{3,}|\*{3,}|_{3,})\s*$"))
				{ html.AppendLine("<hr/>"); continue; }

				// ── Encabezados  # … ######  ────────────────────────────────────
				Match mH = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
				if(mH.Success)
				{
					int    lvl     = mH.Groups[1].Length;
					string content = InlineFormat(mH.Groups[2].Value);
					html.AppendLine(string.Format("<h{0}>{1}</h{0}>", lvl, content));
					continue;
				}

				// ── Bullet list  - item  o  * item  ─────────────────────────────
				if(isBullet)
				{
					if(!inUl) { html.AppendLine("<ul>"); inUl = true; }
					string content = InlineFormat(StripBullet(line));
					html.AppendLine("<li>" + content + "</li>");
					continue;
				}

				// ── Ordered list  1. item  ───────────────────────────────────────
				if(isOrdered)
				{
					if(!inOl) { html.AppendLine("<ol>"); inOl = true; olIdx = 1; }
					string content = InlineFormat(StripOrdered(line));
					html.AppendLine("<li>" + content + "</li>");
					olIdx++;
					continue;
				}

				// ── Línea vacía ──────────────────────────────────────────────────
				if(string.IsNullOrWhiteSpace(line))
				{ html.AppendLine("<br/>"); continue; }

				// ── Párrafo normal ───────────────────────────────────────────────
				html.AppendLine("<p>" + InlineFormat(line) + "</p>");
			}

			if(inUl) html.AppendLine("</ul>");
			if(inOl) html.AppendLine("</ol>");

			return html.ToString();
		}

		// ── Helpers de detección ─────────────────────────────────────────────────

		private static bool IsBulletLine(string line)
		{
			string t = line.TrimStart();
			return (t.StartsWith("- ") || t.StartsWith("* "));
		}

		private static bool IsOrderedLine(string line)
		{
			return Regex.IsMatch(line.TrimStart(), @"^\d+\.\s");
		}

		private static string StripBullet(string line)
		{
			string t = line.TrimStart();
			return t.Substring(2); // Remove "- " or "* "
		}

		private static string StripOrdered(string line)
		{
			return Regex.Replace(line.TrimStart(), @"^\d+\.\s+", string.Empty);
		}

		// ── Formato inline ───────────────────────────────────────────────────────

		private static string InlineFormat(string text)
		{
			// 1. Escapar entidades HTML básicas
			text = text.Replace("&", "&amp;")
			           .Replace("<", "&lt;")
			           .Replace(">", "&gt;");

			// 2. Código inline:  `code`
			text = Regex.Replace(text, @"`([^`]+)`", "<code>$1</code>");

			// 3. Negrita:  **text**  o  __text__
			text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
			text = Regex.Replace(text, @"__(.+?)__",     "<strong>$1</strong>");

			// 4. Cursiva:  *text* (sin mezcla con negrita)  o  _text_
			text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<em>$1</em>");
			text = Regex.Replace(text, @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)",       "<em>$1</em>");

			// 5. Tachado:  ~~text~~
			text = Regex.Replace(text, @"~~(.+?)~~", "<del>$1</del>");

			// 6. Enlaces:  [texto](url)
			text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)",
				"<a href=\"$2\">$1</a>");

			return text;
		}
	}
}
