namespace PathOfExileTools.FileTypes.Utils {
	using System.Text;

	/// <summary>
	/// Contains static methods for the <see href="https://en.wikipedia.org/wiki/Fowler–Noll–Vo_hash_function">Fowler-Noll-Vo hash function</see>.
	/// </summary>
	internal static class FNV {
		/// <summary>
		/// The offset for the FNV-1a 64 bit hash function.
		/// </summary>
		private const ulong FNV1A_64_OFFSET = 0xcbf2_9ce4_8422_2325;

		/// <summary>
		/// The prime for the FNV-1a 64 bit hash function.
		/// </summary>
		private const ulong FNV1A_64_PRIME = 0x100_0000_01b3;

		/// <summary>
		/// Calculates a 64-bit FNV-1a hash from an UTF-8 string.
		/// </summary>
		/// <param name="data">The UTF-8 encoded input string.</param>
		/// <returns>The hash value.</returns>
		public static ulong FNV1a_64(string data) {
			ulong hash = FNV1A_64_OFFSET;

			foreach (byte character in Encoding.UTF8.GetBytes(data)) {
				hash ^= character;
				hash *= FNV1A_64_PRIME;
			}

			return hash;
		}
	}
}
