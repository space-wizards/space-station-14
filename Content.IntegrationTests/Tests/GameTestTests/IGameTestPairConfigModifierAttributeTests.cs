using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(IGameTestPairConfigModifier))]
public sealed class IGameTestPairConfigModifierAttributeTests : GameTest
{
    [Test]
    public void Control()
    {
        Assert.That(Pair.Settings.Connected, Is.True);
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    [Description("Ensures pair settings apply.")]
    public void PairConfigWorks()
    {
        Assert.That(Pair.Settings.Connected, Is.False);
    }
}
