using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePass.Integration.BrowserImport;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class BrowserImportServiceTests
    {
        // ── helpers ───────────────────────────────────────────────────

        private static PwDatabase OpenDb()
        {
            var db  = new PwDatabase();
            var key = new CompositeKey();
            key.AddUserKey(new KcpPassword("test"));
            db.New(new IOConnectionInfo(), key);
            return db;
        }

        private static PwEntry AddEntry(PwDatabase db, string url, string user,
            string title = "entry")
        {
            var pe = new PwEntry(true, true);
            pe.Strings.Set(PwDefs.TitleField,
                new ProtectedString(false, title));
            pe.Strings.Set(PwDefs.UrlField,
                new ProtectedString(false, url));
            pe.Strings.Set(PwDefs.UserNameField,
                new ProtectedString(false, user));
            db.RootGroup.AddEntry(pe, true);
            return pe;
        }

        private static BrowserCredential MakeCred(string url, string user,
            string password = "pass")
        {
            return new BrowserCredential
            {
                Url      = url,
                Username = user,
                Password = password,
            };
        }

        // ── GetAvailableProfiles ──────────────────────────────────────

        [TestMethod]
        public void GetAvailableProfiles_ReturnsNonNullList()
        {
            var svc = new BrowserImportService();
            List<BrowserProfile> profiles = svc.GetAvailableProfiles();

            // May be empty if no browser installed, but must not be null.
            Assert.IsNotNull(profiles);
        }

        [TestMethod]
        public void GetAvailableProfiles_EachProfileHasName()
        {
            var svc      = new BrowserImportService();
            var profiles = svc.GetAvailableProfiles();

            foreach(BrowserProfile p in profiles)
            {
                Assert.IsNotNull(p, "Profile should not be null");
                Assert.IsFalse(string.IsNullOrEmpty(p.BrowserName),
                    "BrowserName should not be empty");
            }
        }

        // ── DetectDuplicates ──────────────────────────────────────────

        [TestMethod]
        public void DetectDuplicates_NullCredentials_ThrowsArgumentNullException()
        {
            var svc = new BrowserImportService();
            var db  = OpenDb();

            System.Exception caught = null;
            try { svc.DetectDuplicates(null, db); }
            catch(System.ArgumentNullException ex) { caught = ex; }

            Assert.IsNotNull(caught, "Expected ArgumentNullException");
        }

        [TestMethod]
        public void DetectDuplicates_NullDb_ReturnsEmptyList()
        {
            var svc = new BrowserImportService();
            var creds = new List<BrowserCredential> { MakeCred("https://a.com", "user") };

            List<BrowserCredential> dupes = svc.DetectDuplicates(creds, null);

            Assert.IsNotNull(dupes);
            Assert.AreEqual(0, dupes.Count);
        }

        [TestMethod]
        public void DetectDuplicates_EmptyDb_ReturnsEmptyList()
        {
            var svc   = new BrowserImportService();
            var db    = OpenDb();
            var creds = new List<BrowserCredential> { MakeCred("https://a.com", "user") };

            List<BrowserCredential> dupes = svc.DetectDuplicates(creds, db);

            Assert.AreEqual(0, dupes.Count);
        }

        [TestMethod]
        public void DetectDuplicates_ExactMatch_IsReportedAsDuplicate()
        {
            var svc = new BrowserImportService();
            var db  = OpenDb();
            AddEntry(db, "https://example.com", "alice");

            var creds = new List<BrowserCredential>
            {
                MakeCred("https://example.com", "alice"),
            };

            List<BrowserCredential> dupes = svc.DetectDuplicates(creds, db);

            Assert.AreEqual(1, dupes.Count);
        }

        [TestMethod]
        public void DetectDuplicates_DifferentUser_IsNotDuplicate()
        {
            var svc = new BrowserImportService();
            var db  = OpenDb();
            AddEntry(db, "https://example.com", "alice");

            var creds = new List<BrowserCredential>
            {
                MakeCred("https://example.com", "bob"),  // different user
            };

            List<BrowserCredential> dupes = svc.DetectDuplicates(creds, db);

            Assert.AreEqual(0, dupes.Count);
        }

        [TestMethod]
        public void DetectDuplicates_EmptyCredentials_ReturnsEmptyList()
        {
            var svc   = new BrowserImportService();
            var db    = OpenDb();
            var creds = new List<BrowserCredential>();

            List<BrowserCredential> dupes = svc.DetectDuplicates(creds, db);

            Assert.AreEqual(0, dupes.Count);
        }
    }
}
