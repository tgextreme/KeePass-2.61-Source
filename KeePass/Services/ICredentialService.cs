// ICredentialService — Abstracción de acceso a credenciales

using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Provides safe access to a single entry's credential fields.
	/// Implementations must never return plaintext passwords as managed strings
	/// any longer than strictly necessary.
	/// </summary>
	public interface ICredentialService
	{
		/// <summary>Returns the username string for the given entry (unprotected).</summary>
		string GetUsername(PwEntry entry);

		/// <summary>Returns the URL field for the given entry.</summary>
		string GetUrl(PwEntry entry);

		/// <summary>Returns the password as a <see cref="ProtectedString"/>
		/// (still encrypted in memory).</summary>
		ProtectedString GetPassword(PwEntry entry);

		/// <summary>Returns the notes field (unprotected).</summary>
		string GetNotes(PwEntry entry);

		/// <summary>Persists modified entry data and marks the database as dirty.</summary>
		void Save(PwEntry entry, PwDatabase db);
	}
}
