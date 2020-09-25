using PoeTools.Util.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;

namespace PoeTools.Util.Oodle
{
	internal static class ByteDecoder
	{
		private static readonly uint[] initialCodePrefix = new uint[] { 0x0, 0x0, 0x2, 0x6, 0xE, 0x1E, 0x3E, 0x7E, 0xFE, 0x1FE, 0x2FE, 0x3FE };

		public static int Decode(ReadOnlyMemory<byte> source, out Memory<byte> destination)
		{
			var chunkType = source[0..1].Span[0].GetBits(3, 4);
			if (chunkType == 0)
			{
				return CopySource(source, out destination);
			}

			return DecompressSource(source, out destination, chunkType);
		}

		private static int CopySource(ReadOnlyMemory<byte> source, out Memory<byte> destination)
		{
			int encodedSize;
			int offset;
			var temp = source[0..3].Span;

			if (temp[0] >= 0x80)
			{
				encodedSize = DWord.FromBytes(temp[0], temp[1]).GetBits(12);
				offset = 2;
			}
			else
			{
				encodedSize = DWord.FromBytes(temp[0], temp[1], temp[2]);
				offset = 3;

				if (encodedSize.GetBits(6, 18) > 0)
				{
					throw new InvalidDataException("Error decoding: reserved bits must not be set");
				}
			}

			destination = new byte[encodedSize];
			source[offset..encodedSize].CopyTo(destination);

			return encodedSize;
		}

		private static int DecompressSource(ReadOnlyMemory<byte> source, out Memory<byte> destination, int chunkType)
		{
			int encodedSize;
			int decodedSize;
			int offset = 0;

			var firstByte = source[0..1].Span[0];

			if (firstByte >= 0x80)
			{ // Short mode, 10 bit size descriptors
				var temp = source[0..3].Span;
				var descriptors = DWord.FromBytes(temp[0], temp[1], temp[2]);

				encodedSize = descriptors.GetBits(10);
				decodedSize = encodedSize + descriptors.GetBits(10, 10) + 1;
				offset += 3;
			}
			else
			{ // Long mode, 18 bit size descriptors
				var temp = source[0..5].Span;
				var descriptors = DWord.FromBytes(temp[1], temp[2], temp[3], temp[4]);

				encodedSize = descriptors.GetBits(18);
				decodedSize = (descriptors.GetBits(14, 18) | (temp[0] << 14)).GetBits(18) + 1;
				offset += 5;
			}

			var relevantSource = source[offset..encodedSize];
			destination = new byte[decodedSize];

			switch (chunkType)
			{
				case 2:
				case 4:
					DecodeType12(relevantSource, destination, chunkType >> 1);
					break;

				case 5:
					DecodeRecursive(relevantSource, destination);
					break;

				case 3:
					DecodeRLE(relevantSource, destination);
					break;

				case 1:
					DecodeTANS(relevantSource, destination);
					break;

				default:
					throw new InvalidDataException($"Error decoding: chunk type {chunkType} is invalid");
			}

			return offset + encodedSize;
		}

		private static void DecodeType12(ReadOnlyMemory<byte> source, Memory<byte> destination, int type)
		{
			Debug.WriteLine($"Decoding {source.Length} bytes using Huffman coding into {destination.Length} bytes. (type = {type})");

			var bits = new BitReader(source, 0, 24);
			bits.Refill();

			uint[] codePrefix = (uint[])initialCodePrefix.Clone();
			var symbols = new byte[1280];
			int symbolCount;

			if (!bits.ReadBit(false))
			{
				symbolCount = Huffman.ReadCodeLengths(bits, ref symbols, ref codePrefix);
			}
			else if (!bits.ReadBit(false))
			{
				symbolCount = GolombRice.ReadCodeLengths(bits, ref symbols, ref codePrefix);
			}
			else
			{
				throw new InvalidDataException($"Error decoding type {type}: symbols are neither encoded with the old or the new Huffman encoding");
			}


		}

		private static void DecodeRecursive(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decoding {source.Length} bytes using recursive coding into {destination.Length} bytes.");

			var firstByte = source[0..1].Span[0];
			int amount = firstByte.GetBits(7);

			if (!firstByte.GetBit(8))
			{
				int offset = 1;

				do
				{
					offset += Decode(source[offset..], out var buffer);
					buffer.CopyTo(destination);
					destination = destination[buffer.Length..];
				} while (--amount != 0);
			}
			else
			{
				//DecodeMultiArray();
			}
		}

		private static void DecodeRLE(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decoding {source.Length} bytes using run-length encoding into {destination.Length} bytes.");
		}

		private static void DecodeTANS(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decoding {source.Length} bytes using tabled asymmetric numeral systems into {destination.Length} bytes.");
		}

		private static void DecodeMultiArray(ReadOnlyMemory<byte> source, Memory<byte> destination)
		{
			Debug.WriteLine($"Decoding {source.Length} bytes using multi array coding into {destination.Length} bytes.");
		}
	}
}
