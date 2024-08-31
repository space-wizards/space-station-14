using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Actions;

[TestFixture]
public sealed class ActionPvsDetachTest
{
    [Test]
    public async Task TestActionDetach()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var (server, client) = pair;
        var sys = server.System<SharedActionsSystem>();
        var cSys = client.System<SharedActionsSystem>();

        // Spawn mob that has some actions
        EntityUid ent = default;
        var map = await pair.CreateTestMap();
        await server.WaitPost(() => ent = server.EntMan.SpawnAtPosition("MobHuman", map.GridCoords));
        await pair.RunTicksSync(5);
        var cEnt = pair.ToClientUid(ent);

        // Verify that both the client & server agree on the number of actions
        var initActions = sys.GetActions(ent).Count();
        Assert.That(initActions, Is.GreaterThan(0));
        Assert.That(initActions, Is.EqualTo(cSys.GetActions(cEnt).Count()));

        // PVS-detach action entities
        // We do this by just giving them the ghost layer
        var visSys = server.System<VisibilitySystem>();

        await server.WaitPost(() =>
        {
            var enumerator = server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                visSys.AddLayer(child, (int)VisibilityFlags.Ghost);
            }
        });

        await pair.RunTicksSync(5);

        // Client's actions have left been detached / are out of view, but action comp state has not changed
        Assert.That(sys.GetActions(ent).Count(), Is.EqualTo(initActions));
        Assert.That(cSys.GetActions(cEnt).Count(), Is.EqualTo(initActions));

        // Re-enter PVS view
        await server.WaitPost(() =>
        {
            var enumerator = server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                visSys.RemoveLayer(child, (int) VisibilityFlags.Ghost);
            }
        });
        await pair.RunTicksSync(5);
        Assert.That(sys.GetActions(ent).Count(), Is.EqualTo(initActions));
        Assert.That(cSys.GetActions(cEnt).Count(), Is.EqualTo(initActions));

        await server.WaitPost(() => server.EntMan.DeleteEntity(map.MapUid));
        await pair.CleanReturnAsync();
    }
}
