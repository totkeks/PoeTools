using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PoeTools.Bundles.Lib {
	/// <summary>
	/// A binary bundle with a header and compressed data.<br/>
	/// Typically contained inside a <c>.bundle.bin</c> file, but can also come from memory.
	/// </summary>
	public class Bundle {
		private readonly string name;

		private int uncompressedSize;
		private int dataSize;
		/* private int headerSize; */

		/* private CompressionAlgorithm compressionAlgorithm; */
		/* private uint unknown1; */
		private long uncompressedSizeL;
		private long dataSizeL;
		private int blockCount;
		private int uncompressedBlockSize;
		/* private ulong unknown2; */
		/* private ulong unknown3; */
		private int[] blockSizes;

		private List<Memory<byte>> compressedDataBlocks;
		private Memory<byte> decompressedData;

		public string Name { get => name; }
		public int CompressedSize { get => dataSize; }
		public int UncompressedSize { get => uncompressedSize; }
		public int BlockCount { get => blockCount; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Bundle"/> class with data from a specified file.
		/// </summary>
		/// <param name="filePath">The path to the bundle file.</param>
		public Bundle(string filePath) : this(
			new FileStream(filePath, FileMode.Open),
			Path.GetFileName(filePath).Replace(".bundle.bin", "")
		) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Bundle"/> class with data from a stream.
		/// </summary>
		/// <param name="stream">The stream containing the bundle.</param>
		/// <param name="name">The name of the bundle.</param>
		public Bundle(Stream stream, string name = "-in-memory-") {
			this.name = name;

			using BinaryReader reader = new BinaryReader(stream);
			ReadPreamble(reader);
			ReadHeader(reader);
			ReadCompressedData(reader);

			// Sanity checks
			Debug.Assert(UncompressedSize == uncompressedSizeL, "Uncompressed sizes are not equal");
			Debug.Assert(dataSize == dataSizeL, "Data sizes are not equal");
			Debug.Assert(blockSizes.Sum() == dataSize, "Sum of blocks doesn't match data size");
			Debug.Assert(compressedDataBlocks.Count == blockCount, "Block count does not match");
			Debug.Assert(reader.Read() == -1, "End of file not reached after reading all blocks");
		}

		/// <summary>
		/// Gets the decompressed content of this bundle.
		/// </summary>
		/// <returns>A memory segment containing the decompressed bundle content.</returns>
		public ReadOnlyMemory<byte> GetContent() {
			return GetContent(0, UncompressedSize);
		}

		public ReadOnlyMemory<byte> GetContent(int block) {
			return GetContent(blockSizes[0..block].Sum(), blockSizes[block]);
		}

		/// <summary>
		/// Gets a slice of the decompressed content of this bundle.
		/// </summary>
		/// <param name="offset">The slice offset from the start of the content.</param>
		/// <param name="length">The slice length .</param>
		/// <returns>A memory segment containing the decompressed bundle content.</returns>
		public ReadOnlyMemory<byte> GetContent(int offset, int length) {
			var firstBlock = offset / uncompressedBlockSize;
			var lastBlock = (offset + length + uncompressedBlockSize - 1) / uncompressedBlockSize;

			DecompressBlocks(firstBlock, lastBlock);

			return decompressedData.Slice(offset, length);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format($"Bundle '{Name}' with {blockCount} blocks");
		}

		private void ReadPreamble(BinaryReader reader) {
			uncompressedSize = reader.ReadInt32();
			dataSize = reader.ReadInt32();
			reader.ReadInt32(); // headerSize
		}

		private void ReadHeader(BinaryReader reader) {
			reader.ReadUInt32(); // compressionAlgorithm
			reader.ReadUInt32(); // unknown1
			uncompressedSizeL = reader.ReadInt64();
			dataSizeL = reader.ReadInt64();
			blockCount = reader.ReadInt32();
			uncompressedBlockSize = reader.ReadInt32();
			reader.ReadUInt64(); // unknown2
			reader.ReadUInt64(); // unknown3

			blockSizes = new int[blockCount];
			for (int ii = 0; ii < blockCount; ii++) {
				blockSizes[ii] = reader.ReadInt32();
			}
		}

		private void ReadCompressedData(BinaryReader reader) {
			compressedDataBlocks = new List<Memory<byte>>();

			for (int ii = 0; ii < blockCount; ii++) {
				compressedDataBlocks.Add(reader.ReadBytes(blockSizes[ii]));
			}
		}

		private void DecompressBlocks(int start, int end) {
			for (int ii = start; ii < end; ii++) {
				DecompressBlock(ii);
			}
		}

		private void DecompressBlock(int index) {
			if (decompressedData.IsEmpty) {
				decompressedData = new byte[UncompressedSize];
			}

			var block = compressedDataBlocks[index];
			if (Memory<byte>.Empty.Equals(block)) {
				return;
			}

			var decompressedBlockStart = uncompressedBlockSize * index;
			var decompressedBlockSize = index == (blockCount - 1) ? UncompressedSize % uncompressedBlockSize : uncompressedBlockSize;
			var decompressionBuffer = new byte[decompressedBlockSize + 64];

			//int actualDecompressedSize = LibOoz.Ooz_Decompress(block.ToArray(), block.Length, decompressionBuffer, decompressedBlockSize);
			int actualDecompressedSize = decompressedBlockSize;

			if (decompressedBlockSize != actualDecompressedSize) {
				throw new Exception(string.Format($"Error decompressing block {index} of {blockCount}. Expected {decompressedBlockSize} bytes, but got {actualDecompressedSize} bytes"));
			}

			var targetBuffer = decompressedData.Slice(decompressedBlockStart, decompressedBlockSize);
			decompressionBuffer.AsMemory(0, decompressedBlockSize).CopyTo(targetBuffer);
			compressedDataBlocks[index] = Memory<byte>.Empty;
		}
	}
}
