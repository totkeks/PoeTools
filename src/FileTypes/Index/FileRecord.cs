using System.IO;

namespace PoETool.FileTypes.Index {
	public class FileRecord {
		public ulong Hash { get; }
		public int BundleIndex { get; }
		public int Offset { get; }
		public int Size { get; }
		public string Path { get; set; }

		public FileRecord(BinaryReader reader) {
			Hash = reader.ReadUInt64();
			BundleIndex = reader.ReadInt32();
			Offset = reader.ReadInt32();
			Size = reader.ReadInt32();
		}
	}
}
