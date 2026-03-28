using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class SearchServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PwDatabase OpenDb()
        {
            var db = new PwDatabase();
            db.New(new KeePassLib.Serialization.IOConnectionInfo(),
                new KeePassLib.Keys.CompositeKey());
            return db;
        }

        private static PwEntry AddEntry(PwDatabase db,
            string title = "", string user = "", string url = "", string notes = "",
            string password = "abc", DateTime? lastAccess = null)
        {
            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.TitleField,    new ProtectedString(false, title));
            e.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, user));
            e.Strings.Set(PwDefs.UrlField,      new ProtectedString(false, url));
            e.Strings.Set(PwDefs.NotesField,    new ProtectedString(false, notes));
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true,  password));
            if(lastAccess.HasValue) e.LastAccessTime = lastAccess.Value;
            db.RootGroup.AddEntry(e, true);
            return e;
        }

        // ── Search ────────────────────────────────────────────────────────────

        [TestMethod]
        public void Search_MatchesByTitle()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, title: "Amazon");
            AddEntry(db, title: "Gmail");

            IList<PwEntry> results = svc.Search(db, "Amazon");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Search_MatchesByUsername()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, user: "john@example.com");
            AddEntry(db, user: "other");

            IList<PwEntry> results = svc.Search(db, "john");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Search_MatchesByUrl()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, url: "https://github.com");
            AddEntry(db, url: "https://google.com");

            IList<PwEntry> results = svc.Search(db, "github");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Search_MatchesByNotes()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, notes: "personal email account");

            IList<PwEntry> results = svc.Search(db, "personal");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Search_IsCaseInsensitive()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, title: "AMAZON");

            IList<PwEntry> results = svc.Search(db, "amazon");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Search_NullDb_ReturnsEmpty()
        {
            IList<PwEntry> results = new SearchService().Search(null, "x");
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Search_EmptyQuery_ReturnsEmpty()
        {
            var db = OpenDb();
            AddEntry(db, title: "Something");

            IList<PwEntry> results = new SearchService().Search(db, string.Empty);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Search_NoMatch_ReturnsEmpty()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, title: "Gmail");

            IList<PwEntry> results = svc.Search(db, "ZZZNOMATCH");

            Assert.AreEqual(0, results.Count);
        }

        // ── SearchByDomain ────────────────────────────────────────────────────

        [TestMethod]
        public void SearchByDomain_MatchesExactHost()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, url: "https://example.com/login");

            IList<PwEntry> results = svc.SearchByDomain(db, "example.com");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void SearchByDomain_MatchesSubdomain()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, url: "https://mail.example.com/inbox");

            // host is "mail.example.com" which ends with ".example.com"
            IList<PwEntry> results = svc.SearchByDomain(db, "example.com");

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void SearchByDomain_NoMatch_DifferentDomain()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, url: "https://notexample.com/page");

            IList<PwEntry> results = svc.SearchByDomain(db, "example.com");

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchByDomain_NullDb_ReturnsEmpty()
        {
            IList<PwEntry> results = new SearchService().SearchByDomain(null, "x.com");
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchByDomain_EmptyDomain_ReturnsEmpty()
        {
            var db = OpenDb();
            AddEntry(db, url: "https://example.com");

            IList<PwEntry> results = new SearchService().SearchByDomain(db, string.Empty);

            Assert.AreEqual(0, results.Count);
        }

        // ── GetRecent ─────────────────────────────────────────────────────────

        [TestMethod]
        public void GetRecent_ReturnsRequestedCount()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            var now = DateTime.UtcNow;

            AddEntry(db, title: "A", lastAccess: now.AddDays(-3));
            AddEntry(db, title: "B", lastAccess: now.AddDays(-1));
            AddEntry(db, title: "C", lastAccess: now.AddDays(-2));

            IList<PwEntry> results = svc.GetRecent(db, 3);

            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public void GetRecent_ReturnsSortedDescending()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            var now = DateTime.UtcNow;

            var oldest = AddEntry(db, title: "Oldest", lastAccess: now.AddDays(-5));
            var newest = AddEntry(db, title: "Newest", lastAccess: now.AddDays(-1));
            var middle = AddEntry(db, title: "Middle", lastAccess: now.AddDays(-3));

            IList<PwEntry> results = svc.GetRecent(db, 3);

            Assert.AreSame(newest, results[0]);
            Assert.AreSame(middle, results[1]);
            Assert.AreSame(oldest, results[2]);
        }

        [TestMethod]
        public void GetRecent_CountLargerThanEntries_ReturnsAll()
        {
            var db  = OpenDb();
            var svc = new SearchService();
            AddEntry(db, title: "Only");

            IList<PwEntry> results = svc.GetRecent(db, 10);

            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void GetRecent_ZeroCount_ReturnsEmpty()
        {
            var db = OpenDb();
            AddEntry(db, title: "X");

            IList<PwEntry> results = new SearchService().GetRecent(db, 0);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void GetRecent_NullDb_ReturnsEmpty()
        {
            IList<PwEntry> results = new SearchService().GetRecent(null, 5);
            Assert.AreEqual(0, results.Count);
        }
    }
}
