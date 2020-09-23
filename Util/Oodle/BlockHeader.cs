using System;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	/// <summary>
	/// Additional header in front of each <c>quantum</c> (a 256k block).
	/// </summary>
	internal struct BlockHeader
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

		public static int ParseAsKraken(ReadOnlyMemory<byte> source, bool useChecksum, out BlockHeader blockHeader)
		{
			var relevantBytes = source[0..6].Span;
			int headerWord = DWord.FromBytes(relevantBytes[0], relevantBytes[1], relevantBytes[2]);

			int size = headerWord.SelectBits(18);
			if (size != BigBlock)
			{
				var compressedSize = size + 1;
				var flag1 = headerWord.SelectBit(19);
				var flag2 = headerWord.SelectBit(20);

				if (useChecksum)
				{
					var checksum = DWord.FromBytes(relevantBytes[3], relevantBytes[4], relevantBytes[5]);
					blockHeader = new BlockHeader(compressedSize, checksum, flag1, flag2, 0);
					return 6;
				}
				else
				{
					blockHeader = new BlockHeader(compressedSize, 0, flag1, flag2, 0);
					return 3;
				}
			}

			if (headerWord.SelectBits(6, 18) == 1)
			{
				var checksum = source[3..1].Span[0];
				blockHeader = new BlockHeader(0, checksum, false, false, 0);
				return 4;
			}

			throw new Exception("Invalid Kraken block header");
		}

		public static int ParseAsLZNA(ReadOnlyMemory<byte> source, bool useChecksum, int blockSize, out BlockHeader blockHeader)
		{
			var relevantBytes = source[0..5].Span;
			var headerWord = DWord.FromBytes(relevantBytes[0], relevantBytes[1]);

			int size = headerWord.SelectBits(14);
			if (size != SmallBlock)
			{
				var compressedSize = size + 1;
				var flag1 = headerWord.SelectBit(15);
				var flag2 = headerWord.SelectBit(16);

				if (useChecksum)
				{
					var checksum = DWord.FromBytes(relevantBytes[2], relevantBytes[3], relevantBytes[4]);
					blockHeader = new BlockHeader(compressedSize, checksum, flag1, flag2, 0);
					return 5;
				}
				else
				{
					blockHeader = new BlockHeader(compressedSize, 0, flag1, flag2, 0);
					return 2;
				}
			}

			var control = headerWord.SelectBits(2, 14);
			// TODO: create enum for this
			switch (control)
			{
				case 0: // Match something something??
					LZNA.ParseWholeMatchInfo(source[2..], out int wholeMatchDistance);
					blockHeader = new BlockHeader(0, 0, false, false, wholeMatchDistance);
					return 0;

				case 1: // memset
					blockHeader = new BlockHeader(0, relevantBytes[2], false, false, 0);
					return 3;

				case 2: // uncompressed
					blockHeader = new BlockHeader(blockSize, 0, false, false, 0);
					return 2;
			}

			throw new Exception("Invalid LZNA block header");
		}

		public BlockHeader(int compressedSize, int checksum, bool flag1, bool flag2, int wholeMatchDistance)
		{
			CompressedSize = compressedSize;
			Checksum = checksum;
			Flag1 = flag1;
			Flag2 = flag2;
			WholeMatchDistance = wholeMatchDistance;
		}
	}
}
