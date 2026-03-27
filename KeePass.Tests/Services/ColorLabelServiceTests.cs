using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class ColorLabelServiceTests
    {
        private static PwEntry NewEntry() => new PwEntry(true, true);

        // ── GetColor / SetColor ────────────────────────────────────────

        [TestMethod]
        public void NewEntry_HasNoColor()
        {
            var pe = NewEntry();
            Assert.IsNull(ColorLabelService.GetColor(pe));
        }

        [TestMethod]
        public void GetColor_NullEntry_ReturnsNull()
        {
            Assert.IsNull(ColorLabelService.GetColor(null));
        }

        [TestMethod]
        public void SetColor_StoresColorAsHex()
        {
            var pe = NewEntry();
            Color red = Color.FromArgb(228, 87, 86);
            ColorLabelService.SetColor(pe, red);

            Color? stored = ColorLabelService.GetColor(pe);
            Assert.IsNotNull(stored, "Debe haber un color guardado");
            Assert.AreEqual(red.R, stored.Value.R);
            Assert.AreEqual(red.G, stored.Value.G);
            Assert.AreEqual(red.B, stored.Value.B);
        }

        [TestMethod]
        public void SetColor_Null_RemovesColor()
        {
            var pe = NewEntry();
            ColorLabelService.SetColor(pe, Color.Red);
            Assert.IsNotNull(ColorLabelService.GetColor(pe)); // precondición

            ColorLabelService.SetColor(pe, null);
            Assert.IsNull(ColorLabelService.GetColor(pe));
        }

        [TestMethod]
        public void SetColor_NullEntry_DoesNotThrow()
        {
            // No debe lanzar excepción
            ColorLabelService.SetColor(null, Color.Red);
        }

        [TestMethod]
        public void SetColor_Overwrite_UpdatesColor()
        {
            var pe = NewEntry();
            ColorLabelService.SetColor(pe, Color.FromArgb(255, 0, 0));
            ColorLabelService.SetColor(pe, Color.FromArgb(0, 255, 0));

            Color? stored = ColorLabelService.GetColor(pe);
            Assert.IsNotNull(stored);
            Assert.AreEqual(0,   stored.Value.R);
            Assert.AreEqual(255, stored.Value.G);
            Assert.AreEqual(0,   stored.Value.B);
        }

        [TestMethod]
        public void SetColor_AllPredefinedColors_RoundTrip()
        {
            foreach (Color clr in ColorLabelService.PredefinedColors)
            {
                var pe = NewEntry();
                ColorLabelService.SetColor(pe, clr);
                Color? stored = ColorLabelService.GetColor(pe);

                Assert.IsNotNull(stored, $"Color {clr} no se recuperó");
                Assert.AreEqual(clr.R, stored.Value.R, $"R mismatch para {clr}");
                Assert.AreEqual(clr.G, stored.Value.G, $"G mismatch para {clr}");
                Assert.AreEqual(clr.B, stored.Value.B, $"B mismatch para {clr}");
            }
        }

        // ── Blend ─────────────────────────────────────────────────────

        [TestMethod]
        public void Blend_White_ReturnsWhite()
        {
            Color result = ColorLabelService.Blend(Color.White);
            // Blending white with white → white (255,255,255)
            Assert.AreEqual(255, result.R);
            Assert.AreEqual(255, result.G);
            Assert.AreEqual(255, result.B);
        }

        [TestMethod]
        public void Blend_ReturnsLighterColor()
        {
            Color dark = Color.FromArgb(0, 0, 0); // negro
            Color blended = ColorLabelService.Blend(dark);
            // Al mezclar negro con blanco al 40 %, el resultado debe ser claro
            Assert.IsTrue(blended.R > 100, "El rojo mezclado debe ser mayor que 100");
            Assert.IsTrue(blended.G > 100, "El verde mezclado debe ser mayor que 100");
            Assert.IsTrue(blended.B > 100, "El azul mezclado debe ser mayor que 100");
        }

        [TestMethod]
        public void Blend_OutputIsLighterThanInput()
        {
            Color saturated = Color.FromArgb(200, 50, 50);
            Color blended = ColorLabelService.Blend(saturated);
            // El resultado pastel debe ser más claro (mayor valor total)
            int inputBrightness  = saturated.R + saturated.G + saturated.B;
            int outputBrightness = blended.R   + blended.G   + blended.B;
            Assert.IsTrue(outputBrightness > inputBrightness,
                "El color mezclado debe ser más claro que el original saturado");
        }

        [TestMethod]
        public void Blend_AlphaIs255()
        {
            Color result = ColorLabelService.Blend(Color.Red);
            Assert.AreEqual(255, result.A, "El color mezclado debe ser totalmente opaco");
        }

        // ── PredefinedColors / PredefinedColorNames ────────────────────

        [TestMethod]
        public void PredefinedColors_HasEightItems()
        {
            Assert.AreEqual(8, ColorLabelService.PredefinedColors.Length);
        }

        [TestMethod]
        public void PredefinedColorNames_HasEightItems()
        {
            Assert.AreEqual(8, ColorLabelService.PredefinedColorNames.Length);
        }

        [TestMethod]
        public void PredefinedColors_And_Names_SameLength()
        {
            Assert.AreEqual(
                ColorLabelService.PredefinedColors.Length,
                ColorLabelService.PredefinedColorNames.Length);
        }

        [TestMethod]
        public void PredefinedColorNames_NoneNullOrEmpty()
        {
            foreach (string name in ColorLabelService.PredefinedColorNames)
                Assert.IsFalse(string.IsNullOrEmpty(name), "Nombre de color no debe ser vacío");
        }
    }
}
