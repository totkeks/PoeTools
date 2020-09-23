namespace PoeTools.Util.Oodle
{
	using System;
	using System.Dynamic;
	using PoeTools.Util.ExtensionMethods;
	using static Constants;

	internal class Kraken
	{
		private ReadOnlyMemory<byte> initialSource;
		private Memory<byte> initialDestination;

		private ReadOnlyMemory<byte> currentSource;
		private Memory<byte> currentDestination;

		public int SourceBytesUsed
		{
			get => initialSource.Length - currentSource.Length;
		}

		public int DestinationBytesUsed
		{
			get => initialDestination.Length - currentDestination.Length;
		}

		public int DecodeBlock(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			initialSource = source;
			initialDestination = destination;

			currentSource = source;
			currentDestination = destination;

			int sourceBytesUsed = 0;

			while (currentDestination.Length != 0)
			{
				var blockSize = currentDestination.Length > _128K ? _128K : currentDestination.Length;
				DecodeHalfBlock(blockSize);
			}

			return sourceBytesUsed;
		}

		private void DecodeHalfBlock(int blockSize)
		{
			var destination = currentDestination[0..blockSize];
			currentDestination = currentDestination[blockSize..];

			if (currentSource.Length < 4)
			{
				throw new Exception($"Error decoding half-block: currentSource must be at least 4 bytes long, but was only {currentSource.Length} bytes long");
			}

			var bytes = currentSource[0..2].Span;
			var header = DWord.FromBytes(bytes[0], bytes[1], bytes[2]);

			if (!header.SelectBit(24))
			{
				// Stored as entropy without any match copying. -- What??
				return DecodeChunk();
			}

			currentSource = currentSource[3..];
			int sourceSize = header.SelectBits(19);
			var mode = header.SelectBits(4, 19);

			if (currentSource.Length < sourceSize)
			{
				throw new Exception($"Error decoding half-block: required {sourceSize} currentSource bytes, but got only {currentSource.Length} bytes");
			}

			if (sourceSize > destination.Length)
			{
				throw new Exception($"Error decoding half-block: currentSource ({currentSource.Length}) is bigger than destination ({destination.Length})");
			}

			if (mode != 0)
			{
				throw new Exception($"Error decoding half-block: mode is zero. Whatever that means.");
			}

			if (sourceSize == destination.Length)
			{
				currentSource[..destination.Length].CopyTo(destination);
				currentSource = currentSource[destination.Length..];
			}

			if (sourceSize < destination.Length)
			{
				// Do some actual decompression
				// 				size_t scratch_usage = Min(Min(3 * dst_count + 32 + 0xd000, 0x6C000), scratch_end - scratch);
				//   if (scratch_usage < sizeof(KrakenLzTable))
				//     return -1;
				//   if (!Kraken_ReadLzTable(mode,
				//                           src, src + src_used,
				//                           dst, dst_count,
				//                           dst - dst_start,
				//                           scratch + sizeof(KrakenLzTable), scratch + scratch_usage,
				//                           (KrakenLzTable *)scratch))
				//     return -1;
				//   if (!Kraken_ProcessLzRuns(mode, dst, dst_count, dst - dst_start, (KrakenLzTable *)scratch))
				//     return -1;
			}
		}

		private static int ReadLzTable(int mode, ReadOnlyMemory<byte> inSource, Memory<byte> inDestination, int offset)
		{
			var currentSource = inSource;
			var destination = inDestination;

			if (mode > 1)
			{
				throw new Exception($"Error reading LZ table: mode is greater 1 ({mode})");
			}

			if (currentSource.Length < 13)
			{
				throw new Exception($"Error reading LZ table: currentSource must be at least 13 bytes long, but was only {currentSource.Length} bytes long");
			}

			if (offset == 0)
			{
				currentSource[0..8].CopyTo(destination);
				currentSource = currentSource[8..];
				destination = destination[8..];
			}

			var flag = currentSource[0..1].Span[0];
			if (flag.SelectBit(8))
			{
				if (flag.SelectBit(7))
				{
					throw new Exception("Error reading LZ table: reserved flag set");
				}

				throw new Exception("Error reading LZ table: flag contains unsupported bits");
			}

			// Disable no copy optimization if currentSource and dest overlap
			//bool force_copy = dst <= src_end && src <= dst + dst_size;
			// TODO: how to check actual adresses with C#? I guess just ignore


		}

		private void DecodeChunk()
		{
			var chunkType = currentSource[0..1].Span[0].SelectBits(3, 4);
			if (chunkType == 0)
			{
				DecodeChunk_Copy();
			}
			else
			{
				DecodeChunk_Decompress(chunkType);
			}
		}

		private void DecodeChunk_Copy()
		{
			int sourceSize;
			var bytes = currentSource[0..2].Span;

			if (bytes[0] >= 0x80)
			{
				sourceSize = DWord.FromBytes(bytes[0], bytes[1]).SelectBits(12);
				currentSource = currentSource[0..2];
			}
			else
			{
				sourceSize = DWord.FromBytes(bytes[0], bytes[1], bytes[2]);
				if (sourceSize.SelectBits(6, 18) > 0)
				{
					throw new Exception("Error decoding chunk: reserved bits must not be set");
				}
				currentSource = currentSource[0..3];
			}

			currentSource[0..sourceSize].CopyTo(currentDestination);
			currentSource = currentSource[sourceSize..];
			currentDestination = currentDestination[sourceSize..];
		}

		private void DecodeChunk_Decompress(int chunkType)
		{
			int sourceSize;
			int destinationSize;
			var firstByte = currentSource[0..1].Span[0];

			if (firstByte >= 0x80)
			{ // Short mode, 10 bit size descriptors
				var bytes = currentSource[0..3].Span;
				var descriptors = DWord.FromBytes(bytes[0], bytes[1], bytes[2]);
				sourceSize = descriptors.SelectBits(10);
				destinationSize = sourceSize + descriptors.SelectBits(10, 10) + 1;
				currentSource = currentSource[3..];
			}
			else
			{ // Long mode, 18 bit size descriptors
				var bytes = currentSource[0..5].Span;
				var descriptors = DWord.FromBytes(bytes[1], bytes[2], bytes[3], bytes[4]);
				sourceSize = descriptors.SelectBits(18);
				destinationSize = (descriptors.SelectBits(14, 18) | (bytes[0] << 14)).SelectBits(18) + 1;
				currentSource = currentSource[5..];

				// TODO: What is happening with the scratch here? shifted a little to make space for the result to re-use it at another point?
				// if (dst == scratch)
				// {
				// 	scratch += dst_size;
				// }
			}

			switch (chunkType)
			{
				case 2:
				case 4:
					DecodeBytes_Type12(sourceSize, chunkType >> 1);
					break;

				case 5:
					// src_used = Krak_DecodeRecursive(src, src_size, dst, dst_size, scratch, scratch_end);
					break;

				case 3:
					// src_used = Krak_DecodeRLE(src, src_size, dst, dst_size, scratch, scratch_end);
					break;

				case 1:
					//  src_used = Krak_DecodeTans(src, src_size, dst, dst_size, scratch, scratch_end);
					break;
			}
		}

		private readonly int[] initialCodePrefix = new[] { 0x0, 0x0, 0x2, 0x6, 0xE, 0x1E, 0x3E, 0x7E, 0xFE, 0x1FE, 0x2FE, 0x3FE };

		private void DecodeBytes_Type12(int size, int type)
		{
			var bits = new BitReader(currentSource[..size], 0, 24);
			bits.Refill();

			int[] codePrefix = (int[])initialCodePrefix.Clone();
			var syms = new byte[1280];
			int symbolCount;

			if (!bits.ReadBit(false))
			{
				symbolCount = Huffman.ReadCodeLengthsOld(bits, ref syms, ref codePrefix);
			}
			else if (!bits.ReadBit(false))
			{
				// num_syms = Huff_ReadCodeLengthsNew(&bits, syms, code_prefix);
			}
			else
			{
				// TODO: throw error
				return;
			}
		}

		private static int ProcessLzRuns()
		{
			return 0;
		}
	}


}
