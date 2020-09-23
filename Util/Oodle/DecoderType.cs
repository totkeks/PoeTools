namespace PoeTools.Util.Oodle
{
	internal enum DecoderType
	{
		LZNA = 5,
		Kraken = 6,
		Mermaid = 10,
		BitKnit = 11,
		Leviathan = 12,
	}

	internal static class DecoderTypeExtensions
	{
		internal static bool IsKrakenDecoder(this DecoderType type)
		{
			return type == DecoderType.Kraken
			 || type == DecoderType.Mermaid
			 || type == DecoderType.Leviathan;
		}
	}
}
