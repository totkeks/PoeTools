using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class KrakenLzTable
	{
		private ReadOnlyMemory<byte> initialSource;
		private Memory<byte> initialDestination;

		private ReadOnlyMemory<byte> remainingSource;
		private Memory<byte> remainingDestination;

		private Memory<byte> packedLengths;
		private Memory<byte> packedOffsets;
		private Memory<byte> packedOffsetsExtra;
		private int offsetScaling;

		private byte[] commands;
		private int[] offsets;
		private byte[] literals;
		private int[] lengths;

		public int SourceBytesUsed
		{
			get => initialSource.Length - remainingSource.Length;
		}

		public int DestinationBytesUsed
		{
			get => initialDestination.Length - remainingDestination.Length;
		}

		private int SourceBytesLeft
		{
			get => remainingSource.Length;
		}

		private int DestinationBytesLeft
		{
			get => remainingDestination.Length;
		}

		public void ReadTable(ReadOnlyMemory<byte> source, Memory<byte> destination, int mode, int offset)
		{
			Debug.WriteLine($"Reading Kraken LZ table from offset {offset}. (mode = {mode})");

			initialSource = source;
			initialDestination = destination;

			remainingSource = source;
			remainingDestination = destination;

			if (mode > 1)
			{
				throw new InvalidDataException($"Error reading LZ table: mode is greater than 1 ({mode})");
			}

			if (SourceBytesLeft < 13)
			{
				throw new InvalidDataException($"Error reading LZ table: currentSource must be at least 13 bytes long, but was only {SourceBytesLeft} bytes long");
			}

			if (offset == 0)
			{
				remainingSource[0..8].CopyTo(remainingDestination);
				ProgressSource(8);
				ProgressDestination(8);
			}

			var flag = remainingSource[0..1].Span[0];
			if (flag.GetBit(8))
			{
				if (flag.GetBit(7))
				{
					throw new InvalidDataException("Error reading LZ table: reserved flag set");
				}

				throw new InvalidDataException("Error reading LZ table: flag contains unsupported bits");
			}

			ProgressSource(ByteDecoder.Decode(remainingSource, out var buffer));
			literals = buffer.ToArray();

			ProgressSource(ByteDecoder.Decode(remainingSource, out buffer));
			commands = buffer.ToArray();

			if (SourceBytesLeft < 3)
			{
				return;
			}

			var temp = remainingSource[0..1].Span[0];
			if (temp.GetBit(8))
			{
				offsetScaling = temp - 127;
				ProgressSource(1);

				ProgressSource(ByteDecoder.Decode(remainingSource, out packedOffsets));

				if (offsetScaling != 1)
				{
					ProgressSource(ByteDecoder.Decode(remainingSource, out packedOffsetsExtra));
				}
			}
			else
			{
				ProgressSource(ByteDecoder.Decode(remainingSource, out packedOffsets));
			}

			ProgressSource(ByteDecoder.Decode(remainingSource, out packedLengths));


			UnpackOffsets();
		}

		private void UnpackOffsets()
		{
			BitReader forwardReader = new BitReader(remainingSource, 0, 24);
			BitReader backwardReader = new BitReader(remainingSource, 0, 24, SourceBytesLeft);

			forwardReader.Refill();
			backwardReader.RefillBackwards();

			int zeroes = 31 - BitOperations.LeadingZeroCount(backwardReader.Bits);
			backwardReader.ReadBits(zeroes);
			backwardReader.RefillBackwards();
			zeroes++;

			uint lengthsSize = (backwardReader.Bits >> (32 - zeroes)) - 1;
			backwardReader.ReadBits(zeroes);
			backwardReader.RefillBackwards();

			var localOffsets = packedOffsets.Span;
			offsets = new int[localOffsets.Length];

			if (offsetScaling == 0)
			{
				for (int ii = 0; ii < localOffsets.Length; ii += 2)
				{
					offsets[ii] = -(int)forwardReader.ReadDistance(localOffsets[ii]);
					if (ii + 1 == localOffsets.Length) { break; }
					offsets[ii + 1] = -(int)backwardReader.ReadDistance(localOffsets[ii + 1], true);
				}
			}
			else
			{
				byte command;

				for (int ii = 0; ii < localOffsets.Length; ii += 2)
				{
					command = localOffsets[ii];
					if ((command >> 3) > 26) { return; }
					offsets[ii] = 8 - (((8 + command.GetBits(3)) << (command >> 3)) | (int)forwardReader.ReadMoreThan24Bits(command >> 3));

					if (ii + 1 == localOffsets.Length) { break; }

					command = localOffsets[ii + 1];
					if ((command >> 3) > 26) { return; }
					offsets[ii + 1] = 8 - (((8 + command.GetBits(3)) << (command >> 3)) | (int)backwardReader.ReadMoreThan24Bits(command >> 3, true));
				}

				if (offsetScaling != 1)
				{
					var localOffsetsExtra = packedOffsetsExtra.Span;
					for (int ii = 0; ii < localOffsetsExtra.Length; ii++)
					{
						offsets[ii] = offsetScaling * offsets[ii] - localOffsetsExtra[ii];
					}
				}
			}

			var lengthsBuffer = new uint[lengthsSize];
			for (int ii = 0; ii < lengthsSize; ii += 2)
			{
				lengthsBuffer[ii] = forwardReader.ReadLength();
				if (ii + 1 == lengthsSize) { break; }
				lengthsBuffer[ii + 1] = backwardReader.ReadLength(true);
			}

			var localLengths = packedOffsets.Span;
			lengths = new int[localLengths.Length];
			int bufferPosition = 0;
			for (int ii = 0; ii < localLengths.Length; ii++)
			{
				uint packedValue = localLengths[ii];
				if (packedValue == 255)
				{
					packedValue += lengthsBuffer[bufferPosition++];
				}
				lengths[ii] = (int)(packedValue + 3);
			}
		}

		private void ProgressSource(int amount)
		{
			remainingSource = remainingSource[amount..];
		}

		private void ProgressDestination(int amount)
		{
			remainingDestination = remainingDestination[amount..];
		}
	}
}
