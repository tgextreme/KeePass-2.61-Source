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
            return new BrowserCredential(url, url, user, password, string.Empty);
        }

        // ── GetSupportedFormats ──────────────────────────────────────

        [TestMethod]
        public void GetSupportedFormats_ReturnsNonNullList()
        {
            var svc = new BrowserImportService();
            List<BrowserCsvFormatInfo> formats = svc.GetSupportedFormats();

            Assert.IsNotNull(formats);
            Assert.IsTrue(formats.Count >= 4);
        }

        [TestMethod]
        public void GetSupportedFormats_EachFormatHasName()
        {
            var svc      = new BrowserImportService();
            var formats = svc.GetSupportedFormats();

            foreach(BrowserCsvFormatInfo f in formats)
            {
                Assert.IsNotNull(f, "Format should not be null");
                Assert.IsFalse(string.IsNullOrEmpty(f.DisplayName),
                    "DisplayName should not be empty");
            }
        }

        [TestMethod]
        public void PreviewCredentialsFromCsv_ChromeStyle_ParsesRows()
        {
            var svc = new BrowserImportService();
            string path = System.IO.Path.GetTempFileName();

            try
            {
                string csv = "name,url,username,password,note\n" +
                    "Example,https://example.com,alice,s3cr3t,n\n";
                System.IO.File.WriteAllText(path, csv);

                List<BrowserCredential> creds = svc.PreviewCredentialsFromCsv(
                    path, BrowserCsvFormat.Chrome);

                Assert.AreEqual(1, creds.Count);
                Assert.AreEqual("Example", creds[0].Title);
                Assert.AreEqual("alice", creds[0].Username);
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }

        [TestMethod]
        public void PreviewCredentialsFromCsv_MissingPasswordColumn_ThrowsFormatException()
        {
            var svc = new BrowserImportService();
            string path = System.IO.Path.GetTempFileName();

            try
            {
                string csv = "name,url,username\n" +
                    "Example,https://example.com,alice\n";
                System.IO.File.WriteAllText(path, csv);

                System.Exception caught = null;
                try
                {
                    svc.PreviewCredentialsFromCsv(path, BrowserCsvFormat.Chrome);
                }
                catch(System.FormatException ex)
                {
                    caught = ex;
                }

                Assert.IsNotNull(caught, "Expected FormatException");
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
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
