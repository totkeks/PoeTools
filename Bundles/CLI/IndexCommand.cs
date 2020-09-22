using System.CommandLine;
using System.IO;

namespace PoeTools.Bundles.CLI
{
	partial class IndexCommand : Command
	{
		public IndexCommand() : base("index", "Access index.bin files")
		{
			AddAlias("i");

			AddCommand(new InfoCommand());
			AddCommand(new BundlesCommand());
			AddCommand(new FilesCommand());
			AddCommand(new ExtractCommand());
		}

		private static Argument<FileInfo> indexFile = new Argument<FileInfo>("indexFile", "Path to the index file");
	}
}
