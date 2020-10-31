using System;
using System.Buffers.Binary;

namespace PoETool.FileTypes.Utils {
	public ref struct MemoryReader {
		private ReadOnlySpan<byte> Data { get; }
		public int Position { get; private set; }

		public MemoryReader(ReadOnlySpan<byte> data, int position = 0) {
			Data = data;
			Position = position;
		}

		public ReadOnlySpan<byte> Read(int count, bool peek = false) {
			var slice = Data.Slice(Position, count);
			Position += peek ? 0 : count;

			return slice;
		}

		public dynamic Read<T>() where T : struct {
			return (default(T)) switch
			{
				bool => Convert.ToBoolean(Read(1)[0]),
				byte => Read(1)[0],
				short => BinaryPrimitives.ReadInt16LittleEndian(Read(2)),
				int => BinaryPrimitives.ReadInt32LittleEndian(Read(4)),
				uint => BinaryPrimitives.ReadUInt32LittleEndian(Read(4)),
				long => BinaryPrimitives.ReadInt64LittleEndian(Read(8)),
				ulong => BinaryPrimitives.ReadUInt64LittleEndian(Read(8)),
				float => BinaryPrimitives.ReadSingleLittleEndian(Read(4)),
				_ => throw new NotImplementedException("Type not implemented")
			};
		}
	}
}

