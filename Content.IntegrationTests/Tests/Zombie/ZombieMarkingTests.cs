using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Zombies;

namespace Content.IntegrationTests.Tests.Zombie;

[TestOf(typeof(ZombieSystem))]
public sealed class ZombieMarkingTests : InteractionTest
{
    protected override string PlayerPrototype => "MobVulpkanin";

    [Test]
    public async Task ProfileApplication()
    {
        await Server.WaitAssertion(() =>
        {
            var zombie = SEntMan.System<ZombieSystem>();
            var visualBody = SEntMan.System<SharedVisualBodySystem>();
            zombie.ZombifyEntity(SPlayer);
            var comp = SEntMan.GetComponent<ZombieComponent>(SPlayer);

            if (!visualBody.TryGatherMarkingsData(SPlayer,
                    null,
                    out var profiles,
                    out _,
                    out _))
            {
                Assert.Fail($"Failed to gather markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
            }

            foreach (var (organ, profile) in profiles)
            {
                Assert.That(profile.SkinColor, Is.EqualTo(comp.SkinColor), $"Organ {organ} has non-zombified skin color");
                Assert.That(profile.EyeColor, Is.EqualTo(comp.EyeColor), $"Organ {organ} has non-zombified skin color");
            }
        });
    }

    [Test]
    public async Task MarkingApplication()
    {
        await Server.WaitAssertion(() =>
        {
            var visualBody = SEntMan.System<SharedVisualBodySystem>();
            if (!visualBody.TryGatherMarkingsData(SPlayer,
                    null,
                    out _,
                    out _,
                    out var preZombieMarkings))
            {
                Assert.Fail($"Failed to gather pre-zombie markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
            }

            var zombie = SEntMan.System<ZombieSystem>();
            zombie.ZombifyEntity(SPlayer);
            var comp = SEntMan.GetComponent<ZombieComponent>(SPlayer);

            if (!visualBody.TryGatherMarkingsData(SPlayer,
                    null,
                    out _,
                    out _,
                    out var postZombieMarkings))
            {
                Assert.Fail($"Failed to gather post-zombie markings data for {SEntMan.ToPrettyString(SPlayer):SPlayer}");
            }

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
                        {
                            Assert.That(color, Is.EqualTo(comp.SkinColor), $"Zombification should change {postMarking.MarkingId} on layer {layer} to the skin color");
                        }
                    }
                }
            }
        });
    }
}
