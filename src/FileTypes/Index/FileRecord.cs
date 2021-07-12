using PoETool.FileTypes.Utils;

namespace PoETool.FileTypes.Index {
	/// <summary>
	/// A file record that references a slice inside a <see cref="Bundle.BundleFile"/> for its content.<br/>
	/// Always part of an <see cref="IndexFile"/> for the <see cref="BundleIndex"/>.
	/// </summary>
	public class FileRecord {
		public ulong Hash { get; }
		public int BundleIndex { get; }
		public int Offset { get; }
		public int Size { get; }
		public string Path { get; set; }

		public static ulong CalculateHash(string filePath) => FNV.FNV1a_64(filePath.ToLower() + "++");

		public FileRecord(ref MemoryReader reader) {
			Hash = reader.ReadUInt64();
			BundleIndex = reader.ReadInt32();
			Offset = reader.ReadInt32();
			Size = reader.ReadInt32();
		}
	}
}
