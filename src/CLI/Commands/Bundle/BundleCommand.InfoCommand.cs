using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

using PoETool.FileTypes.Bundle;

namespace PoETool.CLI.Commands {
	partial class BundleCommand {
		private class InfoCommand : Command {
			public InfoCommand() : base("info", "Display information about the bundle") {
				AddArgument(bundleFile);
				Handler = CommandHandler.Create<FileInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo bundleFile) {
				var bundle = new BundleFile(bundleFile.FullName);

				Console.WriteLine($"Name:              {bundle.Name}");
				Console.WriteLine($"Blocks:            {bundle.BlockCount}");
				Console.WriteLine($"Compressed size:   {bundle.CompressedSize} bytes");
				Console.WriteLine($"Uncompressed size: {bundle.UncompressedSize} bytes");
			}
		}
	}
}
