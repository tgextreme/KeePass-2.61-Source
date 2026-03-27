using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Delegates;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class HibpServiceTests
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

        private static PwEntry AddEntry(PwDatabase db, string password,
            string title = "entry")
        {
            var pe = new PwEntry(true, true);
            pe.Strings.Set(PwDefs.TitleField,
                new ProtectedString(false, title));
            pe.Strings.Set(PwDefs.PasswordField,
                new ProtectedString(true, password));
            db.RootGroup.AddEntry(pe, true);
            return pe;
        }

        // ── ArgumentNullException ─────────────────────────────────────

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void CheckAll_NullDb_ThrowsArgumentNullException()
        {
            HibpService.CheckAll(null, null, null);
        }

        // ── BD vacía (sin entradas) ────────────────────────────────────

        [TestMethod]
        public void CheckAll_EmptyDb_ReturnsEmptyList()
        {
            var db = OpenDb();
            // Sin entradas → no hay llamadas de red → devuelve lista vacía
            List<PwnedEntry> result = HibpService.CheckAll(db, null, null);
            // La BD vacía tiene 0 entradas, así que el bucle no hace nada.
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── Cancelación inmediata ──────────────────────────────────────

        [TestMethod]
        public void CheckAll_ImmediateCancel_ReturnsEmptyList_WithNoNetworkCalls()
        {
            var db = OpenDb();
            AddEntry(db, "some_password");

            // Cancelar desde la primera iteración — el código hace break antes de llamar la red
            GFunc<bool> cancelImmediately = delegate() { return true; };

            List<PwnedEntry> result = HibpService.CheckAll(db, null, cancelImmediately);

            // El resultado debe ser vacío porque se canceló antes de cualquier petición HTTP
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count,
                "Con cancelación inmediata no debe encontrarse ninguna entrada comprometida");
        }

        // ── Entradas sin contraseña ────────────────────────────────────

        [TestMethod]
        public void CheckAll_EntriesWithEmptyPasswords_SkipsNetworkCall()
        {
            var db = OpenDb();
            // Entrada con contraseña vacía → se omite en el bucle (continue)
            var pe = new PwEntry(true, true);
            // No establecemos PwDefs.PasswordField → ReadSafe devolverá string.Empty
            db.RootGroup.AddEntry(pe, true);

            // Sin contraseña → no hay llamadas de red posibles.
            // La única forma de hacer que esto suponga una llamada de red sería
            // que ReadSafe devolviera algo no vacío, lo cual no ocurre aquí.
            // La llamada a CheckAll debe completarse sin errores.
            List<PwnedEntry> result = HibpService.CheckAll(db, null, null);
            Assert.IsNotNull(result);
        }

        // ── Callback de progreso ───────────────────────────────────────

        [TestMethod]
        public void CheckAll_EmptyDb_ProgressCallback_CalledWithFinalState()
        {
            var db = OpenDb(); // sin entradas

            int lastCurrent = -1, lastTotal = -1;
            GAction<int, int> progress = delegate(int current, int total)
            {
                lastCurrent = current;
                lastTotal   = total;
            };

            HibpService.CheckAll(db, progress, null);

            // La BD está vacía: total = 0, el callback reporta (0, 0)
            Assert.AreEqual(0, lastCurrent, "current debe ser 0 para BD vacía");
            Assert.AreEqual(0, lastTotal,   "total debe ser 0 para BD vacía");
        }

        [TestMethod]
        public void CheckAll_NullProgressCallback_DoesNotThrow()
        {
            var db = OpenDb();
            // Debe completarse sin NullReferenceException
            HibpService.CheckAll(db, null, null);
        }

        // ── PwnedEntry estructura ─────────────────────────────────────

        [TestMethod]
        public void PwnedEntry_CountField_IsAccessible()
        {
            // Verificar que PwnedEntry es usable como struct
            var p = new PwnedEntry();
            p.Entry = new PwEntry(true, true);
            p.Count = 42;

            Assert.IsNotNull(p.Entry);
            Assert.AreEqual(42L, p.Count);
        }
    }
}
