// IClipboardService — Portapapeles seguro con auto-limpieza

using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Secure clipboard operations with automatic clearing.
	/// </summary>
	public interface IClipboardService
	{
		/// <summary>
		/// Copies a protected string to the clipboard and schedules a clear
		/// after <paramref name="clearAfterSeconds"/> seconds.
		/// Pass 0 to disable auto-clear.
		/// </summary>
		void CopySecure(ProtectedString value, int clearAfterSeconds);

		/// <summary>Copies a plain string to the clipboard without auto-clear.</summary>
		void CopyPlain(string value);

		/// <summary>Immediately clears the clipboard.</summary>
		void Clear();

		/// <summary>Number of seconds remaining until the clipboard is auto-cleared.
		/// Returns 0 if no auto-clear is pending.</summary>
		int SecondsToClear { get; }
	}
}
