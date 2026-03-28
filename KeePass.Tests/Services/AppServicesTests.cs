using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Services;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class AppServicesTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Ensure clean state between tests
            AppServices.Shutdown();
        }

        [TestMethod]
        public void IsInitialized_IsFalse_BeforeInitialize()
        {
            Assert.IsFalse(AppServices.IsInitialized);
            Assert.IsNull(AppServices.BrowserImport);
        }

        [TestMethod]
        public void Initialize_SetsIsInitializedTrue()
        {
            AppServices.Initialize();

            Assert.IsTrue(AppServices.IsInitialized);
        }

        [TestMethod]
        public void Initialize_CreatesBrowserImportService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.BrowserImport);
        }

        [TestMethod]
        public void Initialize_CreatesCredentialsService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Credentials);
        }

        [TestMethod]
        public void Initialize_CreatesSearchService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Search);
        }

        [TestMethod]
        public void Initialize_CreatesClipboardService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Clipboard);
        }

        [TestMethod]
        public void Initialize_CreatesSecurityService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Security);
        }

        [TestMethod]
        public void Initialize_CreatesAnalysisService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Analysis);
        }

        [TestMethod]
        public void Initialize_CreatesDashboardService()
        {
            AppServices.Initialize();

            Assert.IsNotNull(AppServices.Dashboard);
        }

        [TestMethod]
        public void Shutdown_ClearsState()
        {
            AppServices.Initialize();
            AppServices.Shutdown();

            Assert.IsFalse(AppServices.IsInitialized);
            Assert.IsNull(AppServices.BrowserImport);
            Assert.IsNull(AppServices.Credentials);
            Assert.IsNull(AppServices.Search);
            Assert.IsNull(AppServices.Clipboard);
            Assert.IsNull(AppServices.Security);
            Assert.IsNull(AppServices.Analysis);
            Assert.IsNull(AppServices.Dashboard);
        }

        [TestMethod]
        public void Initialize_CanBeCalledTwice_Reinitializes()
        {
            AppServices.Initialize();
            var first = AppServices.BrowserImport;

            AppServices.Initialize();
            var second = AppServices.BrowserImport;

            Assert.IsTrue(AppServices.IsInitialized);
            Assert.IsNotNull(second);
            // Second call must create a fresh instance
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void Initialize_WithNullDb_Succeeds()
        {
            AppServices.Initialize(db: null);

            Assert.IsTrue(AppServices.IsInitialized);
            Assert.IsNotNull(AppServices.BrowserImport);
        }
    }
}
