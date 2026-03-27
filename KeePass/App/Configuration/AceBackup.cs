/*
  KeePass Modern Vibe — Backup configuration (F8)
  Stored inside AppConfigEx.Backup; serialised to KeePass.config.xml.
*/

using System;

namespace KeePass.App.Configuration
{
	public sealed class AceBackup
	{
		public AceBackup() { }

		private bool m_enabled = true;
		public bool Enabled
		{
			get { return m_enabled; }
			set { m_enabled = value; }
		}

		// Folder where backups are saved. Empty = same folder as the .kdbx file.
		private string m_folder = string.Empty;
		public string Folder
		{
			get { return m_folder; }
			set { m_folder = (value ?? string.Empty); }
		}

		private int m_maxKeep = 10;
		public int MaxKeep
		{
			get { return m_maxKeep; }
			set { m_maxKeep = Math.Max(1, value); }
		}

		private bool m_backupOnSave = true;
		public bool BackupOnSave
		{
			get { return m_backupOnSave; }
			set { m_backupOnSave = value; }
		}
	}
}
