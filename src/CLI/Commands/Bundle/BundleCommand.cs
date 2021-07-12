using System.CommandLine;
using System.IO;

namespace PoETool.CLI.Commands {
	partial class BundleCommand : Command {
		public BundleCommand() : base("bundle", "Access bundles.bin files") {
			AddAlias("b");

			AddCommand(new InfoCommand());
			AddCommand(new ExtractCommand());
		}

		private static readonly Argument<FileInfo> bundleFile = new("bundleFile", "Path to the bundle file");
	}
}
