using System;
using PoeTools.Util.ExtensionMethods;

namespace PoeTools.Util.Oodle
{
	internal class LZNA
	{
		public static int ParseWholeMatchInfo(ReadOnlyMemory<byte> source, out int wholeMatchDistance)
		{
			var relevantBytes = source[0..2].Span;
			ushort headerWord = Word.FromBytes(relevantBytes[0], relevantBytes[1]);

			if (headerWord < 0x8000)
			{ // smaller than 32k / signed value??
				int result = 0, b, pos = 0;
				int offset = 2;

				// TODO: Figure out what this is doing
				// Seems like it builds an output value from lower to higher bytes with 7 relevant bits each
				for (; ; )
				{
					b = source[offset..1].Span[0];
					offset += 1;
					if (b.GetBit(8))
					{
						break;
					}
					result += (b.SetBit(8)) << pos;
					pos += 7;
				}

				result += (b.UnsetBit(8)) << pos;
				wholeMatchDistance = headerWord.SetBit(16).SetBit(1) + (result << 15);
				return offset;
			}
			else
			{
				wholeMatchDistance = headerWord.UnsetBit(16).SetBit(1);
				return 2;
			}
		}
	}
}
