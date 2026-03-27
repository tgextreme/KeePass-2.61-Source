using System.Windows.Forms;

using KeePass.App;
using KeePass.App.Configuration;
using KeePass.Forms;
using KeePass.UI.Background;
using KeePass.Util;

namespace KeePass.Infrastructure.Background
{
	/// <summary>
	/// Orchestrates the background-mode lifecycle:
	///   • Registers / unregisters the mini-search global hotkey
	///   • Enables/disables Windows startup entry
	///   • Forces MinimizeToTray when BackgroundMode.MinimizeToTray is set
	/// </summary>
	public static class BackgroundModeManager
	{
		private static MainForm g_mainForm = null;

		public static bool IsActive
		{
			get
			{
				AceBackgroundMode cfg = Program.Config.BackgroundMode;
				return (cfg != null && cfg.RunInBackground);
			}
		}

		public static void Start(MainForm mf)
		{
			g_mainForm = mf;

			AceBackgroundMode cfg = Program.Config.BackgroundMode;
			if(cfg == null || !cfg.RunInBackground) return;

			// Override the existing MinimizeToTray setting so our setting is authoritative
			if(cfg.MinimizeToTray)
				Program.Config.MainWindow.MinimizeToTray = true;

			// Register global hotkey for MiniSearchPopup
			HotKeyManager.RegisterHotKey(AppDefs.GlobalHotKeyId.MiniSearch,
				Keys.Control | Keys.Alt | Keys.K);

			// Start with Windows — enable/disable autostart registry entry
			if(cfg.StartWithWindows)
				WindowsStartupHelper.Enable(Application.ExecutablePath);
			else
				WindowsStartupHelper.Disable();

			// If the app was launched with --minimized flag, hide the window
			if(cfg.StartMinimized &&
				Program.CommandLineArgs[AppDefs.CommandLineOptions.Minimize] != null)
			{
				mf.Visible = false;
			}
		}

		public static void Stop()
		{
			HotKeyManager.UnregisterHotKey(AppDefs.GlobalHotKeyId.MiniSearch);
			MiniSearchPopup.CloseIfOpen();
		}
	}
}
