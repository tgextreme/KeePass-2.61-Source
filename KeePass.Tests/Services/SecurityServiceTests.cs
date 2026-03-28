using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class SecurityServiceTests
    {
        // ── IsStreamerModeActive — estado inicial ──────────────────────────────

        [TestMethod]
        public void IsStreamerModeActive_InitiallyFalse()
        {
            var svc = new SecurityService();
            Assert.IsFalse(svc.IsStreamerModeActive);
        }

        // ── EnableStreamerMode ────────────────────────────────────────────────

        [TestMethod]
        public void EnableStreamerMode_SetsIsStreamerModeTrue()
        {
            var svc = new SecurityService();
            // Pass null: the implementation only calls NativeSecurityHelper.EnableAntiScreenshot
            // when mainForm != null, so no P/Invoke happens.
            svc.EnableStreamerMode(null);

            Assert.IsTrue(svc.IsStreamerModeActive);
        }

        // ── DisableStreamerMode ───────────────────────────────────────────────

        [TestMethod]
        public void DisableStreamerMode_SetsIsStreamerModeFalse()
        {
            var svc = new SecurityService();
            svc.EnableStreamerMode(null);   // first enable
            svc.DisableStreamerMode(null);  // then disable

            Assert.IsFalse(svc.IsStreamerModeActive);
        }

        [TestMethod]
        public void DisableStreamerMode_WhenAlreadyDisabled_RemainsDisabled()
        {
            var svc = new SecurityService();
            svc.DisableStreamerMode(null); // called without enabling first

            Assert.IsFalse(svc.IsStreamerModeActive);
        }

        // ── Toggle sequence ───────────────────────────────────────────────────

        [TestMethod]
        public void EnableThenDisableThenEnable_StateIsCorrect()
        {
            var svc = new SecurityService();

            svc.EnableStreamerMode(null);
            Assert.IsTrue(svc.IsStreamerModeActive);

            svc.DisableStreamerMode(null);
            Assert.IsFalse(svc.IsStreamerModeActive);

            svc.EnableStreamerMode(null);
            Assert.IsTrue(svc.IsStreamerModeActive);
        }

        // ── EnableAntiScreenshot / DisableAntiScreenshot (null form) ──────────

        [TestMethod]
        public void EnableAntiScreenshot_NullForm_DoesNotThrow()
        {
            // null handle is ignored by NativeSecurityHelper (no valid HWND)
            new SecurityService().EnableAntiScreenshot(null);
        }

        [TestMethod]
        public void DisableAntiScreenshot_NullForm_DoesNotThrow()
        {
            new SecurityService().DisableAntiScreenshot(null);
        }
    }
}
