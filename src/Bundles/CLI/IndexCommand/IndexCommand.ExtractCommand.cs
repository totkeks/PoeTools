using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI {
	partial class IndexCommand {
		private class ExtractCommand : Command {
			public ExtractCommand() : base("extract", "Extract one or multiple files from referenced bundles to the current directory") {
				AddArgument(indexFile);
				AddArgument(new Argument<string>("pathSpec", "Glob pattern for files to extract"));
				AddOption(new Option<bool>(new[] { "-f", "--flat" }, "Flatten the file hierarchy"));
				AddOption(new Option<DirectoryInfo>(new[] { "-o", "--output" }, "Location to place the extracted files"));

				Handler = CommandHandler.Create<FileInfo, string, bool, DirectoryInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo indexFile, string pathSpec, bool flat, DirectoryInfo output) {
				//var index = new Index(indexFile.FullName);

				Console.WriteLine("Executing index.extract command");
			}
		}
	}
}
