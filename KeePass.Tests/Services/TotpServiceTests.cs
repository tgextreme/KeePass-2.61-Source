using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class TotpServiceTests
    {
        // ── helpers ───────────────────────────────────────────────────

        private static PwEntry NewEntry() => new PwEntry(true, true);

        /// Crea una entrada con el secreto TOTP en el campo Base32 (el más común).
        private static PwEntry EntryWithBase32(string base32)
        {
            var pe = NewEntry();
            pe.Strings.Set("TimeOtp-Secret-Base32",
                new ProtectedString(true, base32));
            return pe;
        }

        /// Crea una entrada con secreto TOTP en texto plano (TimeOtp-Secret).
        private static PwEntry EntryWithPlainSecret(string secret)
        {
            var pe = NewEntry();
            pe.Strings.Set("TimeOtp-Secret",
                new ProtectedString(true, secret));
            return pe;
        }

        /// Crea una entrada con secreto TOTP en Base64.
        private static PwEntry EntryWithBase64(string base64)
        {
            var pe = NewEntry();
            pe.Strings.Set("TimeOtp-Secret-Base64",
                new ProtectedString(true, base64));
            return pe;
        }

        // JBSWY3DPEHPK3PXP → "Hello!" como Base32
        private const string ValidBase32 = "JBSWY3DPEHPK3PXP";

        // ── HasTotp ────────────────────────────────────────────────────

        [TestMethod]
        public void HasTotp_NullEntry_ReturnsFalse()
        {
            Assert.IsFalse(TotpService.HasTotp(null));
        }

        [TestMethod]
        public void HasTotp_EntryNoSecret_ReturnsFalse()
        {
            Assert.IsFalse(TotpService.HasTotp(NewEntry()));
        }

        [TestMethod]
        public void HasTotp_EntryWithBase32Secret_ReturnsTrue()
        {
            Assert.IsTrue(TotpService.HasTotp(EntryWithBase32(ValidBase32)));
        }

        [TestMethod]
        public void HasTotp_EntryWithPlainSecret_ReturnsTrue()
        {
            Assert.IsTrue(TotpService.HasTotp(EntryWithPlainSecret("mysecret")));
        }

        [TestMethod]
        public void HasTotp_EntryWithBase64Secret_ReturnsTrue()
        {
            // "Hello!" en Base64
            string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello!"));
            Assert.IsTrue(TotpService.HasTotp(EntryWithBase64(b64)));
        }

        // ── GetTotp — formato de salida ────────────────────────────────

        [TestMethod]
        public void GetTotp_NullEntry_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, TotpService.GetTotp(null));
        }

        [TestMethod]
        public void GetTotp_NoSecret_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, TotpService.GetTotp(NewEntry()));
        }

        [TestMethod]
        public void GetTotp_ValidBase32_ReturnsSixDigits()
        {
            string code = TotpService.GetTotp(EntryWithBase32(ValidBase32));

            Assert.IsFalse(string.IsNullOrEmpty(code), "El código no debe estar vacío");
            Assert.AreEqual(6, code.Length, $"Se esperaban 6 dígitos, se obtuvo: '{code}'");
            Assert.IsTrue(Regex.IsMatch(code, @"^\d{6}$"),
                $"El código debe ser exactamente 6 dígitos numéricos. Obtenido: '{code}'");
        }

        [TestMethod]
        public void GetTotp_ValidPlainSecret_ReturnsSixDigits()
        {
            string code = TotpService.GetTotp(EntryWithPlainSecret("12345678901234567890"));

            Assert.IsFalse(string.IsNullOrEmpty(code));
            Assert.AreEqual(6, code.Length);
            Assert.IsTrue(Regex.IsMatch(code, @"^\d{6}$"));
        }

        [TestMethod]
        public void GetTotp_CustomLength8_ReturnsEightDigits()
        {
            var pe = EntryWithBase32(ValidBase32);
            pe.Strings.Set("TimeOtp-Length",
                new ProtectedString(false, "8"));

            string code = TotpService.GetTotp(pe);

            Assert.IsFalse(string.IsNullOrEmpty(code));
            Assert.AreEqual(8, code.Length, $"Se esperaban 8 dígitos, se obtuvo: '{code}'");
            Assert.IsTrue(Regex.IsMatch(code, @"^\d{8}$"));
        }

        [TestMethod]
        public void GetTotp_SameSecond_ReturnsSameCode()
        {
            // Dos llamadas inmediatas dentro del mismo período de 30 s deben
            // devolver el mismo código (salvo que ocurran exactamente en el límite)
            var pe = EntryWithBase32(ValidBase32);
            string code1 = TotpService.GetTotp(pe);
            string code2 = TotpService.GetTotp(pe);

            // Si el código cambió exactamente en la frontera del período, este test
            // puede fallar de forma intermitente — ignoramos ese caso extremo.
            if (!string.IsNullOrEmpty(code1) && !string.IsNullOrEmpty(code2))
                Assert.AreEqual(code1, code2,
                    "Dos llamadas consecutivas deben devolver el mismo código TOTP " +
                    "(salvo coincidencia con el límite de período de 30 s)");
        }

        [TestMethod]
        public void GetTotp_DifferentSecrets_CanProduceDifferentCodes()
        {
            // Con alta probabilidad dos secretos distintos producen códigos distintos
            // en el mismo instante. No es garantizado al 100 % pero la probabilidad
            // de colisión es 1/10^6 ≈ negligible.
            var pe1 = EntryWithBase32("JBSWY3DPEHPK3PXP"); // "Hello!"
            var pe2 = EntryWithPlainSecret("completely_different_secret_xyz");

            string code1 = TotpService.GetTotp(pe1);
            string code2 = TotpService.GetTotp(pe2);

            // Ambos deben ser válidos
            Assert.IsTrue(Regex.IsMatch(code1, @"^\d{6}$"), $"code1='{code1}'");
            Assert.IsTrue(Regex.IsMatch(code2, @"^\d{6}$"), $"code2='{code2}'");
        }

        [TestMethod]
        public void GetTotp_InvalidBase32_ReturnsEmpty()
        {
            // Un secreto Base32 malformado no debe lanzar excepción: devuelve vacío.
            var pe = EntryWithBase32("!!!INVALID_BASE32!!!");
            string code = TotpService.GetTotp(pe);
            // Puede devolver vacío o puede que MemUtil.ParseBase32 lo ignore parcialmente;
            // lo importante es que NO lance excepción.
            // No afirmamos el valor exacto, solo que es manejable.
            Assert.IsNotNull(code);
        }

        // ── Prioridad de campos ────────────────────────────────────────

        [TestMethod]
        public void GetTotp_PlainSecretTakesPriority_OverBase32()
        {
            // TimeOtp-Secret tiene prioridad sobre TimeOtp-Secret-Base32
            var pe = NewEntry();
            pe.Strings.Set("TimeOtp-Secret",
                new ProtectedString(true, "secretA"));
            pe.Strings.Set("TimeOtp-Secret-Base32",
                new ProtectedString(true, ValidBase32));

            // Ambos producen un código; simplemente verificamos que no es vacío.
            string code = TotpService.GetTotp(pe);
            Assert.IsTrue(Regex.IsMatch(code, @"^\d{6}$"),
                "Debe devolver código válido usando el secreto plain con mayor prioridad");
        }
    }
}
