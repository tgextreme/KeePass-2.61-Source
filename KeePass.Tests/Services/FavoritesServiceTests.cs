using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;
using KeePassLib;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class FavoritesServiceTests
    {
        private static PwEntry NewEntry()
        {
            return new PwEntry(true, true);
        }

        // ── IsFavorite ────────────────────────────────────────────────

        [TestMethod]
        public void NewEntry_IsNotFavorite()
        {
            var pe = NewEntry();
            Assert.IsFalse(FavoritesService.IsFavorite(pe));
        }

        [TestMethod]
        public void IsFavorite_NullEntry_ReturnsFalse()
        {
            Assert.IsFalse(FavoritesService.IsFavorite(null));
        }

        // ── Toggle ────────────────────────────────────────────────────

        [TestMethod]
        public void Toggle_MarksEntryAsFavorite()
        {
            var pe = NewEntry();
            FavoritesService.Toggle(pe);
            Assert.IsTrue(FavoritesService.IsFavorite(pe));
        }

        [TestMethod]
        public void Toggle_Twice_UnmarksEntry()
        {
            var pe = NewEntry();
            FavoritesService.Toggle(pe);
            FavoritesService.Toggle(pe);
            Assert.IsFalse(FavoritesService.IsFavorite(pe));
        }

        [TestMethod]
        public void Toggle_NullEntry_DoesNotThrow()
        {
            // Should complete without exception
            FavoritesService.Toggle(null);
        }

        [TestMethod]
        public void Toggle_SetsCustomDataKey()
        {
            var pe = NewEntry();
            FavoritesService.Toggle(pe);
            Assert.AreEqual(FavoritesService.StarredValue,
                pe.CustomData.Get(FavoritesService.CustomDataKey));
        }

        [TestMethod]
        public void Toggle_RemovesCustomDataKey_OnUnstar()
        {
            var pe = NewEntry();
            FavoritesService.Toggle(pe); // star
            FavoritesService.Toggle(pe); // unstar
            Assert.AreNotEqual(FavoritesService.StarredValue,
                pe.CustomData.Get(FavoritesService.CustomDataKey) ?? string.Empty);
        }

        // ── Constants ─────────────────────────────────────────────────

        [TestMethod]
        public void CustomDataKey_HasExpectedValue()
        {
            Assert.AreEqual("KPMVibe.Starred", FavoritesService.CustomDataKey);
        }

        [TestMethod]
        public void StarredValue_IsOne()
        {
            Assert.AreEqual("1", FavoritesService.StarredValue);
        }

        // ── GetAll ────────────────────────────────────────────────────

        [TestMethod]
        public void GetAll_NullDatabase_ReturnsEmptyList()
        {
            var result = FavoritesService.GetAll(null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}
