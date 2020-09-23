using System.Numerics;

namespace PoeTools.Util.Oodle
{
	internal class Huffman
	{
		public static int ReadCodeLengthsOld(BitReader bits, ref byte[] syms, ref int[] codePrefix)
		{
			if (bits.ReadBit(false))
			{
				int n, sym = 0, codeLength, symbolCount = 0;
				int avgBitsTimes4 = 32;
				int forcedBits = bits.ReadBits(2);
				bool skipInitialZeroes = false;

				uint validGammaThreshold = (uint)(1 << (31 - (20 >> forcedBits)));
				if (bits.ReadBit())
				{
					skipInitialZeroes = true;
				}

				do
				{
					if (!skipInitialZeroes)
					{
						if ((bits.Bits & 0xFF000000u) == 0)
						{
							// TODO: Error, too many zeroes
							return -1;
						}

						sym += bits.ReadBits(2 * (BitOperations.LeadingZeroCount(bits.Bits) + 1)) - 2 + 1;
						if (sym >= 256)
						{
							break;
						}
					}

					if ((bits.Bits & 0xFF000000u) == 0)
					{
						// TODO: Error, too many zeroes
						return -1;
					}

					n = bits.ReadBits(2 * (BitOperations.LeadingZeroCount(bits.Bits) + 1)) - 2 + 1;

					if (sym + n > 256)
					{
						// TODO: Overflow error
						return -1;
					}

					bits.Refill();
					symbolCount += n;

					do
					{
						if (bits.Bits < validGammaThreshold)
						{
							// TODO: Error: too big gamma value?
							return -1;
						}

						int zeroes = BitOperations.LeadingZeroCount(bits.Bits);
						int v = bits.ReadBits(zeroes + forcedBits + 1) + ((zeroes - 1) << forcedBits);

						codeLength = (-(int)(v & 1) ^ (v >> 1)) + ((avgBitsTimes4 + 2) >> 2);
						if (codeLength < 1 || codeLength > 11)
						{
							// TODO: Error: codelen out of bounds
							return -1;
						}

						avgBitsTimes4 = codeLength + ((3 * avgBitsTimes4 + 2) >> 2);
						bits.Refill();
						syms[codePrefix[codeLength]++] = (byte)sym++;
					} while ((--n) > 0);
				} while (sym != 256);

				return (sym == 256) && (symbolCount >= 2) ? symbolCount : -1;
			}
			else
			{
				// Sparse symbol encoding (???)
				int symbolCount = bits.ReadBits(8);
				if (symbolCount == 0)
				{
					// TODO: add error
					return -1;
				}
				else if (symbolCount == 1)
				{
					syms[0] = (byte)bits.ReadBits(8);
				}
				else
				{
					int codeBitLength = bits.ReadBits(3);
					if (codeBitLength > 4)
					{
						// TODO: add error
						return -1;
					}

					for (int ii = 0; ii < symbolCount; ii++)
					{
						bits.Refill();
						int sym = bits.ReadBits(8);
						int codeLength = bits.ReadBitsZero(codeBitLength) + 1;
						if (codeLength > 11)
						{
							// TODO: add error
							return -1;
						}
						syms[codePrefix[codeLength]++] = (byte)sym;
					}
				}

				return symbolCount;
			}
		}
	}
}
