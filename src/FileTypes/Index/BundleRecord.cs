using System.IO;
using System.Text;

namespace PoETool.FileTypes.Index {
	public class BundleRecord {
		public int UncompressedSize { get; }
		public string Name { get; }

		public BundleRecord(BinaryReader reader) {
			var nameLength = reader.ReadInt32();
			Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength)) + ".bundle.bin";
			UncompressedSize = reader.ReadInt32();
		}
	}
}
