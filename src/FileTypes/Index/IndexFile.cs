using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

using PoETool.FileTypes.Bundle;
using PoETool.FileTypes.Utils;

namespace PoETool.FileTypes.Index {
	/// <summary>
	/// A binary index that references slices inside bundles and maps them to file names.<br/>
	/// Typically contained inside a <c>.index.bin</c> file.
	/// </summary>
	public class IndexFile {
		private readonly string baseDirectory;
		private readonly string name;

		public int BundleCount { get => bundleRecords.Count; }
		private List<BundleRecord> bundleRecords;

		public int FileCount { get => fileRecords.Count; }
		private Dictionary<ulong, FileRecord> fileRecords;

		public int DirectoryCount { get => directoryRecords.Count; }
		private List<DirectoryRecord> directoryRecords;

		private ImmutableList<Lazy<BundleFile>> bundles;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexFile"/> class with data from a specified file.
		/// </summary>
		/// <param name="filePath">The path to the index file.</param>
		public IndexFile(string filePath, bool init = true) {
			baseDirectory = Path.GetDirectoryName(filePath);
			name = Path.GetFileName(filePath);
			BundleFile bundle = new(filePath);

			MemoryReader reader = new(bundle.GetContent().Span);
			ReadBundleRecords(ref reader);
			ReadFileRecords(ref reader);
			ReadDirectoryRecords(ref reader);

			if (init) {
				MapFilePathsToRecords(ref reader);
				InitializeBundles();
			}
		}

		/// <summary>
		/// Get the paths of all files referenced by this index.
		/// </summary>
		/// <returns>A list containing all file paths as strings.</returns>
		public IEnumerable<string> GetAllFilePaths() {
			return fileRecords.Values.Select(fileRecord => fileRecord.Path);
		}

		/// <summary>
		/// Get the paths of all bundles referenced by this index.
		/// </summary>
		/// <returns>A list containing all bundle paths as strings.</returns>
		public IEnumerable<string> GetAllBundlePaths() {
			return bundleRecords.Select(bundleRecord => bundleRecord.Name);
		}

		/// <summary>
		/// Get the contents of a file referenced by this index.
		/// </summary>
		/// <param name="filePath">Path to the file inside the index.</param>
		/// <returns>A memory segment containing the file contents.</returns>
		public ReadOnlyMemory<byte> GetFile(string filePath) {
			var hash = FileRecord.CalculateHash(filePath);
			var fileRecord = fileRecords[hash];

			return ExtractFileFromBundle(fileRecord);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return $"Index '{name}': {BundleCount} declared bundles, {FileCount} declared files and {DirectoryCount} declared directories";
		}

		private void ReadBundleRecords(ref MemoryReader reader) {
			var bundleCount = reader.ReadInt32();
			bundleRecords = new List<BundleRecord>(bundleCount);

			for (int ii = 0; ii < bundleCount; ii++) {
				bundleRecords.Add(new BundleRecord(ref reader));
			}
		}

		private void ReadFileRecords(ref MemoryReader reader) {
			var fileCount = reader.ReadInt32();
			fileRecords = new Dictionary<ulong, FileRecord>(fileCount);

			for (int ii = 0; ii < fileCount; ii++) {
				FileRecord record = new(ref reader);
				fileRecords.Add(record.Hash, record);
			}
		}

		private void ReadDirectoryRecords(ref MemoryReader reader) {
			var directoryCount = reader.ReadInt32();
			directoryRecords = new List<DirectoryRecord>(directoryCount);

			for (int ii = 0; ii < directoryCount; ii++) {
				directoryRecords.Add(new DirectoryRecord(ref reader));
			}
		}

		private void MapFilePathsToRecords(ref MemoryReader reader) {
			var bundle = new BundleFile(reader, "Directories");

			foreach (var directoryRecord in directoryRecords) {
				foreach (var filePath in directoryRecord.GetFilePaths(bundle)) {
					ulong hash = FileRecord.CalculateHash(filePath);
					fileRecords[hash].Path = filePath;
				}
			}
		}

		private void InitializeBundles() {
			var temp = new List<Lazy<BundleFile>>(BundleCount);

			for (int ii = 0; ii < BundleCount; ii++) {
				int index = ii;
				temp.Add(new Lazy<BundleFile>(
					() => new BundleFile(Path.Combine(baseDirectory, bundleRecords[index].Name)), LazyThreadSafetyMode.ExecutionAndPublication));
			}

			bundles = temp.ToImmutableList();
		}

		private ReadOnlyMemory<byte> ExtractFileFromBundle(FileRecord fileRecord) {
			var bundle = bundles[fileRecord.BundleIndex].Value;

			return bundle.GetContent(fileRecord.Offset, fileRecord.Size);
		}

	}
}
