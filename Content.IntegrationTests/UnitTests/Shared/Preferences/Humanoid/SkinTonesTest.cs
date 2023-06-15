using Content.Shared.Humanoid;
using NUnit.Framework;

namespace Content.Tests.Shared.Preferences.Humanoid;

[TestFixture]
public sealed class SkinTonesTest
{
    [Test]
    public void TestHumanSkinToneValidity()
    {
        for (var i = 0; i <= 100; i++)
        {
            var color = SkinColor.HumanSkinTone(i);
            Assert.That(SkinColor.VerifyHumanSkinTone(color));
        }
    }

    [Test]
    public void TestDefaultSkinToneValid()
    {
        Assert.That(SkinColor.VerifyHumanSkinTone(SkinColor.ValidHumanSkinTone));
    }
}
