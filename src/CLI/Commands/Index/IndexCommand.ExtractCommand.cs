using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DotNet.Globbing;

using PoETool.FileTypes.Index;

namespace PoETool.CLI.Commands {
	partial class IndexCommand {
		private class ExtractCommand : Command {
			public ExtractCommand() : base("extract", "Extract one or multiple files from referenced bundles to the current directory") {
				AddArgument(indexFile);
				AddArgument(new Argument<string>("pathSpec", "Glob pattern for files to extract"));
				AddOption(new Option<bool>(new[] { "-p", "--parallel" }, "Extract files in parallel"));
				AddOption(new Option<bool>(new[] { "-f", "--flat" }, "Flatten the file hierarchy"));
				AddOption(new Option<DirectoryInfo>(new[] { "-o", "--output" }, "Location to place the extracted files"));

				Handler = CommandHandler.Create<FileInfo, string, bool, bool, DirectoryInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo indexFile, string pathSpec, bool parallel, bool flat, DirectoryInfo output) {
				var globOptions = new GlobOptions();
				globOptions.Evaluation.CaseInsensitive = true;
				var glob = Glob.Parse(pathSpec, globOptions);

				string outputDirectory = output?.FullName ?? Directory.GetCurrentDirectory();
				var index = new IndexFile(indexFile.FullName);

				var files = index.GetAllFilePaths().Where(path => glob.IsMatch(path));
				Console.WriteLine($"Extracting {files.Count()} matching files");

				if (parallel) {
					Parallel.ForEach(files, (file) => ExtractFile(index, file, outputDirectory, flat));

				} else {
					foreach (var file in files) {
						ExtractFile(index, file, outputDirectory, flat);
					}
				}
			}

			private static void ExtractFile(IndexFile index, string file, string outputDirectory, bool flat) {
				var content = index.GetFile(file);
				var outputFilePath = Path.Combine(outputDirectory, flat ? Path.GetFileName(file) : file);

				Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
				File.WriteAllBytes(outputFilePath, content.ToArray());
			}
		}
	}
}
