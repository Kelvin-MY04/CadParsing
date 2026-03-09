using CadParsing.Helpers;
using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class LayerNameMatcherTests
    {
        [TestCase("A-PAPER-EX", "PAPER-EX", true,
            TestName = "ExactSuffixMatch_ReturnsTrue")]
        [TestCase("A-PAPER-ex", "PAPER-EX", true,
            TestName = "LayerNameLowercase_CaseInsensitive_ReturnsTrue")]
        [TestCase("A-PAPER-EX", "paper-ex", true,
            TestName = "SuffixLowercase_CaseInsensitive_ReturnsTrue")]
        [TestCase("XREF|FLOOR-PAPER-EX", "PAPER-EX", true,
            TestName = "XrefPrefixedLayer_MatchesSuffix_ReturnsTrue")]
        [TestCase("BOUND$0$FLOOR-PAPER-EX", "PAPER-EX", true,
            TestName = "BoundXrefLayer_MatchesSuffix_ReturnsTrue")]
        [TestCase("A-PAPER_EX", "PAPER-EX", false,
            TestName = "WrongSeparator_ReturnsFalse")]
        [TestCase("PAPER-EX-EXTRA", "PAPER-EX", false,
            TestName = "SuffixInMiddle_ReturnsFalse")]
        [TestCase("", "PAPER-EX", false,
            TestName = "EmptyLayerName_ReturnsFalse")]
        [TestCase(null, "PAPER-EX", false,
            TestName = "NullLayerName_ReturnsFalse")]
        [TestCase("A-PAPER-EX", "", false,
            TestName = "EmptySuffix_ReturnsFalse")]
        [TestCase("A-PAPER-EX", null, false,
            TestName = "NullSuffix_ReturnsFalse")]
        public void MatchesLayerSuffix_VariousCases_ReturnsExpected(
            string layerName, string suffix, bool expected)
        {
            bool result = LayerNameMatcher.MatchesLayerSuffix(layerName, suffix);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("A-TEXT", new[] { "TEXT", "TEX" }, true,
            TestName = "MatchesFirstSuffix_ReturnsTrue")]
        [TestCase("A-TEX", new[] { "TEXT", "TEX" }, true,
            TestName = "MatchesSecondSuffix_ReturnsTrue")]
        [TestCase("A-tex", new[] { "TEXT", "TEX" }, true,
            TestName = "CaseInsensitiveMatch_ReturnsTrue")]
        [TestCase("A-ANNO", new[] { "TEXT", "TEX" }, false,
            TestName = "MatchesNoSuffix_ReturnsFalse")]
        [TestCase(null, new[] { "TEXT", "TEX" }, false,
            TestName = "NullLayerName_AnyLayerSuffix_ReturnsFalse")]
        [TestCase("A-TEXT", new string[0], false,
            TestName = "EmptySuffixArray_ReturnsFalse")]
        [TestCase("A-TEXT", null, false,
            TestName = "NullSuffixArray_ReturnsFalse")]
        public void MatchesAnyLayerSuffix_VariousCases_ReturnsExpected(
            string layerName, string[] suffixes, bool expected)
        {
            bool result = LayerNameMatcher.MatchesAnyLayerSuffix(layerName, suffixes);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
