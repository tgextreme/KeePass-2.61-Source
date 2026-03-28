using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class DashboardServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PwDatabase OpenDb()
        {
            var db = new PwDatabase();
            db.New(new KeePassLib.Serialization.IOConnectionInfo(),
                new KeePassLib.Keys.CompositeKey());
            return db;
        }

        private static PwEntry AddEntry(PwDatabase db, string password = "abc",
            bool expires = false, DateTime? expiryTime = null)
        {
            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, password));
            if(expires)
            {
                e.Expires    = true;
                e.ExpiryTime = expiryTime ?? DateTime.UtcNow.AddDays(-1); // default: already expired
            }
            db.RootGroup.AddEntry(e, true);
            return e;
        }

        // ── GetMetrics — null / closed db ─────────────────────────────────────

        [TestMethod]
        public void GetMetrics_NullDb_ReturnsZeroCounts()
        {
            var svc     = new DashboardService();
            DashboardMetrics m = svc.GetMetrics(null);

            Assert.AreEqual(0, m.TotalEntries);
            Assert.AreEqual(0, m.ExpiredEntries);
            Assert.AreEqual(0, m.ExpiringIn14Days);
            Assert.AreEqual(0, m.WeakPasswords);
            Assert.AreEqual(0, m.DuplicatePasswords);
            Assert.IsNotNull(m.TopRisks);
        }

        // ── GetMetrics — TotalEntries ──────────────────────────────────────────

        [TestMethod]
        public void GetMetrics_TotalEntries_MatchesDbCount()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db);
            AddEntry(db);
            AddEntry(db);

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(3, m.TotalEntries);
        }

        // ── GetMetrics — ExpiredEntries ────────────────────────────────────────

        [TestMethod]
        public void GetMetrics_ExpiredEntries_CountsCorrectly()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db, expires: true, expiryTime: DateTime.UtcNow.AddDays(-5)); // expired
            AddEntry(db, expires: false);                                           // not expiring

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(1, m.ExpiredEntries);
        }

        // ── GetMetrics — ExpiringIn14Days ──────────────────────────────────────

        [TestMethod]
        public void GetMetrics_ExpiringIn14Days_CountsCorrectly()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db, expires: true, expiryTime: DateTime.UtcNow.AddDays(5));   // expires in 5 days
            AddEntry(db, expires: true, expiryTime: DateTime.UtcNow.AddDays(-3));  // already expired (not counted)
            AddEntry(db, expires: true, expiryTime: DateTime.UtcNow.AddDays(20));  // expires in 20 (not counted)

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(1, m.ExpiringIn14Days);
        }

        // ── GetMetrics — PwnedPasswords always 0 ──────────────────────────────

        [TestMethod]
        public void GetMetrics_PwnedPasswords_IsAlwaysZero()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db);

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(0, m.PwnedPasswords);
        }

        // ── GetMetrics — SecurityScore ─────────────────────────────────────────

        [TestMethod]
        public void GetMetrics_SecurityScore_IsInRange()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db, password: "1");    // weak
            AddEntry(db, password: "P4ssw0rd!longstrongenough_abc"); // strong

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.IsTrue(m.SecurityScore >= 0 && m.SecurityScore <= 100,
                $"SecurityScore fuera de rango: {m.SecurityScore}");
        }

        // ── GetMetrics — DuplicatePasswords ────────────────────────────────────

        [TestMethod]
        public void GetMetrics_DuplicatePasswords_CountsGroups()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db, password: "dup");
            AddEntry(db, password: "dup");
            AddEntry(db, password: "unique1");

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(1, m.DuplicatePasswords);
        }

        // ── GetMetrics — TopRisks ──────────────────────────────────────────────

        [TestMethod]
        public void GetMetrics_TopRisks_MaxFiveEntries()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            // Add 6 expired entries
            for(int i = 0; i < 6; i++)
                AddEntry(db, expires: true, expiryTime: DateTime.UtcNow.AddDays(-1));

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.IsTrue(m.TopRisks.Count <= 5);
        }

        [TestMethod]
        public void GetMetrics_TopRisks_IsNotNull()
        {
            var db  = OpenDb();
            var svc = new DashboardService();
            AddEntry(db);

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.IsNotNull(m.TopRisks);
        }

        // ── Constructor con IPasswordAnalysisService inyectado ─────────────────

        [TestMethod]
        public void Constructor_WithCustomAnalysis_Works()
        {
            var analysis = new PasswordAnalysisService();
            var svc      = new DashboardService(analysis);
            var db       = OpenDb();
            AddEntry(db);

            DashboardMetrics m = svc.GetMetrics(db);

            Assert.AreEqual(1, m.TotalEntries);
        }
    }
}
