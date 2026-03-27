using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.App.Configuration;

namespace KeePass.Tests.Config
{
    [TestClass]
    public class AceLayoutTests
    {
        [TestMethod]
        public void DefaultFontScale_IsOne()
        {
            var layout = new AceLayout();
            Assert.AreEqual(1.0f, layout.FontScale, 0.0001f);
        }

        [TestMethod]
        public void FontScale_ClampedToMin()
        {
            var layout = new AceLayout();
            layout.FontScale = 0.1f;
            Assert.AreEqual(0.5f, layout.FontScale, 0.0001f);
        }

        [TestMethod]
        public void FontScale_ClampedToMax()
        {
            var layout = new AceLayout();
            layout.FontScale = 9.9f;
            Assert.AreEqual(3.0f, layout.FontScale, 0.0001f);
        }

        [TestMethod]
        public void FontScale_AcceptsValueInRange()
        {
            var layout = new AceLayout();
            layout.FontScale = 1.5f;
            Assert.AreEqual(1.5f, layout.FontScale, 0.0001f);
        }

        [TestMethod]
        public void FontScale_AcceptsBoundaryMin()
        {
            var layout = new AceLayout();
            layout.FontScale = 0.5f;
            Assert.AreEqual(0.5f, layout.FontScale, 0.0001f);
        }

        [TestMethod]
        public void FontScale_AcceptsBoundaryMax()
        {
            var layout = new AceLayout();
            layout.FontScale = 3.0f;
            Assert.AreEqual(3.0f, layout.FontScale, 0.0001f);
        }
    }

    [TestClass]
    public class AceBackupTests
    {
        [TestMethod]
        public void Defaults_Enabled_IsTrue()
        {
            var cfg = new AceBackup();
            Assert.IsTrue(cfg.Enabled);
        }

        [TestMethod]
        public void Defaults_Folder_IsEmpty()
        {
            var cfg = new AceBackup();
            Assert.AreEqual(string.Empty, cfg.Folder);
        }

        [TestMethod]
        public void Defaults_MaxKeep_IsTen()
        {
            var cfg = new AceBackup();
            Assert.AreEqual(10, cfg.MaxKeep);
        }

        [TestMethod]
        public void Defaults_BackupOnSave_IsTrue()
        {
            var cfg = new AceBackup();
            Assert.IsTrue(cfg.BackupOnSave);
        }

        [TestMethod]
        public void MaxKeep_ClampedToOne_WhenSetToZero()
        {
            var cfg = new AceBackup();
            cfg.MaxKeep = 0;
            Assert.AreEqual(1, cfg.MaxKeep);
        }

        [TestMethod]
        public void MaxKeep_ClampedToOne_WhenSetToNegative()
        {
            var cfg = new AceBackup();
            cfg.MaxKeep = -5;
            Assert.AreEqual(1, cfg.MaxKeep);
        }

        [TestMethod]
        public void MaxKeep_AcceptsPositiveValue()
        {
            var cfg = new AceBackup();
            cfg.MaxKeep = 5;
            Assert.AreEqual(5, cfg.MaxKeep);
        }
    }

    [TestClass]
    public class AceBackgroundModeTests
    {
        [TestMethod]
        public void Defaults_RunInBackground_IsTrue()
        {
            var cfg = new AceBackgroundMode();
            Assert.IsTrue(cfg.RunInBackground);
        }

        [TestMethod]
        public void Defaults_StartWithWindows_IsFalse()
        {
            var cfg = new AceBackgroundMode();
            Assert.IsFalse(cfg.StartWithWindows);
        }

        [TestMethod]
        public void Defaults_StartMinimized_IsFalse()
        {
            var cfg = new AceBackgroundMode();
            Assert.IsFalse(cfg.StartMinimized);
        }

        [TestMethod]
        public void Defaults_MinimizeToTray_IsTrue()
        {
            var cfg = new AceBackgroundMode();
            Assert.IsTrue(cfg.MinimizeToTray);
        }

        [TestMethod]
        public void Defaults_CloseToTray_IsTrue()
        {
            var cfg = new AceBackgroundMode();
            Assert.IsTrue(cfg.CloseToTray);
        }

        [TestMethod]
        public void Defaults_ShowRecentCount_IsFive()
        {
            var cfg = new AceBackgroundMode();
            Assert.AreEqual(5, cfg.ShowRecentCount);
        }
    }
}
