#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Actions;

[TestFixture]
[TestOf(typeof(SharedActionsSystem))]
public sealed class ActionPvsDetachTest : GameTest
{
    private static readonly EntProtoId TestMob = "MobHuman";

    [SidedDependency(Side.Server)] private readonly SharedActionsSystem _sActionsSys = null!;
    [SidedDependency(Side.Client)] private readonly SharedActionsSystem _cActionsSys = null!;
    [SidedDependency(Side.Server)] private readonly VisibilitySystem _sVisibilitySys = null!;

    [Test]
    public async Task TestActionDetach()
    {
        // Spawn mob that has some actions
        var map = await Pair.CreateTestMap();
        var ent = await SpawnAtPosition(TestMob, map.GridCoords);
        await RunTicksSync(5);
        var cEnt = ToClientUid(ent);

        // Verify that both the client & server agree on the number of actions
        var initActionsCount = _sActionsSys.GetActions(ent).Count();
        Assert.That(initActionsCount, Is.GreaterThan(0));
        Assert.That(initActionsCount, Is.EqualTo(_cActionsSys.GetActions(cEnt).Count()));

        // PVS-detach action entities
        // We do this by just giving them the ghost layer
        await Server.WaitPost(() =>
        {
            var enumerator = Server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                _sVisibilitySys.AddLayer(child, (int)VisibilityFlags.Ghost);
            }
        });

        // Client's actions have left been detached / are out of view, but action comp state has not changed
        Assert.That(_sActionsSys.GetActions(ent).Count(), Is.EqualTo(initActionsCount));
        Assert.That(_cActionsSys.GetActions(cEnt).Count(), Is.EqualTo(initActionsCount));

        // Re-enter PVS view
        await Server.WaitPost(() =>
        {
            var enumerator = Server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                _sVisibilitySys.RemoveLayer(child, (int)VisibilityFlags.Ghost);
            }
        });

        Assert.That(_sActionsSys.GetActions(ent).Count(), Is.EqualTo(initActionsCount));
        Assert.That(_cActionsSys.GetActions(cEnt).Count(), Is.EqualTo(initActionsCount));
    }
}
