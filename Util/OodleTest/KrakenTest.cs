using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace PoeTools.Util.OodleTest
{
	[TestClass]
	public class KrakenTest
	{
		[TestMethod]
		public void DecompressDickens()
		{
			var expectedResult = File.ReadAllBytes("testdata/dickens");

			var decoder = new Oodle.Decoder();
			var actualResult = decoder.DecompressFile(new FileInfo("testdata/dickens.kraken"));

			CollectionAssert.AreEqual(expectedResult, actualResult.ToArray(), "Mismatch in decompressed data.");
		}
	}
}
