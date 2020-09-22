using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.RegularExpressions;

namespace PoeTools.Bundles.CLI
{
	class Program
	{
		static int Main(string[] args)
		{
			var rootCommand = new RootCommand("CLI for Path of Exile binary bundles and indices.");

			rootCommand.AddCommand(new BundleCommand());
			setupIndexCommand(rootCommand);

			return rootCommand.Invoke(args);
		}

		static void setupIndexCommand(RootCommand rootCommand)
		{
			var indexCommand = new Command("index", "Access index.bin files");
			indexCommand.AddAlias("i");

			var indexFile = new Argument<FileInfo>("indexFile", "Path to the index file");

			var infoCommand = new Command("info", "Display information about the index")
			{
				indexFile
			};
			indexCommand.Add(infoCommand);

			var bundlesCommand = new Command("bundles", "List all bundles referenced by this index")
			{
				indexFile
			};
			indexCommand.Add(bundlesCommand);

			var filesCommand = new Command("files", "List all files referenced by this index")
			{
				indexFile
			};
			indexCommand.Add(filesCommand);

			var extractCommand = new Command("extract", "Extract one or multiple files from referenced bundles to the current directory")
			{
				indexFile,
				new Argument<string>("pathSpec", "Glob pattern for files to extract"),
				new Option<bool>(new[] {"-f", "--flat"}, "Flatten the file hierarchy"),
				new Option<DirectoryInfo>(new[] {"-o", "--output"}, "Location to place the extracted files")
			};
			indexCommand.Add(extractCommand);

			rootCommand.Add(indexCommand);
		}
	}
}
