using System.CommandLine;
using System.IO;

namespace PoETool.CLI.Commands {
	partial class IndexCommand : Command {
		public IndexCommand() : base("index", "Access index.bin files") {
			AddAlias("i");

			AddCommand(new InfoCommand());
			AddCommand(new BundlesCommand());
			AddCommand(new FilesCommand());
			AddCommand(new ExtractCommand());
		}

		private static readonly Argument<FileInfo> indexFile = new("indexFile", "Path to the index file");
	}
}
