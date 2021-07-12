using PoETool.FileTypes.Utils;

namespace PoETool.FileTypes.Index {
	/// <summary>
	/// A bundle record that references a <see cref="Bundle.BundleFile"/> by name.<br/>
	/// Always part of an <see cref="IndexFile"/>.
	/// </summary>
	public class BundleRecord {
		public int UncompressedSize { get; }
		public string Name { get; }

		public BundleRecord(ref MemoryReader reader) {
			Name = reader.ReadString(reader.ReadInt32()) + ".bundle.bin";
			UncompressedSize = reader.ReadInt32();
		}
	}
}
