using System.CommandLine;
using System.CommandLine.Parsing;

using PoETool.CLI.Commands;

namespace PoETool.CLI {
	internal class Program {
		private static int Main(string[] args) {
			RootCommand rootCommand = new("CLI for Path of Exile binary bundles and indices.");

			rootCommand.AddCommand(new BundleCommand());
			rootCommand.AddCommand(new IndexCommand());

			return rootCommand.Invoke(args);
		}
	}
}
