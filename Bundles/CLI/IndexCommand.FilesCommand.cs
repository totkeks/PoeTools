using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI
{
	partial class IndexCommand
	{
		private class FilesCommand : Command
		{
			public FilesCommand() : base("files", "List all files referenced by this index")
			{
				AddArgument(indexFile);
				Handler = CommandHandler.Create<FileInfo>(executeCommand);
			}

			private static void executeCommand(FileInfo indexFile)
			{
				//var index = new Index(indexFile.FullName);

				Console.WriteLine("Executing index.files command");
			}
		}
	}
}
