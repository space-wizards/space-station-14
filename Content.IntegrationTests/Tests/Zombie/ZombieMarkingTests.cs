#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Zombie;

[TestOf(typeof(ZombieSystem))]
public sealed class ZombieMarkingTests : InteractionTest
{
    protected override string PlayerPrototype => "MobVulpkanin";

    [SidedDependency(Side.Server)] private ZombieSystem _sZombieSystem = default!;
    [SidedDependency(Side.Server)] private SharedVisualBodySystem _sVisualBodySystem = default!;

    [SidedDependency(Side.Server)] private EntityQuery<ZombieComponent> _sQuery = default!;

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that all organs change color from the player's HumanoidCharacterProfile when zombified.")]
    public async Task ProfileApplication()
    {
        _sZombieSystem.ZombifyEntity(SPlayer);
        var comp = SEntMan.GetComponent<ZombieComponent>(SPlayer);

        Assert.That(
            _sVisualBodySystem.TryGatherMarkingsData(SPlayer,
                null,
                out var profiles,
                out _,
                out _),
            Is.True,
            $"Failed to gather markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
        Assert.That(profiles, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            foreach (var (organ, profile) in profiles)
            {
                Assert.That(profile.SkinColor, Is.EqualTo(comp.SkinColor), $"Organ {organ} has non-zombified skin color");
                Assert.That(profile.EyeColor, Is.EqualTo(comp.EyeColor), $"Organ {organ} has non-zombified skin color");
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Checks that zombification only changes an entity's markings, not removing or adding them, or mutating its organs.")]
    public async Task MarkingApplication()
    {
        Assert.That(
            _sVisualBodySystem.TryGatherMarkingsData(SPlayer,
                null,
                out _,
                out _,
                out var preZombieMarkings),
            Is.True,
            $"Failed to gather pre-zombie markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
        Assert.That(preZombieMarkings, Is.Not.Null);

        _sZombieSystem.ZombifyEntity(SPlayer);
        var comp = _sQuery.Comp(SPlayer);

        Assert.That(
            _sVisualBodySystem.TryGatherMarkingsData(SPlayer,
                null,
                out _,
                out _,
                out var postZombieMarkings),
            Is.True,
            $"Failed to gather pre-zombie markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
        Assert.That(postZombieMarkings, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            foreach (var (organ, layers) in postZombieMarkings)
            {
                Assert.That(preZombieMarkings, Does.ContainKey(organ), "Zombification added organs (it shouldn't)");
                Assert.That(preZombieMarkings[organ], Is.Not.SameAs(layers), "Zombification shouldn't mutate the existing data structures");

                foreach (var (layer, markingSet) in layers)
                {
                    Assert.That(preZombieMarkings[organ], Does.ContainKey(layer), "Zombification added layers (it shouldn't)");
                    Assert.That(preZombieMarkings[organ][layer], Is.Not.SameAs(markingSet), "Zombification shouldn't mutate the existing data structures");
                    Assert.That(preZombieMarkings[organ][layer], Has.Count.EqualTo(markingSet.Count), "Zombification shouldn't change the amount of markings");

                    if (!ZombieSystem.AdditionalZombieLayers.Contains(layer))
                        continue;

                    foreach (var (preMarking, postMarking) in preZombieMarkings[organ][layer].Zip(markingSet))
                    {
                        Assert.That(preMarking, Is.Not.EqualTo(postMarking), $"Zombification should change marking {postMarking.MarkingId} on layer {layer}");

                        foreach (var color in postMarking.MarkingColors)
                            Assert.That(color, Is.EqualTo(comp.SkinColor), $"Zombification should change {postMarking.MarkingId} on layer {layer} to the skin color");
                    }
                }
            }
        }
    }
}
