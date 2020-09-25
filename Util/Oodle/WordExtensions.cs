using System;

namespace PoeTools.Util.ExtensionMethods
{
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

	public static class SwapExtensions
	{
		public static uint SwapBytes(this uint dword)
		{
			return ((dword >> 24) & 0x0000_00FF) | ((dword >> 8) & 0x0000_FF00) | ((dword << 8) & 0x00FF_0000) | ((dword << 24) & 0xFF00_0000);
		}

		public static ulong SwapBytes(this ulong dword)
		{
			return ((dword >> 56) & 0x0000_0000_0000_00FF) | ((dword >> 40) & 0x0000_0000_0000_FF00) | ((dword >> 24) & 0x0000_0000_00FF_0000) | ((dword >> 8) & 0x0000_0000_FF00_0000) | ((dword << 8) & 0x0000_00FF_0000_0000) | ((dword << 24) & 0x0000_FF00_0000_0000) | ((dword << 40) & 0x00FF_0000_0000_0000) | ((dword << 56) & 0xFF00_0000_0000_0000);
		}
	}
}
