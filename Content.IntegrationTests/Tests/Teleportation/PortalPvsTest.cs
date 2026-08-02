using Content.IntegrationTests.Fixtures;
using Robust.Shared;
using Content.Shared.CCVar;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Teleportation;

[TestFixture]
public sealed class PortalPvsTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task NearbyPlayerReceivesBothNearestPortalViews()
    {
        await Server.WaitPost(() =>
        {
            Server.CfgMan.SetCVar(CVars.NetPVS, true);
            Server.CfgMan.SetCVar(CCVars.PortalMaxPreloaded, 1);
        });
        await Pair.RunUntilSynced();

        var map = await Pair.CreateTestMap();
        EntityUid player = default;
        EntityUid nearestEntrance = default;
        EntityUid nearestDestination = default;
        EntityUid nearestDestinationMarker = default;
        EntityUid fartherEntrance = default;
        EntityUid fartherDestination = default;
        EntityUid fartherDestinationMarker = default;

        await Server.WaitPost(() =>
        {
            Assert.That(ServerSession?.AttachedEntity, Is.Not.Null);
            player = ServerSession!.AttachedEntity!.Value;

            var playerCoords = map.GridCoords.Offset(new(0, 0));
            var nearestEntranceCoords = map.GridCoords.Offset(new(1, 0));
            var nearestDestinationCoords = map.GridCoords.Offset(new(80, 0));
            var fartherEntranceCoords = map.GridCoords.Offset(new(4, 0));
            var fartherDestinationCoords = map.GridCoords.Offset(new(100, 0));

            nearestEntrance = SEntMan.SpawnAtPosition("PortalRed", nearestEntranceCoords);
            nearestDestination = SEntMan.SpawnAtPosition("PortalBlue", nearestDestinationCoords);
            nearestDestinationMarker = SEntMan.SpawnAtPosition("FoodDonkpocket", nearestDestinationCoords.Offset(new(1, 0)));

            fartherEntrance = SEntMan.SpawnAtPosition("PortalRed", fartherEntranceCoords);
            fartherDestination = SEntMan.SpawnAtPosition("PortalBlue", fartherDestinationCoords);
            fartherDestinationMarker = SEntMan.SpawnAtPosition("FoodDonkpocket", fartherDestinationCoords.Offset(new(1, 0)));

            var link = SEntMan.System<LinkedEntitySystem>();
            link.TryLink(nearestEntrance, nearestDestination);
            link.TryLink(fartherEntrance, fartherDestination);

            SEntMan.System<SharedTransformSystem>().SetCoordinates(player, playerCoords);
        });

        await Pair.RunUntilSynced();

        await Server.WaitAssertion(() =>
        {
            Assert.That(ServerSession!.ViewSubscriptions.Contains(nearestEntrance), Is.True, "Nearest entrance portal was not a view subscription.");
            Assert.That(ServerSession.ViewSubscriptions.Contains(nearestDestination), Is.True, "Nearest linked portal was not a view subscription.");
            Assert.That(ServerSession.ViewSubscriptions.Contains(fartherEntrance), Is.False, "Farther entrance portal should not be preloaded when the limit is 1.");
            Assert.That(ServerSession.ViewSubscriptions.Contains(fartherDestination), Is.False, "Farther linked portal should not be preloaded when the limit is 1.");
        });

        var clientNearestDestination = Pair.ToClientUid(nearestDestination);
        var clientNearestMarker = Pair.ToClientUid(nearestDestinationMarker);

        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.EntityExists(clientNearestDestination), Is.True, "Nearest linked portal destination did not enter client PVS.");
            Assert.That(CEntMan.EntityExists(clientNearestMarker), Is.True, "Entity near the nearest linked portal did not enter client PVS.");
        });
    }
}
