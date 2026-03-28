// CredentialService — Implementación de ICredentialService

using KeePassLib;
using KeePassLib.Security;

namespace KeePass.Services
{
	/// <summary>
	/// Default implementation of <see cref="ICredentialService"/>.
	/// Delegates to the existing <c>PwDefs</c> field constants and
	/// <c>PwEntry.Strings</c> API to avoid duplicating logic.
	/// </summary>
	public sealed class CredentialService : ICredentialService
	{
		public string GetUsername(PwEntry entry)
		{
			if(entry == null) return string.Empty;
			return entry.Strings.ReadSafe(PwDefs.UserNameField);
		}

		public string GetUrl(PwEntry entry)
		{
			if(entry == null) return string.Empty;
			return entry.Strings.ReadSafe(PwDefs.UrlField);
		}

		public ProtectedString GetPassword(PwEntry entry)
		{
			if(entry == null) return ProtectedString.Empty;
			return entry.Strings.GetSafe(PwDefs.PasswordField);
		}

		public string GetNotes(PwEntry entry)
		{
			if(entry == null) return string.Empty;
			return entry.Strings.ReadSafe(PwDefs.NotesField);
		}

		public void Save(PwEntry entry, PwDatabase db)
		{
			if(entry == null || db == null) return;
			entry.Touch(true);  // updates LastModificationTime
			db.Modified = true;
		}
	}
}
