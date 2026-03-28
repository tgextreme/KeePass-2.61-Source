using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class CredentialServiceTests
    {
        private static PwEntry MakeEntry(string title = "Test", string user = "user",
            string url = "https://example.com", string password = "pass", string notes = "notes")
        {
            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.TitleField,    new ProtectedString(false, title));
            e.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, user));
            e.Strings.Set(PwDefs.UrlField,      new ProtectedString(false, url));
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true,  password));
            e.Strings.Set(PwDefs.NotesField,    new ProtectedString(false, notes));
            return e;
        }

        private static PwDatabase OpenDb()
        {
            var db = new PwDatabase();
            db.New(new KeePassLib.Serialization.IOConnectionInfo(),
                new KeePassLib.Keys.CompositeKey());
            return db;
        }

        // ── GetUsername ───────────────────────────────────────────────────────

        [TestMethod]
        public void GetUsername_ReturnsCorrectValue()
        {
            var svc = new CredentialService();
            var e   = MakeEntry(user: "john");

            Assert.AreEqual("john", svc.GetUsername(e));
        }

        [TestMethod]
        public void GetUsername_NullEntry_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, new CredentialService().GetUsername(null));
        }

        [TestMethod]
        public void GetUsername_EmptyEntry_ReturnsEmpty()
        {
            var svc = new CredentialService();
            var e   = new PwEntry(true, true); // no fields set
            Assert.AreEqual(string.Empty, svc.GetUsername(e));
        }

        // ── GetUrl ────────────────────────────────────────────────────────────

        [TestMethod]
        public void GetUrl_ReturnsCorrectValue()
        {
            var svc = new CredentialService();
            var e   = MakeEntry(url: "https://example.com");

            Assert.AreEqual("https://example.com", svc.GetUrl(e));
        }

        [TestMethod]
        public void GetUrl_NullEntry_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, new CredentialService().GetUrl(null));
        }

        // ── GetPassword ───────────────────────────────────────────────────────

        [TestMethod]
        public void GetPassword_ReturnsProtectedString()
        {
            var svc = new CredentialService();
            var e   = MakeEntry(password: "s3cr3t");

            ProtectedString ps = svc.GetPassword(e);
            Assert.IsNotNull(ps);
            Assert.AreEqual("s3cr3t", ps.ReadString());
        }

        [TestMethod]
        public void GetPassword_NullEntry_ReturnsEmpty()
        {
            ProtectedString ps = new CredentialService().GetPassword(null);
            Assert.IsNotNull(ps);
            Assert.AreEqual(string.Empty, ps.ReadString());
        }

        // ── GetNotes ──────────────────────────────────────────────────────────

        [TestMethod]
        public void GetNotes_ReturnsCorrectValue()
        {
            var svc = new CredentialService();
            var e   = MakeEntry(notes: "my notes");

            Assert.AreEqual("my notes", svc.GetNotes(e));
        }

        [TestMethod]
        public void GetNotes_NullEntry_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, new CredentialService().GetNotes(null));
        }

        // ── Save ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void Save_SetsDbModifiedTrue()
        {
            var svc = new CredentialService();
            var db  = OpenDb();
            var e   = MakeEntry();
            db.Modified = false;

            svc.Save(e, db);

            Assert.IsTrue(db.Modified);
        }

        [TestMethod]
        public void Save_UpdatesLastModificationTime()
        {
            var svc    = new CredentialService();
            var db     = OpenDb();
            var e      = MakeEntry();
            var before = e.LastModificationTime;

            System.Threading.Thread.Sleep(10);
            svc.Save(e, db);

            Assert.IsTrue(e.LastModificationTime >= before);
        }

        [TestMethod]
        public void Save_NullEntry_DoesNotThrow()
        {
            var db = OpenDb();
            new CredentialService().Save(null, db); // should not throw
        }

        [TestMethod]
        public void Save_NullDb_DoesNotThrow()
        {
            var e = MakeEntry();
            new CredentialService().Save(e, null); // should not throw
        }
    }
}
