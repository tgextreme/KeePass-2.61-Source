using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class ExpiryServiceTests
    {
        // ── helpers ───────────────────────────────────────────────────

        /// Crea una base de datos en memoria completamente abierta.
        private static PwDatabase OpenDb()
        {
            var db  = new PwDatabase();
            var key = new CompositeKey();
            key.AddUserKey(new KcpPassword("test"));
            db.New(new IOConnectionInfo(), key);
            return db;
        }

        /// Crea una entrada con expiración configurada y la añade al grupo raíz.
        private static PwEntry AddEntry(PwDatabase db, DateTime expiryUtc,
            bool expires = true, string title = "entry")
        {
            var pe = new PwEntry(true, true);
            pe.Strings.Set(PwDefs.TitleField,
                new KeePassLib.Security.ProtectedString(false, title));
            pe.Expires    = expires;
            pe.ExpiryTime = expiryUtc;
            db.RootGroup.AddEntry(pe, true);
            return pe;
        }

        // ── GetExpiredEntries ─────────────────────────────────────────

        [TestMethod]
        public void GetExpiredEntries_NullDb_ReturnsEmpty()
        {
            var result = ExpiryService.GetExpiredEntries(null);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiredEntries_EmptyDb_ReturnsEmpty()
        {
            var db = OpenDb();
            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiredEntries_ExpiredEntry_IsIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-1)); // expiró ayer

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void GetExpiredEntries_FutureEntry_IsNotIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(10)); // expira en 10 días

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiredEntries_NonExpiringEntry_IsNotIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-1), expires: false);

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiredEntries_DaysRemaining_IsNegative()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-5)); // expiró hace ~5 días

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(1,    result.Count);
            Assert.IsTrue(result[0].DaysRemaining <= 0,
                "DaysRemaining debe ser negativo para una entrada expirada");
        }

        [TestMethod]
        public void GetExpiredEntries_MultipleEntries_OnlyExpiredIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-2), title: "expired");
            AddEntry(db, DateTime.UtcNow.AddDays(+5), title: "future");
            AddEntry(db, DateTime.UtcNow.AddDays(-1), expires: false, title: "no_expiry");

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("expired",
                result[0].Entry.Strings.ReadSafe(PwDefs.TitleField));
        }

        [TestMethod]
        public void GetExpiredEntries_ReturnsCorrectCount()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-1));
            AddEntry(db, DateTime.UtcNow.AddDays(-2));
            AddEntry(db, DateTime.UtcNow.AddDays(-3));

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(3, result.Count);
        }

        // ── GetExpiringSoon ────────────────────────────────────────────

        [TestMethod]
        public void GetExpiringSoon_NullDb_ReturnsEmpty()
        {
            var result = ExpiryService.GetExpiringSoon(null, 7);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiringSoon_ZeroDays_ReturnsEmpty()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(1));

            var result = ExpiryService.GetExpiringSoon(db, 0);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiringSoon_EntryExpiresWithinWindow_IsIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(3)); // expira en 3 días → dentro de ventana de 7

            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void GetExpiringSoon_EntryExpiresOutsideWindow_IsNotIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(30)); // expira en 30 días → fuera de ventana de 7

            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiringSoon_AlreadyExpiredEntry_IsNotIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-1)); // ya expiró

            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(0, result.Count,
                "GetExpiringSoon no debe incluir entradas ya expiradas");
        }

        [TestMethod]
        public void GetExpiringSoon_NonExpiringEntry_IsNotIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(3), expires: false);

            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetExpiringSoon_DaysRemaining_IsPositive()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(5));

            var result = ExpiryService.GetExpiringSoon(db, 14);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].DaysRemaining >= 0,
                "DaysRemaining debe ser >= 0 para una entrada que expira próximamente");
        }

        [TestMethod]
        public void GetExpiringSoon_MixedEntries_OnlyExpiringSoonIncluded()
        {
            var db = OpenDb();
            AddEntry(db, DateTime.UtcNow.AddDays(-1),  title: "expired");    // ya expiró
            AddEntry(db, DateTime.UtcNow.AddDays(+3),  title: "soon");       // dentro de 7d
            AddEntry(db, DateTime.UtcNow.AddDays(+30), title: "far");        // fuera de 7d
            AddEntry(db, DateTime.UtcNow.AddDays(+2),  expires: false, title: "never"); // sin expiración

            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("soon",
                result[0].Entry.Strings.ReadSafe(PwDefs.TitleField));
        }

        [TestMethod]
        public void GetExpiringSoon_EmptyDb_ReturnsEmpty()
        {
            var db = OpenDb();
            var result = ExpiryService.GetExpiringSoon(db, 7);
            Assert.AreEqual(0, result.Count);
        }

        // ── ExpiryItem properties ──────────────────────────────────────

        [TestMethod]
        public void ExpiryItem_EntryReference_IsPreserved()
        {
            var db = OpenDb();
            var pe = AddEntry(db, DateTime.UtcNow.AddDays(-1));

            var result = ExpiryService.GetExpiredEntries(db);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(pe, result[0].Entry);
        }
    }
}
