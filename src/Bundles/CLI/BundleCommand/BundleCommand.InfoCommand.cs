using PoeTools.Bundles.Lib;

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace PoeTools.Bundles.CLI {
	partial class BundleCommand {
		private class InfoCommand : Command {
			public InfoCommand() : base("info", "Display information about the bundle") {
				AddArgument(bundleFile);
				Handler = CommandHandler.Create<FileInfo>(ExecuteCommand);
			}

			private static void ExecuteCommand(FileInfo bundleFile) {
				var bundle = new Bundle(bundleFile.FullName);

				Console.WriteLine($"Name:              {bundle.Name}");
				Console.WriteLine($"Blocks:            {bundle.BlockCount}");
				Console.WriteLine($"Uncompressed size: {bundle.UncompressedSize} bytes");
			}
		}
	}
}
