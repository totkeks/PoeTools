using System;
using System.IO;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class BlockHeader
	{
		const byte MAGIC_NUMBER = 0xC;

		public DecoderType DecoderType { get; }
		public bool RestartDecoder { get; set; }
		public bool Uncompressed { get; }
		public bool UseChecksums { get; }

		public static int Parse(ReadOnlyMemory<byte> source, out BlockHeader blockHeader)
		{
			var firstByte = source[0..1].Span[0];

			var lowerNibble = firstByte.GetBits(4);
			if (lowerNibble != MAGIC_NUMBER)
			{
				throw new InvalidDataException($"Invalid lower nibble in magic byte. Expected 0x{MAGIC_NUMBER:X}, got 0x{lowerNibble:X}");
			}

			var upperNibble = firstByte.GetBits(4, 4);
			var lowerTwoBits = IntBitExtensions.GetBits(upperNibble, (int)2);
			if (lowerTwoBits != 0)
			{
				throw new InvalidDataException($"Invalid upper nibble in magic byte. Expected lower two bits to be 0, got {Convert.ToString(lowerTwoBits, 2),2}");
			}

			var uncompressed = upperNibble.GetBit(3);
			var restartDecoder = upperNibble.GetBit(4);

			var secondByte = source[1..2].Span[0];
			var decoderType = (DecoderType)secondByte.GetBits(7);
			var useChecksums = secondByte.GetBit(8);

			blockHeader = new BlockHeader(decoderType, restartDecoder, uncompressed, useChecksums);

			return 2;
		}

		private BlockHeader(DecoderType decoderType, bool restartDecoder, bool uncompressed, bool useChecksums)
		{
			DecoderType = decoderType;
			RestartDecoder = restartDecoder;
			Uncompressed = uncompressed;
			UseChecksums = useChecksums;
		}
	}
}
