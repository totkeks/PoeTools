using System.CommandLine;
using System.IO;

namespace PoeTools.Bundles.CLI
{
	partial class BundleCommand : Command
	{
		public BundleCommand() : base("bundle", "Access bundles.bin files")
		{
			AddAlias("b");

			AddCommand(new InfoCommand());
			AddCommand(new ExtractCommand());
		}

		private static Argument<FileInfo> bundleFile = new Argument<FileInfo>("bundleFile", "Path to the bundle file");
	}
}
