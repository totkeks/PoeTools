using System.IO;
using System.Numerics;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class Huffman
	{
		public static int ReadCodeLengths(BitReader bits, ref byte[] symbols, ref uint[] codePrefix)
		{
			if (bits.ReadBit(false))
			{
				return DecodeGammaSymbols(bits, ref symbols, ref codePrefix);
			}

			return DecodeSparseSymbols(bits, ref symbols, ref codePrefix);
		}

		private static int DecodeGammaSymbols(BitReader bits, ref byte[] symbols, ref uint[] codePrefix)
		{
			int n, symbol = 0, codeLength, symbolCount = 0;
			int avgBitsTimes4 = 32;

			int forcedBits = bits.ReadBits(2);
			uint validGammaThreshold = (uint)(1 << (31 - (20 >> forcedBits)));

			bool skipInitialZeroes = bits.ReadBit();

			do
			{
				if (!skipInitialZeroes)
				{
					if (bits.Bits.GetBits(8, 24) == 0)
					{
						throw new InvalidDataException("Error decoding gamma symbols: too many zeroes in bit stream");
					}

					symbol += bits.ReadBits(2 * (BitOperations.LeadingZeroCount(bits.Bits) + 1)) - 2 + 1;
					if (symbol >= 256)
					{
						break;
					}
				}

				if (bits.Bits.GetBits(8, 24) == 0)
				{
					throw new InvalidDataException("Error decoding gamma symbols: too many zeroes in bit stream");
				}

				n = bits.ReadBits(2 * (BitOperations.LeadingZeroCount(bits.Bits) + 1)) - 2 + 1;

				if (symbol + n > 256)
				{
					throw new InvalidDataException($"Error decoding gamma symbols: new symbol {symbol + n} ({symbol} + {n}) must be smaller than or equal to 256");
				}

				bits.Refill();
				symbolCount += n;

				do
				{
					if (bits.Bits < validGammaThreshold)
					{
						throw new InvalidDataException($"Error decoding gamma symbols: {bits.Bits} is below the threshold {validGammaThreshold} for a valid gamma value");
					}

					int zeroes = BitOperations.LeadingZeroCount(bits.Bits);
					int v = bits.ReadBits(zeroes + forcedBits + 1) + ((zeroes - 1) << forcedBits);

					codeLength = (-(int)(v & 1) ^ (v >> 1)) + ((avgBitsTimes4 + 2) >> 2);
					if (codeLength < 1 || codeLength > 11)
					{
						throw new InvalidDataException($"Error decoding gamma symbols: code length {codeLength} must be between 1 and 11");
					}

					avgBitsTimes4 = codeLength + ((3 * avgBitsTimes4 + 2) >> 2);
					bits.Refill();
					symbols[codePrefix[codeLength]++] = (byte)symbol++;
				} while ((--n) > 0);
			} while (symbol != 256);

			if (symbol != 256)
			{
				throw new InvalidDataException($"Error decoding gamma symbols: last symbol {symbol} must be 256");
			}

			if (symbolCount < 2)
			{
				throw new InvalidDataException($"Error decoding gamma symbols: must have at least 2 symbols, but found only {symbolCount}");
			}

			return symbolCount;
		}

		private static int DecodeSparseSymbols(BitReader bits, ref byte[] symbols, ref uint[] codePrefix)
		{
			int symbolCount = bits.ReadBits(8);
			if (symbolCount == 0)
			{
				throw new InvalidDataException($"Error decoding sparse symbols: symbol count is zero");
			}

			if (symbolCount == 1)
			{
				symbols[0] = (byte)bits.ReadBits(8);
				return symbolCount;
			}

			int codeBitLength = bits.ReadBits(3);
			if (codeBitLength > 4)
			{
				throw new InvalidDataException($"Error decoding sparse symbols: code bit length must be smaller than 5, but is {codeBitLength}");
			}

			for (int ii = 0; ii < symbolCount; ii++)
			{
				bits.Refill();
				int symbol = bits.ReadBits(8);
				int codeLength = bits.ReadBitsZero(codeBitLength) + 1;
				if (codeLength > 11)
				{
					throw new InvalidDataException($"Error decoding sparse symbols: code length must be smaller than 12, but is {codeLength}");
				}
				symbols[codePrefix[codeLength]++] = (byte)symbol;
			}

			return symbolCount;
		}
	}
}
