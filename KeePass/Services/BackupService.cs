/*
  KeePass Modern Vibe — Automatic Backup Service (F8)

  Every time the database is saved successfully this service copies the
  .kdbx file to a backup folder with a timestamp suffix, then prunes
  old backups so that at most MaxKeep copies are kept.

  Only local-file databases are backed up (IOConnectionInfo.IsLocalFile()).
*/

using System;
using System.IO;
using System.Linq;

using KeePass.App.Configuration;
using KeePass.Forms;

namespace KeePass.Services
{
	public static class BackupService
	{
		// Subscribed to MainForm.FileSaved in OnFormLoad.
		public static void OnDatabaseSaved(object sender, FileSavedEventArgs e)
		{
			if(e == null || !e.Success) return;
			if(e.Database == null) return;

			AceBackup cfg = Program.Config.Backup;
			if(!cfg.Enabled || !cfg.BackupOnSave) return;

			var ioc = e.Database.IOConnectionInfo;
			if(ioc == null) return;

			// Only back up local files (no HTTP/FTP connections)
			if(!ioc.IsLocalFile()) return;

			string srcPath = ioc.Path;
			if(!File.Exists(srcPath)) return;

			try
			{
				string backupDir = GetBackupDir(srcPath, cfg);
				Directory.CreateDirectory(backupDir);

				string fileName  = Path.GetFileNameWithoutExtension(srcPath);
				string ext       = Path.GetExtension(srcPath);  // ".kdbx"
				string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				string destPath  = Path.Combine(backupDir,
					string.Concat(fileName, ".", timestamp, ext));

				File.Copy(srcPath, destPath, overwrite: false);

				PurgeOldBackups(backupDir, fileName, ext, cfg.MaxKeep);
			}
			catch(Exception) { /* Never interrupt the user for a backup failure */ }
		}

		// ── Helpers ──────────────────────────────────────────────────────────────

		public static string GetBackupDir(string srcPath, AceBackup cfg)
		{
			if(cfg.Folder != null && cfg.Folder.Trim().Length > 0)
				return cfg.Folder.Trim();

			// Default: subfolder "KeePassBackups" next to the .kdbx file
			string dbDir = Path.GetDirectoryName(srcPath) ?? string.Empty;
			return Path.Combine(dbDir, "KeePassBackups");
		}

		public static void PurgeOldBackups(string dir, string baseName,
			string ext, int maxKeep)
		{
			if(!Directory.Exists(dir)) return;

			// Pattern: "BaseName.YYYYMMDD_HHMMSS.kdbx"
			string pattern = baseName + ".*" + ext;

			var files = Directory.GetFiles(dir, pattern)
				.OrderByDescending(f => f)  // lexicographic = chronological for our timestamps
				.ToArray();

			for(int i = maxKeep; i < files.Length; i++)
			{
				try { File.Delete(files[i]); }
				catch(Exception) { }
			}
		}
	}
}
