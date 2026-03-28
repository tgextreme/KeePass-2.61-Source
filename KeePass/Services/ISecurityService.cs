// ISecurityService — Anti-screenshot y streamer mode

using System.Windows.Forms;

namespace KeePass.Services
{
	/// <summary>
	/// Runtime security controls (anti-screenshot, streamer mode).
	/// </summary>
	public interface ISecurityService
	{
		/// <summary>Gets whether streamer/anti-screenshot mode is active.</summary>
		bool IsStreamerModeActive { get; }

		/// <summary>Enables anti-screenshot on the given form.</summary>
		void EnableAntiScreenshot(Form form);

		/// <summary>Removes anti-screenshot protection from the given form.</summary>
		void DisableAntiScreenshot(Form form);

		/// <summary>Enables streamer mode (applies anti-screenshot to MainForm
		/// and sets a flag for other windows).</summary>
		void EnableStreamerMode(Form mainForm);

		/// <summary>Disables streamer mode and removes protection from MainForm.</summary>
		void DisableStreamerMode(Form mainForm);
	}
}
