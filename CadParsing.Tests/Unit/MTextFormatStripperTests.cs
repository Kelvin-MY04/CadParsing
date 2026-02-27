using CadParsing.Helpers;
using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class MTextFormatStripperTests
    {
        [Test]
        public void Strip_NullInput_ReturnsEmptyString()
        {
            Assert.That(MTextFormatStripper.Strip(null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Strip_EmptyInput_ReturnsEmptyString()
        {
            Assert.That(MTextFormatStripper.Strip(string.Empty), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Strip_PlainText_ReturnsUnchanged()
        {
            Assert.That(MTextFormatStripper.Strip("LEVEL 1"), Is.EqualTo("LEVEL 1"));
        }

        [Test]
        public void Strip_ParagraphBreakCode_IsRemoved()
        {
            Assert.That(MTextFormatStripper.Strip(@"\PLEVEL 1"), Is.EqualTo("LEVEL 1"));
        }

        [Test]
        public void Strip_HeightCode_IsRemoved()
        {
            Assert.That(MTextFormatStripper.Strip(@"\H300;LEVEL 1"), Is.EqualTo("LEVEL 1"));
        }

        [Test]
        public void Strip_FontCode_IsRemoved()
        {
            Assert.That(
                MTextFormatStripper.Strip(@"\fArial|b0|i0;LEVEL 1"),
                Is.EqualTo("LEVEL 1"));
        }

        [Test]
        public void Strip_GroupBracketsWithFontCode_AreRemoved()
        {
            Assert.That(
                MTextFormatStripper.Strip(@"{\fArial;FLOOR PLAN}"),
                Is.EqualTo("FLOOR PLAN"));
        }

        [Test]
        public void Strip_MultipleMixedCodes_AreAllRemoved()
        {
            Assert.That(
                MTextFormatStripper.Strip(@"\H400;\CFLOOR\PPLAN"),
                Is.EqualTo("FLOOR PLAN"));
        }

        [Test]
        public void Strip_OnlyFormatCodes_ReturnsEmptyString()
        {
            Assert.That(MTextFormatStripper.Strip(@"\H300;"), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Strip_OnlyWhitespaceAfterStripping_ReturnsEmptyString()
        {
            Assert.That(MTextFormatStripper.Strip(@"   \P  "), Is.EqualTo(string.Empty));
        }
    }
}
