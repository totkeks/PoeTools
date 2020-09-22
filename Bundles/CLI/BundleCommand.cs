using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.RegularExpressions;

namespace PoeTools.Bundles.CLI
{
	class BundleCommand : Command
	{
		public BundleCommand() : base("bundle", "Access bundles.bin files")
		{
			AddAlias("b");

			AddCommand(new InfoCommand());
			AddCommand(new ExtractCommand());
		}
		private class InfoCommand : Command
		{
			public InfoCommand() : base("info", "Display information about the bundle")
			{
				AddArgument(bundleFile);
				Handler = CommandHandler.Create<FileInfo>(executeCommand);
			}

			private static void executeCommand(FileInfo bundleFile)
			{
				var bundle = new Bundle(bundleFile.FullName);

				Console.WriteLine($"Name:              {bundle.Name}");
				Console.WriteLine($"Blocks:            {bundle.BlockCount}");
				Console.WriteLine($"Uncompressed size: {bundle.UncompressedSize} bytes");
			}
		}

		private static Argument<FileInfo> bundleFile = new Argument<FileInfo>("bundleFile", "Path to the bundle file");

		private class ExtractCommand : Command
		{
			public ExtractCommand() : base("extract", "Extract data from the bundle")
			{
				AddArgument(bundleFile);
				AddArgument(new Argument<FileInfo>("outputFile", "Path to the output file"));
				AddOption(new Option<bool>(new[] { "-a", "--all" }, "Extract all data"));
				AddOption(new Option<int>(new[] { "-b", "--block" }, "Extract a specific block (0-indexed)"));
				var sliceOption = new Option<string>(new[] { "-s", "--slice" }, "Extract a slice of data (syntax: 'from,to'; to is exclusive)");
				sliceOption.AddValidator(result => validateSlice(result.GetValueOrDefault<string>()));
				AddOption(sliceOption);
				AddValidator(validateMutuallyExclusiveOptions);

				Handler = CommandHandler.Create<FileInfo>(executeCommand);
			}

			private static string validateMutuallyExclusiveOptions(CommandResult commandResult)
			{
				if (commandResult.Children.Contains("all") && commandResult.Children.Contains("block")
						 || commandResult.Children.Contains("all") && commandResult.Children.Contains("slice")
						 || commandResult.Children.Contains("block") && commandResult.Children.Contains("slice"))
				{
					return "Options '--all', '--block' and '--slice' cannot be used together.";
				}

				return null;
			}

			private static void executeCommand(FileInfo bundleFile)
			{
				Console.WriteLine("Executing command bundle.extract");
				Console.WriteLine($"Bundlefile: {bundleFile}");
			}
		}

		private static string validateSlice(string slice)
		{
			var parsed = parseSlice(slice);

			if (parsed == null)
			{
				return "Slice must be in the format 'from,to'.";
			}

			if (parsed.Item1 > parsed.Item2)
			{
				return "Invalid slice, 'to' must be smaller than 'from'.";
			}

			return null;
		}

		private static Tuple<int, int> parseSlice(string slice)
		{
			var match = Regex.Match(slice, "^(0|[1-9][0-9]*),([1-9][0-9]*)$");

			if (!match.Success)
			{
				return null;
			}

			var from = int.Parse(match.Groups[1].Value);
			var to = int.Parse(match.Groups[2].Value);

			return Tuple.Create(from, to);
		}
	}
}
