using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
public sealed class HandTests
{
    [Test]
    public async Task TestPickupDrop()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var sys = entMan.System<SharedHandsSystem>();

        var data = await PoolManager.CreateTestMap(pairTracker);
        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        EntityUid item = default;
        EntityUid player = default;
        HandsComponent hands = default!;
        await server.WaitPost(() =>
        {
            player = playerMan.Sessions.First().AttachedEntity!.Value;
            var xform = entMan.GetComponent<TransformComponent>(player);
            item = entMan.SpawnEntity("Crowbar", xform.MapPosition);
            hands = entMan.GetComponent<HandsComponent>(player);
            sys.TryPickup(player, item, hands.ActiveHand!);
        });

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await PoolManager.RunTicksSync(pairTracker.Pair, 5);
        Assert.That(hands.ActiveHandEntity == item);

        await server.WaitPost(() =>
        {
            sys.TryDrop(player, item, null!);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);
        Assert.That(hands.ActiveHandEntity == null);

        await server.WaitPost(() => mapMan.DeleteMap(data.MapId));
        await pairTracker.CleanReturnAsync();
    }
}
