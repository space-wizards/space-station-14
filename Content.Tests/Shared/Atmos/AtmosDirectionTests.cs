using Content.Shared.Atmos;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Shared.Atmos;

[TestFixture, TestOf(typeof(AtmosDirection))]
[Parallelizable(ParallelScope.All)]
public sealed class AtmosDirectionTests
{
    [TestCase(5, 5, AtmosDirection.North, 5, 6)]
    [TestCase(5, 5, AtmosDirection.South, 5, 4)]
    [TestCase(5, 5, AtmosDirection.East, 6, 5)]
    [TestCase(5, 5, AtmosDirection.West, 4, 5)]
    [TestCase(5, 5, AtmosDirection.NorthEast, 6, 6)]
    [TestCase(5, 5, AtmosDirection.NorthWest, 4, 6)]
    [TestCase(5, 5, AtmosDirection.SouthEast, 6, 4)]
    [TestCase(5, 5, AtmosDirection.SouthWest, 4, 4)]
    [TestCase(0, 0, AtmosDirection.North, 0, 1)]
    [TestCase(-5, -5, AtmosDirection.South, -5, -6)]
    [TestCase(5, 5, AtmosDirection.Invalid, 5, 5)]
    public void Offset_OffsetsCorrectly(int startX, int startY, AtmosDirection direction, int expectedX, int expectedY)
    {
        var pos = new Vector2i(startX, startY);
        var result = pos.Offset(direction);
        Assert.That(result, Is.EqualTo(new Vector2i(expectedX, expectedY)));
    }

    [Test]
    public void Offset_Multiple_DirectionsApplied_CorrectlyChains()
    {
        var pos = new Vector2i(0, 0);
        var result = pos.Offset(AtmosDirection.North).Offset(AtmosDirection.East).Offset(AtmosDirection.East);
        Assert.That(result, Is.EqualTo(new Vector2i(2, 1)));
    }

    [Test]
    public void Offset_AndOppositeDirection_ReturnsOriginal()
    {
        var pos = new Vector2i(10, 10);
        var result = pos.Offset(AtmosDirection.North).Offset(AtmosDirection.South);
        Assert.That(result, Is.EqualTo(pos));
    }

    [TestCase(AtmosDirection.North, 0, 1)]
    [TestCase(AtmosDirection.South, 0, -1)]
    [TestCase(AtmosDirection.East, 1, 0)]
    [TestCase(AtmosDirection.West, -1, 0)]
    [TestCase(AtmosDirection.NorthEast, 1, 1)]
    [TestCase(AtmosDirection.NorthWest, -1, 1)]
    [TestCase(AtmosDirection.SouthEast, 1, -1)]
    [TestCase(AtmosDirection.SouthWest, -1, -1)]
    [TestCase(AtmosDirection.Invalid, 0, 0)]
    [TestCase(AtmosDirection.All, 0, 0)]
    public void CardinalToIntVec_ReturnsCorrectOffset(AtmosDirection direction, int expectedX, int expectedY)
    {
        var result = direction.CardinalToIntVec();
        Assert.That(result, Is.EqualTo(new Vector2i(expectedX, expectedY)));
    }
}
