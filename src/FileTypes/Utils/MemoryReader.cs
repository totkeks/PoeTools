using System;
using System.Buffers.Binary;
using System.Text;

namespace PoETool.FileTypes.Utils {
	public ref struct MemoryReader {
		private ReadOnlySpan<byte> Data { get; }
		public int Position { get; private set; }
		public int Length { get => Data.Length; }

		public MemoryReader(ReadOnlySpan<byte> data, int position = 0) {
			Data = data;
			Position = position;
		}

		public ReadOnlySpan<byte> Read(int count, bool peek = false) {
			var slice = Data.Slice(Position, count);
			Position += peek ? 0 : count;

			return slice;
		}

		public bool ReadBoolean() {
			return Convert.ToBoolean(Data[Position++]);
		}

		public byte ReadByte() {
			return Data[Position++];
		}

		public short ReadInt16() {
			return BinaryPrimitives.ReadInt16LittleEndian(Read(2));
		}

		public int ReadInt32() {
			return BinaryPrimitives.ReadInt32LittleEndian(Read(4));
		}

		public uint ReadUInt32() {
			return BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
		}

		public long ReadInt64() {
			return BinaryPrimitives.ReadInt64LittleEndian(Read(8));
		}

		public ulong ReadUInt64() {
			return BinaryPrimitives.ReadUInt64LittleEndian(Read(8));
		}

		public float ReadSingle() {
			return BinaryPrimitives.ReadSingleLittleEndian(Read(4));
		}

		public double ReadDouble() {
			return BinaryPrimitives.ReadDoubleLittleEndian(Read(8));
		}

		public string ReadString(int length) {
			return Encoding.UTF8.GetString(Read(length));
		}

		public string ReadNullTerminatedString() {
			int offset = 0;

			while (Data[Position + offset++] != '\0') { }
			string str = Encoding.ASCII.GetString(Data.Slice(Position, offset - 1));
			Position += offset;

			return str;
		}

		public dynamic Read<T>() where T : struct {
			return default(T) switch {
				bool => ReadBoolean(),
				byte => ReadByte(),
				short => ReadInt16(),
				int => ReadInt32(),
				uint => ReadUInt32(),
				long => ReadInt64(),
				ulong => ReadUInt64(),
				float => ReadSingle(),
				double => ReadDouble(),
				_ => throw new NotImplementedException("Type not implemented")
			};
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format($"MemoryReader at position {Position} out of {Length}");
		}
	}
}

