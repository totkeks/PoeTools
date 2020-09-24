using System;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	/// <summary>
	/// Additional header in front of each <c>quantum</c> (a 256k block).
	/// </summary>
	internal struct QuantumHeader
	{
		private const int BigBlock = 0x3FFFF; // 256k - 1
		private const int SmallBlock = 0x3FFF; // 16k - 1

		public int CompressedSize { get; }
		public int Checksum { get; }
		public bool Flag1 { get; }
		public bool Flag2 { get; }
		public int WholeMatchDistance { get; }

		public bool Fill
		{
			get => CompressedSize == 0;
		}

		public static int ParseAsKraken(ReadOnlyMemory<byte> source, bool useChecksum, out QuantumHeader quantumHeader)
		{
			var relevantBytes = source[0..6].Span;
			int headerWord = DWord.FromBytes(relevantBytes[0], relevantBytes[1], relevantBytes[2]);

			int size = headerWord.GetBits(18);
			if (size != BigBlock)
			{
				var compressedSize = size + 1;
				var flag1 = headerWord.GetBit(19);
				var flag2 = headerWord.GetBit(20);

				if (useChecksum)
				{
					var checksum = DWord.FromBytes(relevantBytes[3], relevantBytes[4], relevantBytes[5]);
					quantumHeader = new QuantumHeader(compressedSize, checksum, flag1, flag2, 0);
					return 6;
				}
				else
				{
					quantumHeader = new QuantumHeader(compressedSize, 0, flag1, flag2, 0);
					return 3;
				}
			}

			if (headerWord.GetBits(6, 18) == 1)
			{
				var checksum = source[3..1].Span[0];
				quantumHeader = new QuantumHeader(0, checksum, false, false, 0);
				return 4;
			}

			throw new Exception("Invalid Kraken block header");
		}

		public static int ParseAsLZNA(ReadOnlyMemory<byte> source, bool useChecksum, int blockSize, out QuantumHeader quantumHeader)
		{
			var relevantBytes = source[0..5].Span;
			var headerWord = DWord.FromBytes(relevantBytes[0], relevantBytes[1]);

			int size = headerWord.GetBits(14);
			if (size != SmallBlock)
			{
				var compressedSize = size + 1;
				var flag1 = headerWord.GetBit(15);
				var flag2 = headerWord.GetBit(16);

				if (useChecksum)
				{
					var checksum = DWord.FromBytes(relevantBytes[2], relevantBytes[3], relevantBytes[4]);
					quantumHeader = new QuantumHeader(compressedSize, checksum, flag1, flag2, 0);
					return 5;
				}
				else
				{
					quantumHeader = new QuantumHeader(compressedSize, 0, flag1, flag2, 0);
					return 2;
				}
			}

			var control = headerWord.GetBits(2, 14);
			// TODO: create enum for this
			switch (control)
			{
				case 0: // Match something something??
					LZNA.ParseWholeMatchInfo(source[2..], out int wholeMatchDistance);
					quantumHeader = new QuantumHeader(0, 0, false, false, wholeMatchDistance);
					return 0;

				case 1: // memset
					quantumHeader = new QuantumHeader(0, relevantBytes[2], false, false, 0);
					return 3;

				case 2: // uncompressed
					quantumHeader = new QuantumHeader(blockSize, 0, false, false, 0);
					return 2;
			}

			throw new Exception("Invalid LZNA block header");
		}

		public QuantumHeader(int compressedSize, int checksum, bool flag1, bool flag2, int wholeMatchDistance)
		{
			CompressedSize = compressedSize;
			Checksum = checksum;
			Flag1 = flag1;
			Flag2 = flag2;
			WholeMatchDistance = wholeMatchDistance;
		}
	}
}
