using System;

namespace PoeTools.Util.ExtensionMethods
{
	public static class BitExtensions
	{
		/* Int */
		public static bool SelectBit(this int self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static int SelectBits(this int self, int amount, int offset = 0)
		{
			return (self >> offset) & (2 << amount - 1);
		}

		public static int SetBit(this int self, int index)
		{
			return self | (1 << (index - 1));
		}

		public static int UnsetBit(this int self, int index)
		{
			return self & ~(1 << (index - 1));
		}

		public static ushort SetBit(this ushort self, int index)
		{
			return (ushort)(self | (1 << (index - 1)));
		}

		public static ushort UnsetBit(this ushort self, int index)
		{
			return (ushort)(self & ~(1 << (index - 1)));
		}

		/* Bool */
		public static bool SelectBit(this byte self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static int SelectBits(this byte self, int amount, int offset = 0)
		{
			return (self >> offset) & (2 << amount - 1);
		}
	}

	public static class Word
	{
		public static ushort FromBytes(byte two, byte one)
		{
			return (ushort)(two << 8 | one);
		}
	}

	public static class DWord
	{
		public static int FromBytes(byte two, byte one)
		{
			return FromBytes(0, 0, two, one);
		}

		public static int FromBytes(byte three, byte two, byte one)
		{
			return FromBytes(0, three, two, one);
		}

		public static int FromBytes(byte four, byte three, byte two, byte one)
		{
			return four << 24 | three << 16 | two << 8 | one;
		}
	}
}
