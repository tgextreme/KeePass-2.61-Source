using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Infrastructure.Background;

namespace KeePass.Tests.Infrastructure
{
    /// <summary>
    /// Tests for WindowsStartupHelper — reads/writes HKCU registry key.
    /// Cleanup is guaranteed via [TestCleanup] so the test machine is
    /// never left with a stray registry value after the suite runs.
    /// </summary>
    [TestClass]
    public class WindowsStartupHelperTests
    {
        private const string DummyExePath = @"C:\Fake\KeePass.exe";

        [TestCleanup]
        public void Cleanup()
        {
            // Always remove the registry value, regardless of test outcome.
            WindowsStartupHelper.Disable();
        }

        // ── IsEnabled (initial state) ─────────────────────────────────

        [TestMethod]
        public void IsEnabled_ReturnsFalse_WhenKeyNotPresent()
        {
            WindowsStartupHelper.Disable(); // ensure clean state
            Assert.IsFalse(WindowsStartupHelper.IsEnabled());
        }

        // ── Enable / IsEnabled ─────────────────────────────────────────

        [TestMethod]
        public void Enable_Then_IsEnabled_ReturnsTrue()
        {
            WindowsStartupHelper.Enable(DummyExePath);
            Assert.IsTrue(WindowsStartupHelper.IsEnabled());
        }

        [TestMethod]
        public void Enable_WritesExpectedPath()
        {
            WindowsStartupHelper.Enable(DummyExePath);

            // Verify the stored value matches by reading the registry directly
            using(var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                Assert.IsNotNull(key);
                string stored = key.GetValue("KeePassModernVibe") as string;
                Assert.IsNotNull(stored, "El valor de registro debe existir");
                Assert.IsTrue(stored.Contains(DummyExePath),
                    $"El path almacenado debe contener la ruta del exe. Actual: {stored}");
            }
        }

        // ── Disable ────────────────────────────────────────────────────

        [TestMethod]
        public void Disable_After_Enable_RemovesRegistryValue()
        {
            WindowsStartupHelper.Enable(DummyExePath);
            Assert.IsTrue(WindowsStartupHelper.IsEnabled()); // precondition

            WindowsStartupHelper.Disable();
            Assert.IsFalse(WindowsStartupHelper.IsEnabled());
        }

        [TestMethod]
        public void Disable_WhenNotEnabled_DoesNotThrow()
        {
            WindowsStartupHelper.Disable(); // ensure absent
            WindowsStartupHelper.Disable(); // second call — must not throw
        }

        // ── Idempotency ────────────────────────────────────────────────

        [TestMethod]
        public void Enable_CalledTwice_IsIdempotent()
        {
            WindowsStartupHelper.Enable(DummyExePath);
            WindowsStartupHelper.Enable(DummyExePath); // overwrite with same value
            Assert.IsTrue(WindowsStartupHelper.IsEnabled());
        }
    }
}
