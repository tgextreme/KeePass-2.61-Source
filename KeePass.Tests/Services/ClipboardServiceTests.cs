using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;

namespace KeePass.Tests.Services
{
    /// <summary>
    /// Tests for ClipboardService that do NOT touch the actual clipboard
    /// (Clipboard.SetText / ClipboardUtil.Copy require WinForms STA apartment).
    /// </summary>
    [TestClass]
    public class ClipboardServiceTests
    {
        // ── Constructor / estado inicial ──────────────────────────────────────

        [TestMethod]
        public void Constructor_DoesNotThrow()
        {
            using(var svc = new ClipboardService()) { }
        }

        [TestMethod]
        public void SecondsToClear_Initially_IsZero()
        {
            using(var svc = new ClipboardService())
            {
                Assert.AreEqual(0, svc.SecondsToClear);
            }
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [TestMethod]
        public void Dispose_DoesNotThrow()
        {
            var svc = new ClipboardService();
            svc.Dispose(); // first call
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var svc = new ClipboardService();
            svc.Dispose();
            svc.Dispose(); // second call — must be idempotent
        }

        // ── Clear (sin copia previa) ───────────────────────────────────────────

        [TestMethod]
        public void Clear_WithoutPriorCopy_DoesNotThrow()
        {
            using(var svc = new ClipboardService())
            {
                svc.Clear();
            }
        }

        [TestMethod]
        public void Clear_ResetsSecondsToClearToZero()
        {
            using(var svc = new ClipboardService())
            {
                svc.Clear();
                Assert.AreEqual(0, svc.SecondsToClear);
            }
        }

        // ── IClipboardService interface ────────────────────────────────────────

        [TestMethod]
        public void ClipboardService_ImplementsIClipboardService()
        {
            using(var svc = new ClipboardService())
            {
                Assert.IsInstanceOfType(svc, typeof(IClipboardService));
            }
        }

        [TestMethod]
        public void ClipboardService_ImplementsIDisposable()
        {
            using(var svc = new ClipboardService())
            {
                Assert.IsInstanceOfType(svc, typeof(System.IDisposable));
            }
        }
    }
}
