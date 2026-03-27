using Microsoft.Win32;

namespace KeePass.Infrastructure.Background
{
	/// <summary>
	/// Manages the Windows startup registry entry (HKCU — no admin required).
	/// </summary>
	public static class WindowsStartupHelper
	{
		private const string RegKey  = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string AppName = "KeePassModernVibe";

		public static void Enable(string exePath)
		{
			try
			{
				using(RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true))
				{
					if(key != null)
						key.SetValue(AppName, "\"" + exePath + "\" --minimized");
				}
			}
			catch { }
		}

		public static void Disable()
		{
			try
			{
				using(RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true))
				{
					if(key != null && key.GetValue(AppName) != null)
						key.DeleteValue(AppName);
				}
			}
			catch { }
		}

		public static bool IsEnabled()
		{
			try
			{
				using(RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKey))
					return key?.GetValue(AppName) != null;
			}
			catch { return false; }
		}
	}
}
