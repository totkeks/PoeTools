using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.IO;

namespace PoeTools.Bundles.CLI
{
	class Program
	{
		static int Main(string[] args)
		{
			var rootCommand = new RootCommand("CLI for Path of Exile binary bundles and indices.");

			setupIndexCommand(rootCommand);
			setupBundleCommand(rootCommand);

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

		static void setupBundleCommand(RootCommand rootCommand)
		{
			var bundleCommand = new Command("bundle", "Access bundles.bin files");
			bundleCommand.AddAlias("b");

			var bundleFile = new Argument<FileInfo>("bundleFile", "Path to the bundle file");

			var infoCommand = new Command("info", "Display information about the bundle")
			{
				bundleFile
			};
			bundleCommand.Add(infoCommand);

			var extractCommand = new Command("extract", "Extract data from the bundle")
			{
				bundleFile,
				new Argument<FileInfo>("outputFile", "Path to the output file"),
				new Option<bool>(new[] {"-a", "--all" }, "Extract all data"),
				new Option<int>(new[] {"-b", "--block"}, "Extract a specific block (0-indexed)"),
				new Option<string>(new[] {"-s", "--slice"}, "Extract a slice of data (syntax: 'from,to'; to is exclusive)"),
			};

			extractCommand.AddValidator(commandResult =>
			{
				if (commandResult.Children.Contains("all") && commandResult.Children.Contains("block")
					|| commandResult.Children.Contains("all") && commandResult.Children.Contains("slice")
					|| commandResult.Children.Contains("block") && commandResult.Children.Contains("slice"))
				{
					return "Options '--all', '--block' and '--slice' cannot be used together.";
				}

				return null;
			});
			bundleCommand.Add(extractCommand);

			rootCommand.Add(bundleCommand);
		}
	}
}
