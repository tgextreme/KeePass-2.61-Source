// SecurityService — Anti-screenshot y streamer mode (ISecurityService)

using System.Windows.Forms;
using KeePass.Infrastructure.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Default implementation of <see cref="ISecurityService"/>.
	/// Delegates to <see cref="NativeSecurityHelper"/> for Win32 calls.
	/// </summary>
	public sealed class SecurityService : ISecurityService
	{
		private bool m_streamerMode;

		public bool IsStreamerModeActive { get { return m_streamerMode; } }

		public void EnableAntiScreenshot(Form form)
		{
			NativeSecurityHelper.EnableAntiScreenshot(form);
		}

		public void DisableAntiScreenshot(Form form)
		{
			NativeSecurityHelper.DisableAntiScreenshot(form);
		}

		public void EnableStreamerMode(Form mainForm)
		{
			m_streamerMode = true;
			if(mainForm != null)
				NativeSecurityHelper.EnableAntiScreenshot(mainForm);
		}

		public void DisableStreamerMode(Form mainForm)
		{
			m_streamerMode = false;
			if(mainForm != null)
				NativeSecurityHelper.DisableAntiScreenshot(mainForm);
		}
	}
}
