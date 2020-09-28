using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PoeTools.Bundles.Lib {
	/// <summary>
	/// A binary bundle with a header and compressed data.<br/>
	/// Typically contained inside a <c>.bundle.bin</c> file, but can also come from memory.
	/// </summary>
	public class Bundle {
		private long uncompressedSizeL;
		private long dataSizeL;
		private int uncompressedBlockSize;
		private int[] blockSizes;

		private List<Memory<byte>> compressedDataBlocks;
		private Memory<byte> decompressedData;

		public string Name { get; }
		public int CompressedSize { get; private set; }
		public int UncompressedSize { get; private set; }
		public int BlockCount { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Bundle"/> class with data from a specified file.
		/// </summary>
		/// <param name="filePath">The path to the bundle file.</param>
		public Bundle(string filePath) : this(
			new BinaryReader(new FileStream(filePath, FileMode.Open)),
			Path.GetFileName(filePath).Replace(".bundle.bin", "")
		) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Bundle"/> class with data from a stream.
		/// </summary>
		/// <param name="stream">The stream containing the bundle.</param>
		/// <param name="name">The name of the bundle.</param>
		public Bundle(BinaryReader reader, string name = "-in-memory-") {
			Name = name;

			ReadPreamble(reader);
			ReadHeader(reader);
			ReadCompressedData(reader);

			// Sanity checks
			if (UncompressedSize != uncompressedSizeL) {
				throw new InvalidDataException($"Error reading bundle: uncompressed sizes are not equal. {UncompressedSize} vs {uncompressedSizeL}");
			}
			if (CompressedSize != dataSizeL) {
				throw new InvalidDataException($"Error reading bundle: compressed sizes are not equal. {CompressedSize} vs {dataSizeL}");
			}
			if (blockSizes.Sum() != CompressedSize) {
				throw new InvalidDataException($"Error reading bundle: sum of blocks does not match data size. {blockSizes.Sum()} vs {CompressedSize}");
			}
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
			return string.Format($"Bundle '{Name}' with {BlockCount} blocks");
		}

		private void ReadPreamble(BinaryReader reader) {
			UncompressedSize = reader.ReadInt32();
			CompressedSize = reader.ReadInt32();
			reader.ReadInt32(); // headerSize
		}

		private void ReadHeader(BinaryReader reader) {
			reader.ReadUInt32(); // compressionAlgorithm
			reader.ReadUInt32(); // unknown1
			uncompressedSizeL = reader.ReadInt64();
			dataSizeL = reader.ReadInt64();
			BlockCount = reader.ReadInt32();
			uncompressedBlockSize = reader.ReadInt32();
			reader.ReadUInt64(); // unknown2
			reader.ReadUInt64(); // unknown3

			blockSizes = new int[BlockCount];
			for (int ii = 0; ii < BlockCount; ii++) {
				blockSizes[ii] = reader.ReadInt32();
			}
		}

		private void ReadCompressedData(BinaryReader reader) {
			compressedDataBlocks = new List<Memory<byte>>();

			for (int ii = 0; ii < BlockCount; ii++) {
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
			var decompressedBlockSize = index == (BlockCount - 1) ? UncompressedSize % uncompressedBlockSize : uncompressedBlockSize;
			var decompressionBuffer = new byte[decompressedBlockSize + 64];

			int actualDecompressedSize = LibOoz.Ooz_Decompress(block.ToArray(), block.Length, decompressionBuffer, decompressedBlockSize);

			if (decompressedBlockSize != actualDecompressedSize) {
				throw new Exception(string.Format($"Error decompressing block {index} of {BlockCount}. Expected {decompressedBlockSize} bytes, but got {actualDecompressedSize} bytes"));
			}

			var targetBuffer = decompressedData.Slice(decompressedBlockStart, decompressedBlockSize);
			decompressionBuffer.AsMemory(0, decompressedBlockSize).CopyTo(targetBuffer);
			compressedDataBlocks[index] = Memory<byte>.Empty;
		}
	}
}
