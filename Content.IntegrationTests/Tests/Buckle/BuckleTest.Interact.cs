using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Buckle;

public sealed partial class BuckleTest
{

    [Test]
    public async Task BuckleInteractUnbuckleOther()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var buckleSystem = entMan.System<SharedBuckleSystem>();
        var coordinates = testMap.GridCoords;

        EntityUid user = default;
        EntityUid victim = default;
        EntityUid chair = default;
        BuckleComponent buckle = null;
        StrapComponent strap = null;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity(BuckleDummyId, coordinates);
            victim = entMan.SpawnEntity(BuckleDummyId, coordinates);
            chair = entMan.SpawnEntity(StrapDummyId, coordinates);

            Assert.That(entMan.TryGetComponent(victim, out buckle));
            Assert.That(entMan.TryGetComponent(chair, out strap));

#pragma warning disable RA0002
            buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

            // Buckle victim to chair
            Assert.That(buckleSystem.TryBuckle(victim, user, chair, buckle));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
                Assert.That(buckle.Buckled, "Victim is not buckled.");
                Assert.That(strap.BuckledEntities, Does.Contain(victim), "Chair does not have victim buckled to it.");
            });
        });

        // Wait enough ticks for the unbuckling cooldown to run out
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            // InteractHand with chair
            buckleSystem.OnStrapInteractHand(chair, strap, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(strap.BuckledEntities, Does.Not.Contain(victim));
            });
        });

        await pair.CleanReturnAsync();
    }
}
