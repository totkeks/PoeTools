﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI {
	partial class IndexCommand {
		private class FilesCommand : Command {
			public FilesCommand() : base("files", "List all files referenced by this index") {
				AddArgument(indexFile);
				Handler = CommandHandler.Create<FileInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo indexFile) {
				var index = new Lib.Index(indexFile.FullName);

				foreach (var path in index.GetAllFilePaths()) {
					Console.WriteLine(path);
				}
			}
		}
	}
}
