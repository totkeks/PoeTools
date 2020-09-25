using System.CommandLine;
using System.CommandLine.Parsing;

namespace PoeTools.Bundles.CLI {
	class Program {
		static int Main(string[] args) {
			var rootCommand = new RootCommand("CLI for Path of Exile binary bundles and indices.");

			rootCommand.AddCommand(new BundleCommand());
			rootCommand.AddCommand(new IndexCommand());

			return rootCommand.Invoke(args);
		}
	}
}
