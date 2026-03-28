// F1 — HIBP HTTP Client
// Realiza peticiones a https://api.pwnedpasswords.com/range/{prefix5}
// usando k-anonimato: nunca se envía el hash completo.

using System;
using System.IO;
using System.Net;

namespace KeePass.Infrastructure.Security
{
	/// <summary>
	/// Thin HTTP wrapper for the HaveIBeenPwned Passwords API v3.
	/// Only the first 5 hex characters of the SHA-1 hash are sent over the wire.
	/// </summary>
	public static class HibpClient
	{
		private const string ApiBase    = "https://api.pwnedpasswords.com/range/";
		private const string UserAgent  = "KeePassModernVibe/2.61";
		private const int    TimeoutMs  = 10000;   // 10 s per request

		/// <summary>
		/// Queries HIBP for all SHA-1 hashes that share the given 5-char prefix.
		/// Returns the raw response text (one "SUFFIX:COUNT" entry per line).
		/// Throws <see cref="WebException"/> on network errors.
		/// </summary>
		public static string QueryRange(string hashPrefix5)
		{
			if(hashPrefix5 == null || hashPrefix5.Length != 5)
				throw new ArgumentException("hashPrefix5 must be exactly 5 hex characters.");

			string url = ApiBase + hashPrefix5.ToUpperInvariant();

			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Method       = "GET";
			req.UserAgent    = UserAgent;
			req.Timeout      = TimeoutMs;
			req.ReadWriteTimeout = TimeoutMs;
			req.CachePolicy  = new System.Net.Cache.RequestCachePolicy(
				System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

			using(WebResponse resp = req.GetResponse())
			using(Stream      stream = resp.GetResponseStream())
			using(StreamReader reader = new StreamReader(stream,
				System.Text.Encoding.ASCII))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
