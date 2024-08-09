using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Buckle;

public sealed partial class BuckleTest
{
    [Test]
    public async Task BuckleInteractUnbuckleOther()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var buckleSystem = entMan.System<SharedBuckleSystem>();

        EntityUid user = default;
        EntityUid victim = default;
        EntityUid chair = default;
        BuckleComponent buckle = null;
        StrapComponent strap = null;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity(BuckleDummyId, MapCoordinates.Nullspace);
            victim = entMan.SpawnEntity(BuckleDummyId, MapCoordinates.Nullspace);
            chair = entMan.SpawnEntity(StrapDummyId, MapCoordinates.Nullspace);

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

            // InteractHand with chair to unbuckle victim
            entMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(strap.BuckledEntities, Does.Not.Contain(victim));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BuckleInteractBuckleUnbuckleSelf()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        EntityUid user = default;
        EntityUid chair = default;
        BuckleComponent buckle = null;
        StrapComponent strap = null;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity(BuckleDummyId, MapCoordinates.Nullspace);
            chair = entMan.SpawnEntity(StrapDummyId, MapCoordinates.Nullspace);

            Assert.That(entMan.TryGetComponent(user, out buckle));
            Assert.That(entMan.TryGetComponent(chair, out strap));

#pragma warning disable RA0002
            buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

            // Buckle user to chair
            entMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
                Assert.That(buckle.Buckled, "Victim is not buckled.");
                Assert.That(strap.BuckledEntities, Does.Contain(user), "Chair does not have victim buckled to it.");
            });

            // InteractHand with chair to unbuckle
            entMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(strap.BuckledEntities, Does.Not.Contain(user));
            });
        });

        await pair.CleanReturnAsync();
    }
}
