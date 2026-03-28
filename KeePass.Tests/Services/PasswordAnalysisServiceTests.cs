using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class PasswordAnalysisServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PwDatabase OpenDb()
        {
            var db = new PwDatabase();
            db.New(new KeePassLib.Serialization.IOConnectionInfo(),
                new KeePassLib.Keys.CompositeKey());
            return db;
        }

        private static PwEntry AddEntry(PwDatabase db, string password)
        {
            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, password));
            db.RootGroup.AddEntry(e, true);
            return e;
        }

        // ── GetWeakEntries ────────────────────────────────────────────────────

        [TestMethod]
        public void GetWeakEntries_WeakPassword_IsIncluded()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "1"); // extremely weak

            // threshold 100 bits catches even medium passwords
            IList<PwEntry> weak = svc.GetWeakEntries(db, 100u);

            Assert.IsTrue(weak.Count > 0);
        }

        [TestMethod]
        public void GetWeakEntries_StrongPassword_IsExcluded()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            // Very long password that exceeds the high threshold
            AddEntry(db, "Xk9!mN#2pQ@vL7$wRjE4&sY1^aB8*cF5");

            IList<PwEntry> weak = svc.GetWeakEntries(db, 1u); // threshold 1 bit — nothing is that weak

            Assert.AreEqual(0, weak.Count);
        }

        [TestMethod]
        public void GetWeakEntries_NullDb_ReturnsEmpty()
        {
            IList<PwEntry> weak = new PasswordAnalysisService().GetWeakEntries(null, 50u);
            Assert.AreEqual(0, weak.Count);
        }

        [TestMethod]
        public void GetWeakEntries_EmptyDb_ReturnsEmpty()
        {
            var db  = OpenDb(); // no entries
            IList<PwEntry> weak = new PasswordAnalysisService().GetWeakEntries(db, 50u);
            Assert.AreEqual(0, weak.Count);
        }

        [TestMethod]
        public void GetWeakEntries_EmptyPassword_IsIgnored()
        {
            var db = OpenDb();
            // Entry with empty password — service skips it (see implementation: string.IsNullOrEmpty check)
            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, string.Empty));
            db.RootGroup.AddEntry(e, true);

            IList<PwEntry> weak = new PasswordAnalysisService().GetWeakEntries(db, 100u);

            Assert.AreEqual(0, weak.Count);
        }

        // ── GetDuplicateGroups ────────────────────────────────────────────────

        [TestMethod]
        public void GetDuplicateGroups_SamePassword_ReturnsSingleGroup()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "duplicated");
            AddEntry(db, "duplicated");

            IList<IList<PwEntry>> groups = svc.GetDuplicateGroups(db);

            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(2, groups[0].Count);
        }

        [TestMethod]
        public void GetDuplicateGroups_AllUnique_ReturnsEmpty()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "pass1");
            AddEntry(db, "pass2");
            AddEntry(db, "pass3");

            IList<IList<PwEntry>> groups = svc.GetDuplicateGroups(db);

            Assert.AreEqual(0, groups.Count);
        }

        [TestMethod]
        public void GetDuplicateGroups_NullDb_ReturnsEmpty()
        {
            IList<IList<PwEntry>> groups = new PasswordAnalysisService().GetDuplicateGroups(null);
            Assert.AreEqual(0, groups.Count);
        }

        [TestMethod]
        public void GetDuplicateGroups_ThreeIdentical_ReturnGroupOfThree()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "same");
            AddEntry(db, "same");
            AddEntry(db, "same");

            IList<IList<PwEntry>> groups = svc.GetDuplicateGroups(db);

            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(3, groups[0].Count);
        }

        // ── GetReport ─────────────────────────────────────────────────────────

        [TestMethod]
        public void GetReport_NullDb_ReturnsScoreZero()
        {
            SecurityReport report = new PasswordAnalysisService().GetReport(null);

            Assert.AreEqual(0, report.Score);
            Assert.AreEqual(0, report.TotalEntries);
        }

        [TestMethod]
        public void GetReport_EmptyDb_ReturnsHundred()
        {
            var db = OpenDb(); // no entries
            SecurityReport report = new PasswordAnalysisService().GetReport(db);

            // 100 - (0 * 100 / 0) — no entries means penalties=0, total=0 → returns 100
            Assert.AreEqual(100, report.Score);
        }

        [TestMethod]
        public void GetReport_ScoreIsInRange()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "1");  // weak
            AddEntry(db, "strong_Passw0rd!_long_enough_abc");

            SecurityReport report = svc.GetReport(db);

            Assert.IsTrue(report.Score >= 0 && report.Score <= 100,
                $"Score out of range: {report.Score}");
        }

        [TestMethod]
        public void GetReport_TotalEntries_MatchesCount()
        {
            var db  = OpenDb();
            var svc = new PasswordAnalysisService();
            AddEntry(db, "abc");
            AddEntry(db, "def");
            AddEntry(db, "ghi");

            SecurityReport report = svc.GetReport(db);

            Assert.AreEqual(3, report.TotalEntries);
        }

        [TestMethod]
        public void GetReport_WeakEntries_NonNull()
        {
            var db = OpenDb();
            AddEntry(db, "abc");

            SecurityReport report = new PasswordAnalysisService().GetReport(db);

            Assert.IsNotNull(report.WeakEntries);
            Assert.IsNotNull(report.DuplicateGroups);
        }
    }
}
