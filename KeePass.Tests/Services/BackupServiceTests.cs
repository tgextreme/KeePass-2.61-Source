using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.App.Configuration;
using KeePass.Services;

namespace KeePass.Tests.Services
{
    [TestClass]
    public class BackupServiceTests
    {
        // ── GetBackupDir ──────────────────────────────────────────────

        [TestMethod]
        public void GetBackupDir_UsesCustomFolder_WhenConfigured()
        {
            string customFolder = @"C:\MyBackups";
            var cfg = new AceBackup { Folder = customFolder };

            string result = BackupService.GetBackupDir(@"C:\data\vault.kdbx", cfg);

            Assert.AreEqual(customFolder, result);
        }

        [TestMethod]
        public void GetBackupDir_UsesDefaultSubfolder_WhenFolderIsEmpty()
        {
            var cfg = new AceBackup { Folder = string.Empty };
            string srcPath = @"C:\data\vault.kdbx";

            string result = BackupService.GetBackupDir(srcPath, cfg);

            string expected = Path.Combine(@"C:\data", "KeePassBackups");
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetBackupDir_UsesDefaultSubfolder_WhenFolderIsWhitespace()
        {
            var cfg = new AceBackup { Folder = "   " };
            string srcPath = @"C:\data\vault.kdbx";

            string result = BackupService.GetBackupDir(srcPath, cfg);

            string expected = Path.Combine(@"C:\data", "KeePassBackups");
            Assert.AreEqual(expected, result);
        }

        // ── PurgeOldBackups ───────────────────────────────────────────

        private static string CreateTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "KeePassTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// Creates <paramref name="count"/> dummy backup files with controlled timestamps.
        private static void CreateBackupFiles(string dir, string baseName, string ext, int count)
        {
            for(int i = 0; i < count; i++)
            {
                string name = $"{baseName}.{i:D4}{ext}";
                string path = Path.Combine(dir, name);
                File.WriteAllText(path, "backup");
                // Spread LastWriteTime so ordering is deterministic
                File.SetLastWriteTime(path, DateTime.Now.AddMinutes(-count + i));
            }
        }

        [TestMethod]
        public void PurgeOldBackups_KeepsExactlyMaxKeepFiles()
        {
            string dir = CreateTempDir();
            try
            {
                CreateBackupFiles(dir, "vault", ".kdbx", 8);
                BackupService.PurgeOldBackups(dir, "vault", ".kdbx", maxKeep: 5);
                int remaining = Directory.GetFiles(dir).Length;
                Assert.AreEqual(5, remaining);
            }
            finally { Directory.Delete(dir, true); }
        }

        [TestMethod]
        public void PurgeOldBackups_DeletesOldestFiles()
        {
            string dir = CreateTempDir();
            try
            {
                CreateBackupFiles(dir, "vault", ".kdbx", 6);
                // Identify the 4 newest before purge (highest index = newest by our timestamp logic)
                var before = Directory.GetFiles(dir);
                Array.Sort(before, (a, b) =>
                    File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                string[] expectedKept = new string[4];
                Array.Copy(before, 0, expectedKept, 0, 4);

                BackupService.PurgeOldBackups(dir, "vault", ".kdbx", maxKeep: 4);

                var after = Directory.GetFiles(dir);
                Assert.AreEqual(4, after.Length);
                foreach(string kept in expectedKept)
                    Assert.IsTrue(File.Exists(kept),
                        $"El archivo más reciente debería haberse conservado: {kept}");
            }
            finally { Directory.Delete(dir, true); }
        }

        [TestMethod]
        public void PurgeOldBackups_DoesNothing_WhenBelowMaxKeep()
        {
            string dir = CreateTempDir();
            try
            {
                CreateBackupFiles(dir, "vault", ".kdbx", 3);
                BackupService.PurgeOldBackups(dir, "vault", ".kdbx", maxKeep: 10);
                Assert.AreEqual(3, Directory.GetFiles(dir).Length);
            }
            finally { Directory.Delete(dir, true); }
        }

        [TestMethod]
        public void PurgeOldBackups_DoesNothing_WhenDirectoryDoesNotExist()
        {
            // Should not throw
            BackupService.PurgeOldBackups(@"C:\NonExistentDir_XqZ", "vault", ".kdbx", maxKeep: 5);
        }

        [TestMethod]
        public void PurgeOldBackups_OnlyPurgesMatchingExtension()
        {
            string dir = CreateTempDir();
            try
            {
                // 5 .kdbx files + 3 unrelated files
                CreateBackupFiles(dir, "vault", ".kdbx", 5);
                CreateBackupFiles(dir, "other", ".txt", 3);

                BackupService.PurgeOldBackups(dir, "vault", ".kdbx", maxKeep: 3);

                int kdbxCount = Directory.GetFiles(dir, "*.kdbx").Length;
                int txtCount  = Directory.GetFiles(dir, "*.txt").Length;

                Assert.AreEqual(3, kdbxCount, ".kdbx debería quedar en 3");
                Assert.AreEqual(3, txtCount,  ".txt no debe tocarse");
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
