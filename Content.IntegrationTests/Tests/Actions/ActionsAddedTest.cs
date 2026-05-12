#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// This test checks that actions properly get added to an entity's actions component.
/// </summary>
public sealed class ActionsAddedTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [SidedDependency(Side.Server)] private readonly SharedActionsSystem _sActionSystem = default!;
    [SidedDependency(Side.Client)] private readonly SharedActionsSystem _cActionSystem = default!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<InstantActionComponent> _sQuery = default!;
    [SidedDependency(Side.Client)] private readonly EntityQuery<InstantActionComponent> _cQuery = default!;

    // TODO add magboot test (inventory action)
    // TODO add ghost toggle-fov test (client-side action)

    [Test]
    public async Task TestCombatActionsAdded()
    {
        var clientSession = Client.Session;
        var serverSession = Server.ResolveDependency<IPlayerManager>().Sessions.Single();

        // Dummy ticker is disabled - client should be in control of a normal mob.
        Assert.That(serverSession.AttachedEntity, Is.Not.Null);
        var serverEnt = serverSession.AttachedEntity!.Value;
        var clientEnt = clientSession!.AttachedEntity!.Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverEnt, Is.Not.Deleted(Server));
            Assert.That(clientEnt, Is.Not.Deleted(Client));
            Assert.That(serverEnt, Has.Comp<ActionsComponent>(Server));
            Assert.That(clientEnt, Has.Comp<ActionsComponent>(Client));
            Assert.That(serverEnt, Has.Comp<CombatModeComponent>(Server));
            Assert.That(clientEnt, Has.Comp<CombatModeComponent>(Client));
        }

        var sComp = SComp<ActionsComponent>(serverEnt);
        var cComp = CComp<ActionsComponent>(clientEnt);

        // Mob should have a combat-mode action.
        // This action should have a non-null event both on the server & client.
        var evType = typeof(ToggleCombatActionEvent);

        var sActions = _sActionSystem.GetActions(serverEnt).Where(
            ent => _sQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();
        var cActions = _cActionSystem.GetActions(clientEnt).Where(
            ent => _cQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sActions, Has.Length.EqualTo(1));
            Assert.That(cActions, Has.Length.EqualTo(1));
        }

        var sAct = sActions[0];
        var cAct = cActions[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sAct.Comp, Is.Not.Null);
            Assert.That(cAct.Comp, Is.Not.Null);

            // Finally, make sure these two actions are not the same object
            // Required because integration tests do not respect the [NonSerialized] attribute and will simply compare events by reference.
            Assert.That(ReferenceEquals(sAct.Comp, cAct.Comp), Is.False);
            Assert.That(ReferenceEquals(_sQuery.GetComponent(sAct).Event, _cQuery.GetComponent(cAct).Event), Is.False);
        }

    }
}
