using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.UI;

namespace KeePass.Tests.UI
{
    [TestClass]
    public class ThemeManagerTests
    {
        /// <summary>Restore Light theme after every test to avoid state leakage.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            ThemeManager.SetTheme(KeePassTheme.Light);
        }

        // ── Estado inicial ────────────────────────────────────────────────────

        [TestMethod]
        public void CurrentTheme_DefaultIsLight()
        {
            // Cleanup already resets to Light; verify default state.
            Assert.AreEqual(KeePassTheme.Light, ThemeManager.CurrentTheme);
        }

        [TestMethod]
        public void IsDark_DefaultIsFalse()
        {
            Assert.IsFalse(ThemeManager.IsDark);
        }

        // ── SetTheme ──────────────────────────────────────────────────────────

        [TestMethod]
        public void SetTheme_Dark_ChangesCurrentTheme()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Assert.AreEqual(KeePassTheme.Dark, ThemeManager.CurrentTheme);
        }

        [TestMethod]
        public void SetTheme_Dark_SetsIsDarkTrue()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Assert.IsTrue(ThemeManager.IsDark);
        }

        [TestMethod]
        public void SetTheme_BackToLight_SetsIsDarkFalse()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            ThemeManager.SetTheme(KeePassTheme.Light);
            Assert.IsFalse(ThemeManager.IsDark);
        }

        [TestMethod]
        public void SetTheme_Custom_ChangesCurrentTheme()
        {
            ThemeManager.SetTheme(KeePassTheme.Custom);
            Assert.AreEqual(KeePassTheme.Custom, ThemeManager.CurrentTheme);
        }

        // ── GetBackColor ──────────────────────────────────────────────────────

        [TestMethod]
        public void GetBackColor_Dark_ReturnsDarkColor()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Color c = ThemeManager.GetBackColor();
            Assert.AreEqual(Color.FromArgb(30, 30, 30), c);
        }

        [TestMethod]
        public void GetBackColor_Light_ReturnsSystemColor()
        {
            ThemeManager.SetTheme(KeePassTheme.Light);
            Color c = ThemeManager.GetBackColor();
            Assert.AreEqual(SystemColors.Window, c);
        }

        // ── GetForeColor ──────────────────────────────────────────────────────

        [TestMethod]
        public void GetForeColor_Dark_ReturnsDarkForeColor()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Color c = ThemeManager.GetForeColor();
            Assert.AreEqual(Color.FromArgb(220, 220, 220), c);
        }

        [TestMethod]
        public void GetForeColor_Light_ReturnsSystemWindowText()
        {
            ThemeManager.SetTheme(KeePassTheme.Light);
            Color c = ThemeManager.GetForeColor();
            Assert.AreEqual(SystemColors.WindowText, c);
        }

        // ── GetControlColor ────────────────────────────────────────────────────

        [TestMethod]
        public void GetControlColor_Dark_ReturnsDarkControlColor()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Color c = ThemeManager.GetControlColor();
            Assert.AreEqual(Color.FromArgb(45, 45, 48), c);
        }

        [TestMethod]
        public void GetControlColor_Light_ReturnsSystemControl()
        {
            ThemeManager.SetTheme(KeePassTheme.Light);
            Color c = ThemeManager.GetControlColor();
            Assert.AreEqual(SystemColors.Control, c);
        }

        // ── GetBorderColor ─────────────────────────────────────────────────────

        [TestMethod]
        public void GetBorderColor_Dark_ReturnsDarkBorderColor()
        {
            ThemeManager.SetTheme(KeePassTheme.Dark);
            Color c = ThemeManager.GetBorderColor();
            Assert.AreEqual(Color.FromArgb(63, 63, 70), c);
        }

        [TestMethod]
        public void GetBorderColor_Light_ReturnsSystemControlDark()
        {
            ThemeManager.SetTheme(KeePassTheme.Light);
            Color c = ThemeManager.GetBorderColor();
            Assert.AreEqual(SystemColors.ControlDark, c);
        }

        // ── ThemeChanged event ─────────────────────────────────────────────────

        [TestMethod]
        public void ThemeChanged_Fires_WhenThemeChanges()
        {
            bool fired = false;
            System.EventHandler<KeePassTheme> h = (s, t) => { fired = true; };
            ThemeManager.ThemeChanged += h;
            try
            {
                ThemeManager.SetTheme(KeePassTheme.Dark);
                Assert.IsTrue(fired);
            }
            finally
            {
                ThemeManager.ThemeChanged -= h;
            }
        }

        [TestMethod]
        public void ThemeChanged_ReportsNewTheme()
        {
            KeePassTheme reported = KeePassTheme.Light;
            System.EventHandler<KeePassTheme> h = (s, t) => { reported = t; };
            ThemeManager.ThemeChanged += h;
            try
            {
                ThemeManager.SetTheme(KeePassTheme.Dark);
                Assert.AreEqual(KeePassTheme.Dark, reported);
            }
            finally
            {
                ThemeManager.ThemeChanged -= h;
            }
        }

        [TestMethod]
        public void SetTheme_SameThemeNoForm_DoesNotFireEvent()
        {
            // Already Light after Cleanup. Setting Light again with no form → no change → no event.
            bool fired = false;
            System.EventHandler<KeePassTheme> h = (s, t) => { fired = true; };
            ThemeManager.ThemeChanged += h;
            try
            {
                ThemeManager.SetTheme(KeePassTheme.Light); // same as current, no form
                Assert.IsFalse(fired);
            }
            finally
            {
                ThemeManager.ThemeChanged -= h;
            }
        }
    }
}
