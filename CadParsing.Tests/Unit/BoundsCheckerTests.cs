using CadParsing.Helpers;
using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class BoundsCheckerTests
    {
        // Bounds used in most tests: minX=0, minY=0, maxX=100, maxY=100

        [Test]
        public void IsInsideBounds_CenterPoint_ReturnsTrue()
        {
            Assert.That(BoundsChecker.IsInsideBounds(50, 50, 0, 0, 100, 100), Is.True);
        }

        [Test]
        public void IsInsideBounds_AllCorners_ReturnTrue()
        {
            Assert.That(BoundsChecker.IsInsideBounds(0, 0, 0, 0, 100, 100), Is.True,
                "Bottom-left corner");
            Assert.That(BoundsChecker.IsInsideBounds(100, 0, 0, 0, 100, 100), Is.True,
                "Bottom-right corner");
            Assert.That(BoundsChecker.IsInsideBounds(0, 100, 0, 0, 100, 100), Is.True,
                "Top-left corner");
            Assert.That(BoundsChecker.IsInsideBounds(100, 100, 0, 0, 100, 100), Is.True,
                "Top-right corner");
        }

        [Test]
        public void IsInsideBounds_EdgePoints_ReturnTrue()
        {
            Assert.That(BoundsChecker.IsInsideBounds(50, 0, 0, 0, 100, 100), Is.True,
                "Bottom edge midpoint");
            Assert.That(BoundsChecker.IsInsideBounds(50, 100, 0, 0, 100, 100), Is.True,
                "Top edge midpoint");
            Assert.That(BoundsChecker.IsInsideBounds(0, 50, 0, 0, 100, 100), Is.True,
                "Left edge midpoint");
            Assert.That(BoundsChecker.IsInsideBounds(100, 50, 0, 0, 100, 100), Is.True,
                "Right edge midpoint");
        }

        [Test]
        public void IsInsideBounds_PointJustOutsideEachEdge_ReturnFalse()
        {
            Assert.That(BoundsChecker.IsInsideBounds(50, -0.001, 0, 0, 100, 100), Is.False,
                "Just below bottom edge");
            Assert.That(BoundsChecker.IsInsideBounds(50, 100.001, 0, 0, 100, 100), Is.False,
                "Just above top edge");
            Assert.That(BoundsChecker.IsInsideBounds(-0.001, 50, 0, 0, 100, 100), Is.False,
                "Just left of left edge");
            Assert.That(BoundsChecker.IsInsideBounds(100.001, 50, 0, 0, 100, 100), Is.False,
                "Just right of right edge");
        }

        [Test]
        public void IsInsideBounds_AllZeroExtents_ReturnsTrueForOrigin()
        {
            Assert.That(BoundsChecker.IsInsideBounds(0, 0, 0, 0, 0, 0), Is.True);
        }

        [Test]
        public void IsInsideBounds_AllZeroExtents_ReturnsFalseForNonOrigin()
        {
            Assert.That(BoundsChecker.IsInsideBounds(1, 0, 0, 0, 0, 0), Is.False);
            Assert.That(BoundsChecker.IsInsideBounds(0, 1, 0, 0, 0, 0), Is.False);
        }
    }
}
