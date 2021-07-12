using System.Collections.Generic;
using System.IO;
using System.Linq;

using PoETool.FileTypes.Bundle;
using PoETool.FileTypes.Utils;

namespace PoETool.FileTypes.Index {
	public class DirectoryRecord {
		public ulong Hash { get; }
		public int Offset { get; }
		public int Size { get; }

		public DirectoryRecord(ref MemoryReader reader) {
			Hash = reader.ReadUInt64();
			Offset = reader.ReadInt32();
			Size = reader.ReadInt32();
			reader.ReadUInt32(); // Unknown
		}

		public List<string> GetFilePaths(BundleFile bundle) {
			MemoryReader reader = new(bundle.GetContent(Offset, Size).Span);

			var templates = BuildPathTemplates(ref reader);
			return BuildFilePaths(ref reader, templates);
		}

		private static Dictionary<int, string> BuildPathTemplates(ref MemoryReader reader) {
			var templates = new Dictionary<int, string>();
			string nextPath = string.Empty;
			int dword = reader.ReadInt32();

			if (dword != 0) {
				throw new InvalidDataException($"Error building path templates: expected first dword to be 0, was {dword} instead");
			}

			while ((dword = reader.ReadInt32()) > 0) {
				if (templates.Count == 0) {
					templates.Add(dword, reader.ReadNullTerminatedString());

				} else {
					if (templates.ContainsKey(dword)) {
						nextPath += templates[dword];
					}

					nextPath += reader.ReadNullTerminatedString();
					templates.Add(templates.Keys.Max() + 1, nextPath);
					nextPath = string.Empty;
				}
			}

			return templates;
		}

		private static List<string> BuildFilePaths(ref MemoryReader reader, Dictionary<int, string> templates) {
			List<string> paths = new();
			string nextPath = string.Empty;
			int word;

			while (reader.Position < reader.Length) {
				word = reader.ReadInt32();

				if (templates.ContainsKey(word)) {
					nextPath += templates[word];
				}

				nextPath += reader.ReadNullTerminatedString();
				paths.Add(nextPath);
				nextPath = string.Empty;
			}

			return paths;
		}

		public static ulong CalculateHash(string directoryPath) => FNV.FNV1a_64(directoryPath.ToLower().Trim('/') + "++");
	}
}
