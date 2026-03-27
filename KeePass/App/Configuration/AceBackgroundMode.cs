using System.ComponentModel;

namespace KeePass.App.Configuration
{
	public sealed class AceBackgroundMode
	{
		public AceBackgroundMode() { }

		[DefaultValue(true)]
		public bool RunInBackground  { get; set; } = true;

		[DefaultValue(false)]
		public bool StartWithWindows { get; set; } = false;

		[DefaultValue(false)]
		public bool StartMinimized   { get; set; } = false;

		[DefaultValue(true)]
		public bool MinimizeToTray   { get; set; } = true;

		[DefaultValue(true)]
		public bool CloseToTray      { get; set; } = true;

		[DefaultValue(5)]
		public int  ShowRecentCount  { get; set; } = 5;
	}
}
