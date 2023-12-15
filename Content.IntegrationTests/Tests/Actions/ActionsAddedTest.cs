using System.Linq;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// This tests checks that actions properly get added to an entity's actions component..
/// </summary>
[TestFixture]
public sealed class ActionsAddedTest
{
    // TODO add magboot test (inventory action)
    // TODO add ghost toggle-fov test (client-side action)

    [Test]
    public async Task TestCombatActionsAdded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false});
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var clientSession = client.ResolveDependency<Robust.Client.Player.IPlayerManager>().LocalPlayer?.Session;
        var serverSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sActionSystem = server.System<SharedActionsSystem>();
        var cActionSystem = client.System<SharedActionsSystem>();

        // Dummy ticker is disabled - client should be in control of a normal mob.
        Assert.NotNull(serverSession.AttachedEntity);
        var serverEnt = serverSession.AttachedEntity!.Value;
        var clientEnt = clientSession!.AttachedEntity!.Value;
        Assert.That(sEntMan.EntityExists(serverEnt));
        Assert.That(cEntMan.EntityExists(clientEnt));
        Assert.That(sEntMan.HasComponent<ActionsComponent>(serverEnt));
        Assert.That(cEntMan.HasComponent<ActionsComponent>(clientEnt));
        Assert.That(sEntMan.HasComponent<CombatModeComponent>(serverEnt));
        Assert.That(cEntMan.HasComponent<CombatModeComponent>(clientEnt));

        var sComp = sEntMan.GetComponent<ActionsComponent>(serverEnt);
        var cComp = cEntMan.GetComponent<ActionsComponent>(clientEnt);

        // Mob should have a combat-mode action.
        // This action should have a non-null event both on the server & client.
        var evType = typeof(ToggleCombatActionEvent);

        var sActions = sActionSystem.GetActions(serverEnt).Where(
            x => x.Comp is InstantActionComponent act && act.Event?.GetType() == evType).ToArray();
        var cActions = cActionSystem.GetActions(clientEnt).Where(
            x => x.Comp is InstantActionComponent act && act.Event?.GetType() == evType).ToArray();

        Assert.That(sActions.Length, Is.EqualTo(1));
        Assert.That(cActions.Length, Is.EqualTo(1));

        var sAct = sActions[0].Comp;
        var cAct = cActions[0].Comp;

        Assert.NotNull(sAct);
        Assert.NotNull(cAct);

        // Finally, these two actions are not the same object
        // required, because integration tests do not respect the [NonSerialized] attribute and will simply events by reference.
        Assert.That(ReferenceEquals(sAct, cAct), Is.False);
        Assert.That(ReferenceEquals(sAct.BaseEvent, cAct.BaseEvent), Is.False);

        await pair.CleanReturnAsync();
    }
}
