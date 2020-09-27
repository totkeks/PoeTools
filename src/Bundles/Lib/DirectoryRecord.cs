using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PoeTools.Bundles.Lib {
	public class DirectoryRecord {
		public ulong Hash { get; }
		public int Offset { get; }
		public int Size { get; }

		public DirectoryRecord(BinaryReader reader) {
			Hash = reader.ReadUInt64();
			Offset = reader.ReadInt32();
			Size = reader.ReadInt32();
			reader.ReadUInt32(); // Unknown
		}

		public List<string> GetFilePaths(Bundle bundle) {
			var reader = new BinaryReader(new MemoryStream(bundle.GetContent(Offset, Size).ToArray()));
			Debug.Assert(reader.ReadUInt32() == 0, "Path templates not starting with 0-word");

			var templates = BuildPathTemplates(reader);
			return BuildFilePaths(reader, templates);
		}

		private static Dictionary<int, string> BuildPathTemplates(BinaryReader reader) {
			var templates = new Dictionary<int, string>();
			string nextPath = string.Empty;
			int word;

			while ((word = reader.ReadInt32()) > 0) {
				if (templates.Count == 0) {
					templates.Add(word, ReadNullTerminatedString(reader));

				} else {
					if (templates.ContainsKey(word)) {
						nextPath += templates[word];
					}

					nextPath += ReadNullTerminatedString(reader);
					templates.Add(templates.Keys.Max() + 1, nextPath);
					nextPath = string.Empty;
				}
			}

			return templates;
		}

		private static List<string> BuildFilePaths(BinaryReader reader, Dictionary<int, string> templates) {
			List<string> paths = new List<string>();
			string nextPath = string.Empty;
			int word;

			while (reader.BaseStream.Position < reader.BaseStream.Length) {
				word = reader.ReadInt32();

				if (templates.ContainsKey(word)) {
					nextPath += templates[word];
				}

				nextPath += ReadNullTerminatedString(reader);
				paths.Add(nextPath);
				nextPath = string.Empty;
			}

			return paths;
		}

		private static string ReadNullTerminatedString(BinaryReader reader) {
			var builder = new StringBuilder(256);
			byte b;

			while ((b = reader.ReadByte()) != '\0') {
				builder.Append((char)b);
			}

			return builder.ToString();
		}
	}
}
