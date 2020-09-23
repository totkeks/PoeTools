namespace PoeTools.Util.Oodle
{
	using System;
	using static Constants;

	public class Decoder
	{
		// From KrakenDecoder
		private int sourceBytesUsed;
		private int destinationBytesUsed;
		private Memory<byte> scratch;

		// From KrakenHeader
		Header header;

		// From DecodeStep
		private int destinationOffset;
		// private int

		private ReadOnlyMemory<byte> source;
		private Memory<byte> destination;

		private ReadOnlyMemory<byte> remainingSource;
		private Memory<byte> remainingDestination;

		private int SourceBytesLeft
		{
			get => remainingSource.Length;
		}

		private int DestinationBytesLeft
		{
			get => remainingDestination.Length;
		}

		public int Decompress(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			destinationOffset = 0;

			this.source = source;
			remainingSource = source;
			this.destination = destination;
			remainingDestination = destination;

			sourceBytesUsed = 0;
			destinationBytesUsed = 0;
			// scratch = new byte[0x6C000];

			// Parse header every 256kb of output data. for real? is this for bigger block sizes?
			ProgressSource(Header.Parse(source, out header));

			destinationOffset += DecodeBlock();

			return -1;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns>Actual size of the block</returns>
		private int DecodeBlock()
		{
			bool isKrakenDecoder = header.DecoderType.IsKrakenDecoder();
			int blockSize = Math.Min(isKrakenDecoder ? _256K : _16K, DestinationBytesLeft);

			if (header.Uncompressed)
			{
				CopyUncompressedBlock(blockSize);
			}

			ProgressSource(isKrakenDecoder ?
				BlockHeader.ParseAsKraken(source, header.UseChecksums, out BlockHeader blockHeader) :
				BlockHeader.ParseAsLZNA(source, header.UseChecksums, blockSize, out blockHeader));

			if (SourceBytesLeft < blockHeader.CompressedSize)
			{
				throw new Exception($"Error decoding compressed data. Expected {blockHeader.CompressedSize} bytes, but got only {SourceBytesLeft} bytes");
			}

			if (blockHeader.CompressedSize > DestinationBytesLeft)
			{
				throw new Exception($"Error decoding "); // TODO: text?
			}

			if (blockHeader.Fill)
			{
				FillQuantum(blockHeader, blockSize);

				return blockSize;
			}

			// TODO: CRC Check
			// 		if (dec->hdr.use_checksums &&
			//    (Kraken_GetCrc(src, qhdr.compressed_size) & 0xFFFFFF) != qhdr.checksum)
			//  return false;

			if (blockHeader.CompressedSize == blockSize)
			{
				CopyUncompressedBlock(blockSize);
			}

			int compressedSize = 0;
			switch (header.DecoderType)
			{
				case DecoderType.LZNA:
					if (header.RestartDecoder)
					{
						header.RestartDecoder = false;
						//LZNA_InitLookup((struct LznaState *)dec->scratch);
					}
					throw new Exception("LZNA not yet implemented");
					// n = LZNA_DecodeQuantum
					break;

				case DecoderType.Kraken:
					// compressedSize = Kraken.DecodeQuantum();
					throw new Exception("Kraken not yet implemented");
					break;

				case DecoderType.Mermaid:
					throw new Exception("Mermaid not yet implemented");
					// n = Mermaid_DecodeQuantum
					break;

				case DecoderType.BitKnit:
					if (header.RestartDecoder)
					{
						header.RestartDecoder = false;
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

			if (compressedSize != blockHeader.CompressedSize)
			{
				throw new Exception("data returned not as expected"); // TODO: muh
			}

			ProgressSource(compressedSize);
			ProgressDestination(blockSize);

			return compressedSize;
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

		private void FillQuantum(BlockHeader blockHeader, int blockSize)
		{
			if (blockHeader.WholeMatchDistance != 0)
			{
				FillQuantum(blockHeader.WholeMatchDistance, blockSize);
			}
			else
			{
				FillQuantum((byte)blockHeader.Checksum, blockSize);
			}

			ProgressDestination(blockSize);
		}

		private void FillQuantum(byte value, int length)
		{
			var quantumData = destination[destinationOffset..length];

			quantumData.Span.Fill(value);
		}

		private void FillQuantum(int reverseOffset, int length)
		{
			var previousData = destination[(destinationOffset - reverseOffset)..length];
			var quantumData = destination[destinationOffset..length];

			previousData.CopyTo(quantumData);
		}
	}
}
