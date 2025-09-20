using Content.Shared.Humanoid;
using NUnit.Framework;

namespace Content.Tests.Shared.Preferences.Humanoid;

[TestFixture]
public sealed class SkinTonesTest
{
    [Test]
    public void TestHumanSkinToneValidity()
    {
        var strategy = new HumanTonedSkinColoration();

        for (var i = 0; i <= 100; i++)
        {
            var color = strategy.FromUnary(i);
            Assert.That(strategy.VerifySkinColor(color));
        }
    }

    [Test]
    public void TestDefaultSkinToneValid()
    {
        var strategy = new HumanTonedSkinColoration();

        Assert.That(strategy.VerifySkinColor(strategy.ValidHumanSkinTone));
    }
}
