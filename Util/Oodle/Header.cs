using System;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class Header
	{
		const byte MAGIC_NUMBER = 0xC;

		public DecoderType DecoderType { get; }
		public bool RestartDecoder { get; set; }
		public bool Uncompressed { get; }
		public bool UseChecksums { get; }

		public static int Parse(ReadOnlyMemory<byte> source, out Header header)
		{
			var firstByte = source[0..1].Span[0];

			var lowerNibble = firstByte.SelectBits(4);
			if (lowerNibble != MAGIC_NUMBER)
			{
				throw new Exception($"Invalid lower nibble in magic byte. Expected 0x{MAGIC_NUMBER:X}, got 0x{lowerNibble:X}");
			}

			var upperNibble = firstByte.SelectBits(4, 4);
			var lowerTwoBits = upperNibble.SelectBits(2);
			if (lowerTwoBits != 0)
			{
				throw new Exception($"Invalid upper nibble in magic byte. Expected lower two bits to be 0, got {Convert.ToString(lowerTwoBits, 2),2}");
			}

			var uncompressed = upperNibble.SelectBit(3);
			var restartDecoder = upperNibble.SelectBit(4);

			var secondByte = source[1..2].Span[0];
			var decoderType = (DecoderType)secondByte.SelectBits(7);
			var useChecksums = secondByte.SelectBit(8);

			if (!Enum.IsDefined(typeof(DecoderType), decoderType))
			{
				throw new Exception($"Unsupported decoder type {decoderType}");
			}

			header = new Header(decoderType, restartDecoder, uncompressed, useChecksums);

			return 2;
		}

		private Header(DecoderType decoderType, bool restartDecoder, bool uncompressed, bool useChecksums)
		{
			DecoderType = decoderType;
			RestartDecoder = restartDecoder;
			Uncompressed = uncompressed;
			UseChecksums = useChecksums;
		}
	}
}
