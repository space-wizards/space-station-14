using Content.Shared.Humanoid;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Content.Tests.Shared.Preferences.Humanoid;

[TestFixture, TestOf(typeof(SkinColor)), Parallelizable(ParallelScope.All)]
public sealed class SkinTonesTest
{
    public static IEnumerable<int> TestData => Enumerable.Range(0, 101);

    [Test]
    public void TestHumanSkinToneValidity([ValueSource(nameof(TestData))] int tone)
    {
        Assert.That(SkinColor.VerifyHumanSkinTone(SkinColor.HumanSkinTone(tone)));
    }

    [Test]
    public void TestDefaultSkinToneValid()
    {
        Assert.That(SkinColor.VerifyHumanSkinTone(SkinColor.ValidHumanSkinTone));
    }
}
