using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Infrastructure;

namespace KeePass.Tests.Infrastructure
{
    [TestClass]
    public class MarkdownRendererTests
    {
        // ── null / empty ──────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_NullInput_ReturnsEmpty()
        {
            string result = MarkdownRenderer.ToHtml(null);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ToHtml_EmptyString_ReturnsEmpty()
        {
            string result = MarkdownRenderer.ToHtml(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        // ── encabezados ───────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_H1_ProducesH1Tag()
        {
            string result = MarkdownRenderer.ToHtml("# Título");
            StringAssert.Contains(result, "<h1>");
            StringAssert.Contains(result, "Título");
            StringAssert.Contains(result, "</h1>");
        }

        [TestMethod]
        public void ToHtml_H2_ProducesH2Tag()
        {
            string result = MarkdownRenderer.ToHtml("## Subtítulo");
            StringAssert.Contains(result, "<h2>");
            StringAssert.Contains(result, "</h2>");
        }

        [TestMethod]
        public void ToHtml_H6_ProducesH6Tag()
        {
            string result = MarkdownRenderer.ToHtml("###### Pequeño");
            StringAssert.Contains(result, "<h6>");
            StringAssert.Contains(result, "</h6>");
        }

        // ── negrita / cursiva ─────────────────────────────────────────

        [TestMethod]
        public void ToHtml_Bold_ProducesStrongTag()
        {
            string result = MarkdownRenderer.ToHtml("**negrita**");
            StringAssert.Contains(result, "<strong>");
            StringAssert.Contains(result, "negrita");
            StringAssert.Contains(result, "</strong>");
        }

        [TestMethod]
        public void ToHtml_Italic_ProducesEmTag()
        {
            string result = MarkdownRenderer.ToHtml("*cursiva*");
            StringAssert.Contains(result, "<em>");
            StringAssert.Contains(result, "cursiva");
            StringAssert.Contains(result, "</em>");
        }

        // ── código inline ─────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_InlineCode_ProducesCodeTag()
        {
            string result = MarkdownRenderer.ToHtml("`codigo`");
            StringAssert.Contains(result, "<code>");
            StringAssert.Contains(result, "codigo");
            StringAssert.Contains(result, "</code>");
        }

        // ── listas ────────────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_BulletList_ProducesUlAndLi()
        {
            string result = MarkdownRenderer.ToHtml("- item uno\n- item dos");
            StringAssert.Contains(result, "<ul>");
            StringAssert.Contains(result, "<li>");
            StringAssert.Contains(result, "item uno");
            StringAssert.Contains(result, "item dos");
            StringAssert.Contains(result, "</ul>");
        }

        [TestMethod]
        public void ToHtml_AsteriskBulletList_ProducesUlAndLi()
        {
            string result = MarkdownRenderer.ToHtml("* elemento");
            StringAssert.Contains(result, "<ul>");
            StringAssert.Contains(result, "elemento");
        }

        [TestMethod]
        public void ToHtml_OrderedList_ProducesOlAndLi()
        {
            string result = MarkdownRenderer.ToHtml("1. primero\n2. segundo");
            StringAssert.Contains(result, "<ol>");
            StringAssert.Contains(result, "<li>");
            StringAssert.Contains(result, "primero");
            StringAssert.Contains(result, "segundo");
            StringAssert.Contains(result, "</ol>");
        }

        // ── separador ─────────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_HorizontalRule_ProducesHr()
        {
            string result = MarkdownRenderer.ToHtml("---");
            StringAssert.Contains(result, "<hr/>");
        }

        // ── tachado ───────────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_Strikethrough_ProducesDelTag()
        {
            string result = MarkdownRenderer.ToHtml("~~tachado~~");
            StringAssert.Contains(result, "<del>");
            StringAssert.Contains(result, "tachado");
            StringAssert.Contains(result, "</del>");
        }

        // ── enlace ────────────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_Link_ProducesAnchorTag()
        {
            string result = MarkdownRenderer.ToHtml("[texto](https://example.com)");
            StringAssert.Contains(result, "<a ");
            StringAssert.Contains(result, "href=\"https://example.com\"");
            StringAssert.Contains(result, "texto");
            StringAssert.Contains(result, "</a>");
        }

        // ── párrafo normal ────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_PlainText_ProducesPTag()
        {
            string result = MarkdownRenderer.ToHtml("texto normal");
            StringAssert.Contains(result, "<p>");
            StringAssert.Contains(result, "texto normal");
        }

        // ── combinado ─────────────────────────────────────────────────

        [TestMethod]
        public void ToHtml_MixedContent_ContainsAllElements()
        {
            string md = "# Titulo\n\nParrafo con **negrita**.\n\n- item\n\n[link](https://x.com)";
            string result = MarkdownRenderer.ToHtml(md);

            StringAssert.Contains(result, "<h1>");
            StringAssert.Contains(result, "<strong>");
            StringAssert.Contains(result, "<ul>");
            StringAssert.Contains(result, "<a ");
        }
    }
}
