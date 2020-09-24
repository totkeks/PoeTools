using System;

namespace PoeTools.Util.ExtensionMethods
{
	// internal interface IBitExtensions<T> {
	// 	bool GetBit(T self, int index);
	// 	T GetBits(T self, int amount, int offset);
	// 	T SetBit(T self, int index);
	// 	T UnsetBit(T self, int index);
	// }

	public static class LongBitExtensions
	{
		public static bool GetBit(this long self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static long GetBits(this long self, int amount, int offset = 0)
		{
			return (self >> offset) & ((1 << amount) - 1);
		}

		public static long SetBit(this long self, int index)
		{
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
			return self | (1 << (index - 1));
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
		}

		public static long UnsetBit(this long self, int index)
		{
			return self & ~(1 << (index - 1));
		}
	}

	public static class IntBitExtensions
	{
		public static bool GetBit(this int self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static int GetBits(this int self, int amount, int offset = 0)
		{
			return (self >> offset) & ((1 << amount) - 1);
		}

		public static int SetBit(this int self, int index)
		{
			return self | (1 << (index - 1));
		}

		public static int UnsetBit(this int self, int index)
		{
			return self & ~(1 << (index - 1));
		}
	}

	public static class ShortBitExtensions
	{
		public static bool GetBit(this short self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static short GetBits(this short self, int amount, int offset = 0)
		{
			return (short)((self >> offset) & ((1 << amount) - 1));
		}

		public static short SetBit(this short self, int index)
		{
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
			return (short)(self | (1 << (index - 1)));
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
		}

		public static short UnsetBit(this short self, int index)
		{
			return (short)(self & ~(1 << (index - 1)));
		}
	}

	public static class ULongBitExtensions
	{
		public static bool GetBit(this ulong self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static ulong GetBits(this ulong self, int amount, int offset = 0)
		{
			return (self >> offset) & ((1UL << amount) - 1);
		}

		public static ulong SetBit(this ulong self, int index)
		{
			return self | (1UL << (index - 1));
		}

		public static ulong UnsetBit(this ulong self, int index)
		{
			return self & ~(1UL << (index - 1));
		}
	}

	public static class UIntBitExtensions
	{
		public static bool GetBit(this uint self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static uint GetBits(this uint self, int amount, int offset = 0)
		{
			return (self >> offset) & ((1U << amount) - 1);
		}

		public static uint SetBit(this uint self, int index)
		{
			return self | (1U << (index - 1));
		}

		public static uint UnsetBit(this uint self, int index)
		{
			return self & ~(1U << (index - 1));
		}
	}

	public static class UShortBitExtensions
	{
		public static bool GetBit(this ushort self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static ushort GetBits(this ushort self, int amount, int offset = 0)
		{
			return (ushort)((self >> offset) & ((1U << amount) - 1));
		}

		public static ushort SetBit(this ushort self, int index)
		{
			return (ushort)(self | (1U << (index - 1)));
		}

		public static ushort UnsetBit(this ushort self, int index)
		{
			return (ushort)(self & ~(1U << (index - 1)));
		}
	}

	public static class ByteBitExtensions
	{
		public static bool GetBit(this byte self, int index)
		{
			return Convert.ToBoolean((self >> (index - 1)) & 1);
		}

		public static byte GetBits(this byte self, int amount, int offset = 0)
		{
			return (byte)((self >> offset) & ((1U << amount) - 1));
		}

		public static byte SetBit(this byte self, int index)
		{
			return (byte)(self | (1U << (index - 1)));
		}

		public static byte UnsetBit(this byte self, int index)
		{
			return (byte)(self & ~(1U << (index - 1)));
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
