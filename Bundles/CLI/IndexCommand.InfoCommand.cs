using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI
{
	partial class IndexCommand
	{
		private class InfoCommand : Command
		{
			public InfoCommand() : base("info", "Display information about the index")
			{
				AddArgument(indexFile);
				Handler = CommandHandler.Create<FileInfo>(executeCommand);
			}

			private static void executeCommand(FileInfo indexFile)
			{
				//var index = new Index(indexFile.FullName);

				//Console.WriteLine($"Name:              {bundle.Name}");
				//Console.WriteLine($"Blocks:            {bundle.BlockCount}");
				//Console.WriteLine($"Uncompressed size: {bundle.UncompressedSize} bytes");
				Console.WriteLine("Executing index.info command");
			}
		}
	}
}
