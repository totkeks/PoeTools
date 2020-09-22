using PoeTools.Bundles.Lib;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.RegularExpressions;

using Slice = System.Tuple<int, int>;

namespace PoeTools.Bundles.CLI
{
	partial class BundleCommand
	{
		private class ExtractCommand : Command
		{
			public ExtractCommand() : base("extract", "Extract data from the bundle")
			{
				AddArgument(bundleFile);
				AddArgument(new Argument<FileInfo>("outputFile", "Path to the output file"));
				AddOption(new Option<bool>(new[] { "-a", "--all" }, "Extract all data"));
				AddOption(new Option<int>(new[] { "-b", "--block" }, "Extract a specific block (1-indexed)"));
				var sliceOption = new Option<string>(new[] { "-s", "--slice" }, "Extract a slice of data (syntax: 'from,to'; to is exclusive)");
				sliceOption.AddValidator(result => validateSlice(result.GetValueOrDefault<string>()));
				AddOption(sliceOption);
				AddValidator(validateMutuallyExclusiveOptions);

				Handler = CommandHandler.Create<FileInfo, FileInfo, bool, int, string>(executeCommand);
			}

			private static void executeCommand(FileInfo bundleFile, FileInfo outputFile, bool all, int block, string slice)
			{
				var bundle = new Bundle(bundleFile.FullName);
				ReadOnlyMemory<byte> data;
				
				if (all)
				{
					data = bundle.GetContent();
				} else if (block > 0)
				{
					data = bundle.GetContent(block - 1);
				} else
				{
					var parsedSlice = parseSlice(slice);
					data = bundle.GetContent(parsedSlice.Item1, parsedSlice.Item2 - parsedSlice.Item1 + 1);
				}

				File.WriteAllBytes(outputFile.FullName, data.ToArray());

				Console.Write($"Wrote {data.Length} bytes to {outputFile.FullName}");
			}

			private static string validateMutuallyExclusiveOptions(CommandResult commandResult)
			{
				if (commandResult.Children.Contains("all") && commandResult.Children.Contains("block")
						 || commandResult.Children.Contains("all") && commandResult.Children.Contains("slice")
						 || commandResult.Children.Contains("block") && commandResult.Children.Contains("slice"))
				{
					return "Options '--all', '--block' and '--slice' cannot be used together.";
				}

				if (!commandResult.Children.Contains("all") && !commandResult.Children.Contains("block") && !commandResult.Children.Contains("slice"))
				{
					return "One of '--all', '--block' or '--slice' must be set.";
				}

				return null;
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

			private static Slice parseSlice(string slice)
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
}
