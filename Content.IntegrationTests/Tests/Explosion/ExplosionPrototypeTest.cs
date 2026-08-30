using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Explosion;

namespace Content.IntegrationTests.Tests.Explosion;

public sealed class ExplosionPrototypeTest : GameTest
{
    private static string[] _explosionKinds = GameDataScrounger.PrototypesOfKind<ExplosionPrototype>();

    [Test]
    [TestOf(typeof(ExplosionPrototype))]
    [TestCaseSource(nameof(_explosionKinds))]
    [Description("Ensures various properties of ExplosionPrototype are correctly configured.")]
    public async Task Validate(string protoKey)
    {
        var pair = Pair;
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        var proto = protoMan.Index<ExplosionPrototype>(protoKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(proto._tileBreakChance, Is.Not.Empty, $"Tile break chances cannot be empty.");
            // this diagnostic is broken.
#pragma warning disable NUnit2041
            Assert.That(proto._tileBreakChance,
                Has.All.GreaterThanOrEqualTo(0.0f).And.LessThanOrEqualTo(1.0f),
                "Tile break chances are probabilities and must be in the range [0, 1]");
#pragma warning restore NUnit2041
            Assert.That(proto._tileBreakChance,
                Has.Length.EqualTo(proto._tileBreakIntensity.Length),
                $"Tile break chances don't match the tile break intensities.");

            Assert.That(proto.FireStacks, Is.Null.Or.Positive);
            Assert.That(proto.Temperature, Is.Null.Or.Positive);
            Assert.That(proto.TileBreakRerollReduction, Is.Positive.Or.Zero);
            Assert.That(proto.SmallSoundIterationThreshold, Is.Positive.Or.Zero);
            Assert.That(proto.MaxCombineDistance, Is.Positive.Or.Zero);
            Assert.That(proto.IntensityPerState, Is.Positive);
            Assert.That(proto.FireStates, Is.Positive);
        }
    }
}
