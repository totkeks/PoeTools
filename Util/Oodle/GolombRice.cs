using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal static class GolombRice
	{
		public static int ReadCodeLengths(BitReader bits, ref byte[] symbols, ref uint[] codePrefix)
		{
			int forcedBits = bits.ReadBits(2);
			int symbolCount = bits.ReadBits(8) + 1;
			int fluff = bits.ReadFluff(symbolCount);

			int bitPosition = (bits.BitPosition - 24).GetBits(3);
			var offset = bits.DataPosition - ((24 - bits.BitPosition + 7) >> 3);
			var source = bits.Source.Span;

			Span<byte> codeLengths = new byte[512];
			DecodeLengths(source, codeLengths, symbolCount, ref offset, ref bitPosition);
			DecodeBits(source, codeLengths, symbolCount, forcedBits, ref offset, ref bitPosition);

			// TODO: continue here
			return 0;
		}

		private static void DecodeLengths(ReadOnlySpan<byte> source, Span<byte> destination, int symbolCount, ref int sourcePosition, ref int bitPosition)
		{
			if (source.Length == 0)
			{
				throw new InvalidDataException("Error decoding golomb rice lengths: data source is empty");
			}

			int destinationPosition = 0;
			uint count = (uint)-bitPosition;
			uint v = (uint)(source[sourcePosition++] & (255 >> bitPosition));

			for (; ; )
			{
				if (v == 0)
				{
					count += 8;
				}
				else
				{
					uint x = Bits2Value[v];
					CopyData(count + (x & 0x0f0f0f0f), destination, destinationPosition);
					CopyData((x >> 4) & 0x0f0f0f0f, destination, destinationPosition + 4);

					destinationPosition += Bits2Length[v];
					if (destinationPosition >= destination.Length)
					{
						break;
					}
					count = x >> 28;
				}

				v = source[sourcePosition++];
			}

			if (destinationPosition > symbolCount)
			{
				for (int ii = 0; ii < destinationPosition - symbolCount; ii++)
				{
					v &= v - 1;
				}
			}

			bitPosition = 0;
			if (v.GetBit(0))
			{
				sourcePosition--;
				bitPosition = 8 - BitOperations.TrailingZeroCount(v);
			}
		}

		private static void CopyData(uint source, Span<byte> destination, int offset)
		{
			// TODO: Check if this is the right byte order from C++ uint32
			destination[0 + offset] = (byte)source.GetBits(8, 24);
			destination[1 + offset] = (byte)source.GetBits(8, 16);
			destination[2 + offset] = (byte)source.GetBits(8, 8);
			destination[3 + offset] = (byte)source.GetBits(8);
		}

		private static void DecodeBits(ReadOnlySpan<byte> source, Span<byte> destination, int symbolCount, int bitCount, ref int sourcePosition, ref int bitPosition)
		{
			if (bitCount == 0)
			{
				return;
			}

			int localSourcePosition = sourcePosition;
			int localBitPosition = bitPosition;
			int destinationPosition = 0;
			int bitsRequired = bitPosition + bitCount * symbolCount;
			int bytesRequired = (bitsRequired + 7) >> 3;
			if (bytesRequired > source.Length - sourcePosition)
			{
				throw new InvalidDataException($"Error decoding golomb rice bits: required {bytesRequired} source bytes, but got only {source.Length - sourcePosition} bytes");
			}

			sourcePosition += bitsRequired >> 3;
			bitPosition = bitsRequired.GetBits(3);

			if (bitCount < 2)
			{
				Debug.Assert(bitCount == 1);

				do
				{
					// TODO: continue here
					// DWord.FromBytes(source[localBitPosition
				} while (destinationPosition != destination.Length);
			}
		}

		private static readonly uint[] Bits2Value = new uint[] {
			0x80000000, 0x00000007, 0x10000006, 0x00000006, 0x20000005, 0x00000105, 0x10000005, 0x00000005, 0x30000004, 0x00000204, 0x10000104, 0x00000104, 0x20000004, 0x00010004, 0x10000004, 0x00000004, 0x40000003, 0x00000303, 0x10000203, 0x00000203, 0x20000103, 0x00010103, 0x10000103, 0x00000103, 0x30000003, 0x00020003, 0x10010003, 0x00010003, 0x20000003, 0x01000003, 0x10000003, 0x00000003, 0x50000002, 0x00000402, 0x10000302, 0x00000302, 0x20000202, 0x00010202, 0x10000202, 0x00000202, 0x30000102, 0x00020102, 0x10010102, 0x00010102, 0x20000102, 0x01000102, 0x10000102, 0x00000102, 0x40000002, 0x00030002, 0x10020002, 0x00020002, 0x20010002, 0x01010002, 0x10010002, 0x00010002, 0x30000002, 0x02000002, 0x11000002, 0x01000002, 0x20000002, 0x00000012, 0x10000002, 0x00000002, 0x60000001, 0x00000501, 0x10000401, 0x00000401, 0x20000301, 0x00010301, 0x10000301, 0x00000301, 0x30000201, 0x00020201, 0x10010201, 0x00010201, 0x20000201, 0x01000201, 0x10000201, 0x00000201, 0x40000101, 0x00030101, 0x10020101, 0x00020101, 0x20010101, 0x01010101, 0x10010101, 0x00010101, 0x30000101, 0x02000101, 0x11000101, 0x01000101, 0x20000101, 0x00000111, 0x10000101, 0x00000101, 0x50000001, 0x00040001, 0x10030001, 0x00030001, 0x20020001, 0x01020001, 0x10020001, 0x00020001, 0x30010001, 0x02010001, 0x11010001, 0x01010001, 0x20010001, 0x00010011, 0x10010001, 0x00010001, 0x40000001, 0x03000001, 0x12000001, 0x02000001, 0x21000001, 0x01000011, 0x11000001, 0x01000001, 0x30000001, 0x00000021, 0x10000011, 0x00000011, 0x20000001, 0x00001001, 0x10000001, 0x00000001, 0x70000000, 0x00000600, 0x10000500, 0x00000500, 0x20000400, 0x00010400, 0x10000400, 0x00000400, 0x30000300, 0x00020300, 0x10010300, 0x00010300, 0x20000300, 0x01000300, 0x10000300, 0x00000300, 0x40000200, 0x00030200, 0x10020200, 0x00020200, 0x20010200, 0x01010200, 0x10010200, 0x00010200, 0x30000200, 0x02000200, 0x11000200, 0x01000200, 0x20000200, 0x00000210, 0x10000200, 0x00000200, 0x50000100, 0x00040100, 0x10030100, 0x00030100, 0x20020100, 0x01020100, 0x10020100, 0x00020100, 0x30010100, 0x02010100, 0x11010100, 0x01010100, 0x20010100, 0x00010110, 0x10010100, 0x00010100, 0x40000100, 0x03000100, 0x12000100, 0x02000100, 0x21000100, 0x01000110, 0x11000100, 0x01000100, 0x30000100, 0x00000120, 0x10000110, 0x00000110, 0x20000100, 0x00001100, 0x10000100, 0x00000100, 0x60000000, 0x00050000, 0x10040000, 0x00040000, 0x20030000, 0x01030000, 0x10030000, 0x00030000, 0x30020000, 0x02020000, 0x11020000, 0x01020000, 0x20020000, 0x00020010, 0x10020000, 0x00020000, 0x40010000, 0x03010000, 0x12010000, 0x02010000, 0x21010000, 0x01010010, 0x11010000, 0x01010000, 0x30010000, 0x00010020, 0x10010010, 0x00010010, 0x20010000, 0x00011000, 0x10010000, 0x00010000, 0x50000000, 0x04000000, 0x13000000, 0x03000000, 0x22000000, 0x02000010, 0x12000000, 0x02000000, 0x31000000, 0x01000020, 0x11000010, 0x01000010, 0x21000000, 0x01001000, 0x11000000, 0x01000000, 0x40000000, 0x00000030, 0x10000020, 0x00000020, 0x20000010, 0x00001010, 0x10000010, 0x00000010, 0x30000000, 0x00002000, 0x10001000, 0x00001000, 0x20000000, 0x00100000, 0x10000000, 0x00000000
		};

		private static readonly byte[] Bits2Length = new byte[] {
			0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
		};
	}
}
