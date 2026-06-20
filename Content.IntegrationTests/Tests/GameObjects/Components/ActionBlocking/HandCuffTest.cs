#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Robust.Server.Console;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameObjects.Components.ActionBlocking;

[TestOf(typeof(CuffableComponent))]
[TestOf(typeof(HandcuffComponent))]
public sealed class HandCuffTest : GameTest
{
    private const string HumanHandcuffDummy = "HumanHandcuffDummy";
    private const string HandcuffsDummy = "HandcuffsDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {HumanHandcuffDummy}
  id: {HumanHandcuffDummy}
  components:
  - type: Cuffable
  - type: Hands
    hands:
      hand_right:
        location: Right
      hand_left:
        location: Left
    sortedHands:
    - hand_right
    - hand_left
  - type: ComplexInteraction

- type: entity
  name: {HandcuffsDummy}
  id: {HandcuffsDummy}
  components:
  - type: Handcuff
";

    [SidedDependency(Side.Server)] private IServerConsoleHost _sConsoleHost = default!;
    [SidedDependency(Side.Server)] private CuffableSystem _sCuffableSystem = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task Test()
    {
        // Spawn the entities
        var human = SSpawn(HumanHandcuffDummy);
        var otherHuman = SSpawn(HumanHandcuffDummy);
        var cuffs = SSpawn(HandcuffsDummy);
        var secondCuffs = SSpawn(HandcuffsDummy);

        // Test for components existing
        Assert.That(STryComp<CuffableComponent>(human, out var cuffed), $"Human has no {nameof(CuffableComponent)}");
        Assert.That(STryComp<HandsComponent>(human, out var hands), $"Human has no {nameof(HandsComponent)}");
        Assert.That(cuffs, Has.Comp<HandcuffComponent>(Server));
        Assert.That(secondCuffs, Has.Comp<HandcuffComponent>(Server));

        // Test to ensure cuffed players register the handcuffs
        _sCuffableSystem.TryAddNewCuffs(human, human, cuffs, cuffed);
        Assert.That(cuffed!.CuffedHandCount, Is.GreaterThan(0), "Handcuffing a player did not result in their hands being cuffed");

        // Test to ensure a player with 4 hands will still only have 2 hands cuffed
        AddHand(SEntMan.GetNetEntity(human), _sConsoleHost);
        AddHand(SEntMan.GetNetEntity(human), _sConsoleHost);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cuffed.CuffedHandCount, Is.EqualTo(2));
            Assert.That(hands!.SortedHands, Has.Count.EqualTo(4));
        }

        // Test to give a player with 4 hands 2 sets of cuffs
        _sCuffableSystem.TryAddNewCuffs(human, human, secondCuffs, cuffed);
        Assert.That(cuffed.CuffedHandCount, Is.EqualTo(4), "Player doesn't have correct amount of hands cuffed");
    }

    private static void AddHand(NetEntity to, IServerConsoleHost host)
    {
        host.ExecuteCommand(null, $"addhand {to}");
    }
}
