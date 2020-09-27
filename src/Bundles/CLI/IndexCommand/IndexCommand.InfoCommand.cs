using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI {
	partial class IndexCommand {
		private class InfoCommand : Command {
			public InfoCommand() : base("info", "Display information about the index") {
				AddArgument(indexFile);
				Handler = CommandHandler.Create<FileInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo indexFile) {
				var index = new Lib.Index(indexFile.FullName);

				Console.WriteLine($"Name:        {indexFile.Name}");
				Console.WriteLine($"Bundles:     {index.BundleCount}");
				Console.WriteLine($"Directories: {index.DirectoryCount}");
				Console.WriteLine($"Files:       {index.FileCount}");
			}
		}
	}
}
