using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class BitReader
	{
		private readonly ReadOnlyMemory<byte> data;
		private readonly BinaryReader reader;
		private uint bits;
		private int bitPosition;

		public ReadOnlyMemory<byte> Source { get => data; }
		public uint Bits { get => bits; }
		public int BitPosition { get => bitPosition; }
		public int DataPosition { get => (int)reader.BaseStream.Position; }

		public BitReader(ReadOnlyMemory<byte> data, uint bits, int bitPosition, int initialPosition = 0)
		{
			this.data = data;
			reader = new BinaryReader(new MemoryStream(data.ToArray(), false));
			reader.BaseStream.Seek(initialPosition, SeekOrigin.Begin);
			this.bits = bits;
			this.bitPosition = bitPosition;
		}

		public void Refill()
		{
			Debug.Assert(bitPosition <= 24);

			while (bitPosition > 0)
			{
				bits |= (reader.BaseStream.Position < reader.BaseStream.Length ? reader.ReadByte() : 0u) << bitPosition;
				bitPosition -= 8;
			}
		}

		public void RefillBackwards()
		{
			Debug.Assert(bitPosition <= 24);

			while (bitPosition > 0)
			{
				reader.BaseStream.Seek(-1, SeekOrigin.Current);
				bits |= (reader.BaseStream.Position >= reader.BaseStream.Length ? (uint)reader.PeekChar() : 0) << bitPosition;
				bitPosition -= 8;
			}
		}

		public bool ReadBit(bool refill = true)
		{
			if (refill) Refill();

			bool result = bits.GetBit(32);
			bits <<= 1;
			bitPosition += 1;

			return result;
		}

		public int ReadBits(int amount)
		{
			int result = (int)(bits >> (32 - amount));
			bits <<= amount;
			bitPosition += amount;

			return result;
		}

		public int ReadBitsZero(int amount)
		{
			int result = (int)(bits >> 1 >> (31 - amount));
			bits <<= amount;
			bitPosition += amount;

			return result;
		}

		public uint ReadMoreThan24Bits(int amount, bool backwards = false)
		{
			uint result;
			Action refill = backwards ? RefillBackwards : Refill;

			if (amount <= 24)
			{
				result = (uint)ReadBitsZero(amount);
			}
			else
			{
				result = (uint)ReadBits(24) << (amount - 24);
				refill();
				result += (uint)ReadBits(amount - 24);
			}

			refill();
			return result;
		}

		public int ReadGamma()
		{
			int zeroes = 32;

			if (bits != 0)
			{
				zeroes = 31 - BitOperations.LeadingZeroCount(bits);
			}

			int amount = (zeroes << 1) + 2;
			Debug.Assert(amount < 24);

			int result = (int)(bits >> (32 - amount));
			bitPosition += amount;
			bits <<= amount;

			return result - 2;
		}

		public int ReadGammaX(int forcedBits)
		{
			if (bits == 0)
			{
				return 0;
			}

			int amount = 31 - BitOperations.LeadingZeroCount(bits);
			Debug.Assert(amount < 24);

			int result = (int)((bits >> (31 - amount - forcedBits)) + ((amount - 1) << forcedBits));
			bitPosition += amount + forcedBits + 1;
			bits <<= amount + forcedBits + 1;

			return result;
		}

		public uint ReadDistance(uint value, bool backwards = false)
		{
			uint result;
			uint w;
			uint mask;
			int shiftAmount;
			Action refill = backwards ? RefillBackwards : Refill;

			if (value < 0xF0)
			{
				shiftAmount = (int)((value >> 4) + 4);
				w = BitOperations.RotateLeft(bits | 1, shiftAmount);
				mask = (uint)((2 << shiftAmount) - 1);

				bitPosition += shiftAmount;
				bits = w & ~mask;

				result = ((w & mask) << 4) + (value & 0xF) - 0xF8;
			}
			else
			{
				shiftAmount = (int)(value - 0xF0 + 4);
				w = BitOperations.RotateLeft(bits | 1, shiftAmount);
				mask = (uint)((2 << shiftAmount) - 1);

				bitPosition += shiftAmount;
				bits = w & ~mask;

				result = 0x7EFF00 + ((w & mask) << 12);
				refill();
				result += bits >> 20;

				bitPosition += 12;
				bits <<= 12;
			}

			refill();
			return result;
		}

		public uint ReadLength(bool backwards = false)
		{
			Action refill = backwards ? RefillBackwards : Refill;
			int amount = 31 - BitOperations.LeadingZeroCount(bits);
			Debug.Assert(amount <= 12);

			bitPosition += amount;
			bits <<= amount;
			refill();

			amount += 7;
			uint result = (bits >> (32 - amount)) - 0x40;
			bitPosition += amount;
			bits <<= amount;
			refill();

			return result;
		}

		public int ReadFluff(int symbolCount)
		{
			if (symbolCount == 256)
			{
				return 0;
			}

			uint x = (uint)(257 - symbolCount);
			if (x > symbolCount)
			{
				x = (uint)symbolCount;
			}
			x <<= 1;

			int zeroes = BitOperations.LeadingZeroCount(x - 1) + 1;

			uint v = bits >> (32 - zeroes);
			uint z = (1u << zeroes) - x;

			if ((v >> 1) >= z)
			{
				bits <<= zeroes;
				bitPosition += zeroes;
				return (int)(v - z);
			}

			bits <<= zeroes - 1;
			bitPosition += zeroes - 1;
			return (int)(v >> 1);
		}
	}
}
