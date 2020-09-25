namespace PoeTools.Util.Oodle
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using PoeTools.Util.ExtensionMethods;
	using static Constants;

	/// <summary>
	/// A decoder to decompress data compressed using Oodle's algorithms.
	/// </summary>
	public class Decoder
	{
		BlockHeader blockHeader;

		private ReadOnlyMemory<byte> initialSource;
		private Memory<byte> initialDestination;

		private ReadOnlyMemory<byte> remainingSource;
		private Memory<byte> remainingDestination;

		public int SourceBytesUsed
		{
			get => initialSource.Length - remainingSource.Length;
		}

		public int DestinationBytesUsed
		{
			get => initialDestination.Length - remainingDestination.Length;
		}

		private int SourceBytesLeft
		{
			get => remainingSource.Length;
		}

		private int DestinationBytesLeft
		{
			get => remainingDestination.Length;
		}

		/// <summary>
		/// Decompresses a file that contains the
		/// </summary>
		/// <param name="file">The file to decompress.</param>
		/// <returns>The uncompressed contents of the file.</returns>
		public Memory<byte> DecompressFile(FileInfo file)
		{
			if (!file.Exists)
			{
				throw new Exception("File does not exist");
			}

			using var stream = file.OpenRead();
			using var reader = new BinaryReader(stream);

			// Detect if file uses 4 or 8 bytes for uncompressed size
			var firstFourBytes = reader.ReadUInt64();
			ulong uncompressedSize;

			if (firstFourBytes > 0x10000000000)
			{
				reader.BaseStream.Seek(0, SeekOrigin.Begin);
				uncompressedSize = reader.ReadUInt32();
			}
			else
			{
				uncompressedSize = firstFourBytes;
			}

			Memory<byte> source = new byte[reader.BaseStream.Length - reader.BaseStream.Position];
			reader.Read(source.Span);
			reader.Close();
			Memory<byte> destination = new byte[uncompressedSize];

			Decompress(source, destination);

			return destination;
		}

		/// <summary>
		/// Decompresses arbitrary amounts of data.
		/// </summary>
		/// <param name="source">The memory region with the compressed data.</param>
		/// <param name="destination">The memory region for the uncompressed data.</param>
		public void Decompress(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decompressing {source.Length} bytes into {destination.Length} bytes.");

			initialSource = source;
			remainingSource = source;
			initialDestination = destination;
			remainingDestination = destination;

			while (destination.Length != 0)
			{
				if (DestinationBytesUsed.GetBits(18) == 0)
				{
					// There is a header in the source for every decoded 256k
					Debug.WriteLine($"Reading a new block header from source position {SourceBytesUsed}.");
					ProgressSource(BlockHeader.Parse(remainingSource, out blockHeader));
				}

				DecodeBlock();
			}
		}

		private void DecodeBlock()
		{
			bool isKrakenDecoder = blockHeader.DecoderType.IsKrakenDecoder();
			int blockSize = Math.Min(isKrakenDecoder ? _256K : _16K, DestinationBytesLeft);

			if (blockHeader.Uncompressed)
			{
				CopyUncompressedBlock(blockSize);
				return;
			}

			ProgressSource(isKrakenDecoder ?
				QuantumHeader.ParseAsKraken(remainingSource, blockHeader.UseChecksums, out QuantumHeader quantumHeader) :
				QuantumHeader.ParseAsLZNA(remainingSource, blockHeader.UseChecksums, blockSize, out quantumHeader));

			if (SourceBytesLeft < quantumHeader.CompressedSize)
			{
				throw new Exception($"Error decoding compressed data. Expected {quantumHeader.CompressedSize} bytes, but got only {SourceBytesLeft} bytes");
			}

			if (quantumHeader.CompressedSize > DestinationBytesLeft)
			{
				throw new Exception($"Error decoding "); // TODO: text?
			}

			if (quantumHeader.Fill)
			{
				FillQuantum(quantumHeader, blockSize);
			}

			// TODO: CRC Check
			// 		if (dec->hdr.use_checksums &&
			//    (Kraken_GetCrc(src, qhdr.compressed_size) & 0xFFFFFF) != qhdr.checksum)
			//  return false;

			if (quantumHeader.CompressedSize == blockSize)
			{
				CopyUncompressedBlock(blockSize);
			}

			var blockSource = remainingSource[..quantumHeader.CompressedSize];
			var blockDestination = remainingDestination[..blockSize];
			int compressedSize = 0;
			switch (blockHeader.DecoderType)
			{
				case DecoderType.LZNA:
					if (blockHeader.RestartDecoder)
					{
						blockHeader.RestartDecoder = false;
						//LZNA_InitLookup((struct LznaState *)dec->scratch);
					}
					throw new Exception("LZNA not yet implemented");
					// n = LZNA_DecodeQuantum
					break;

				case DecoderType.Kraken:
					new Kraken().DecodeQuantum(blockSource, blockDestination);
					break;

				case DecoderType.Mermaid:
					throw new Exception("Mermaid not yet implemented");
					// n = Mermaid_DecodeQuantum
					break;

				case DecoderType.BitKnit:
					if (blockHeader.RestartDecoder)
					{
						blockHeader.RestartDecoder = false;
						// BitknitState_Init((struct BitknitState *)dec->scratch);
					}
					throw new Exception("BitKnit not yet implemented");
					// n = (int)Bitknit_Decode
					break;

				case DecoderType.Leviathan:
					throw new Exception("Leviathan not yet implemented");
					// n = Leviathan_DecodeQuantum
					break;
			}

			if (compressedSize != quantumHeader.CompressedSize)
			{
				throw new Exception("data returned not as expected"); // TODO: muh
			}

			ProgressSource(compressedSize);
			ProgressDestination(blockSize);
		}

		private void CopyUncompressedBlock(int blockSize)
		{
			if (SourceBytesLeft < blockSize)
			{
				throw new Exception($"Error decoding uncompressed data. Expected {blockSize} source bytes, but got only {SourceBytesLeft} bytes");
			}

			remainingSource[0..blockSize].CopyTo(remainingDestination[0..blockSize]);

			ProgressSource(blockSize);
			ProgressDestination(blockSize);
		}

		private void ProgressSource(int amount)
		{
			remainingSource = remainingSource[amount..];
		}

		private void ProgressDestination(int amount)
		{
			remainingDestination = remainingDestination[amount..];
		}

		private void FillQuantum(QuantumHeader quantumHeader, int blockSize)
		{
			if (quantumHeader.WholeMatchDistance != 0)
			{
				FillQuantum(quantumHeader.WholeMatchDistance, blockSize);
			}
			else
			{
				FillQuantum((byte)quantumHeader.Checksum, blockSize);
			}

			ProgressDestination(blockSize);
		}

		private void FillQuantum(byte value, int length)
		{
			var quantumData = initialDestination[DestinationBytesUsed..length];

			quantumData.Span.Fill(value);
		}

		private void FillQuantum(int reverseOffset, int length)
		{
			var previousData = initialDestination[(DestinationBytesUsed - reverseOffset)..length];
			var quantumData = initialDestination[DestinationBytesUsed..length];

			previousData.CopyTo(quantumData);
		}
	}
}
