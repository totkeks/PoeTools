namespace PoeTools.Util.Oodle
{
	using System;
	using System.Diagnostics;
	using System.Dynamic;
	using PoeTools.Util.ExtensionMethods;
	using static Constants;

	internal class Kraken
	{
		private ReadOnlyMemory<byte> initialSource;
		private Memory<byte> initialDestination;

		private ReadOnlyMemory<byte> remainingSource;
		private Memory<byte> remainingDestination;

		private KrakenLzTable lzTable;

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

		public int DecodeQuantum(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decoding Kraken quantum of {source.Length} bytes into {destination.Length} bytes.");

			initialSource = source;
			initialDestination = destination;

			remainingSource = source;
			remainingDestination = destination;

			int sourceBytesUsed = 0;

			while (remainingDestination.Length != 0)
			{
				var segmentSize = remainingDestination.Length > _128K ? _128K : remainingDestination.Length;
				DecodeQuantumSegment(segmentSize);
			}

			return sourceBytesUsed;
		}

		private void DecodeQuantumSegment(int segmentSize)
		{
			Debug.WriteLine($"Decoding Kraken quantum segment with {segmentSize} bytes.");

			var temp = remainingSource[0..3].Span;
			var header = DWord.FromBytes(temp[0], temp[1], temp[2]);

			if (!header.GetBit(24))
			{
				ProgressSource(ByteDecoder.Decode(remainingSource, out var buffer));
				buffer.CopyTo(remainingDestination);
				ProgressDestination(buffer.Length);
				return;
			}

			ProgressSource(3);
			int compressedSize = header.GetBits(18);
			var mode = header.GetBits(4, 19);

			if (SourceBytesLeft < compressedSize)
			{
				throw new Exception($"Error decoding quantum segment: required {compressedSize} source bytes, but got only {SourceBytesLeft} bytes");
			}

			if (segmentSize == compressedSize)
			{
				remainingSource[..segmentSize].CopyTo(remainingDestination);
				ProgressSource(segmentSize);
				ProgressDestination(segmentSize);
				return;
			}

			lzTable = new KrakenLzTable();
			lzTable.ReadTable(remainingSource, remainingDestination, mode, SourceBytesUsed);

			throw new Exception("Kraken decompression unfinished");

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

		private static int ProcessLzRuns()
		{
			return 0;
		}

		private void ProgressSource(int amount)
		{
			remainingSource = remainingSource[amount..];
		}

		private void ProgressDestination(int amount)
		{
			remainingDestination = remainingDestination[amount..];
		}
	}
}
