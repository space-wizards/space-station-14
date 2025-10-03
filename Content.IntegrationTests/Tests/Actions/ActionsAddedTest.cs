using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.CombatMode;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// This tests checks that actions properly get added to an entity's actions component..
/// </summary>
[TestFixture]
public sealed class ActionsAddedTest
{

    [Test]
    public async Task TestCombatActionsAdded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var clientSession = client.Session;
        var serverSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sActionSystem = server.System<SharedActionsSystem>();
        var cActionSystem = client.System<SharedActionsSystem>();

        // Dummy ticker is disabled - client should be in control of a normal mob.
        Assert.That(serverSession.AttachedEntity, Is.Not.Null);
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

        var sQuery = sEntMan.GetEntityQuery<InstantActionComponent>();
        var cQuery = cEntMan.GetEntityQuery<InstantActionComponent>();
        var sActions = sActionSystem.GetActions(serverEnt).Where(
            ent => sQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();
        var cActions = cActionSystem.GetActions(clientEnt).Where(
            ent => cQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();

        Assert.That(sActions.Length, Is.EqualTo(1));
        Assert.That(cActions.Length, Is.EqualTo(1));

        var sAct = sActions[0];
        var cAct = cActions[0];

        Assert.That(sAct.Comp, Is.Not.Null);
        Assert.That(cAct.Comp, Is.Not.Null);

        // Finally, these two actions are not the same object
        // required, because integration tests do not respect the [NonSerialized] attribute and will simply events by reference.
        Assert.That(ReferenceEquals(sAct.Comp, cAct.Comp), Is.False);
        Assert.That(ReferenceEquals(sQuery.GetComponent(sAct).Event, cQuery.GetComponent(cAct).Event), Is.False);

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestMagbootActionsAdded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var sProtoMan = server.ResolveDependency<IPrototypeManager>();
        var sInventorySystem = server.System<InventorySystem>();
        var sActionSystem = server.System<SharedActionsSystem>();
        var cActionSystem = client.System<SharedActionsSystem>();
        var serverSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var clientSession = client.Session;

        Assert.That(serverSession.AttachedEntity, Is.Not.Null);
        var serverEnt = serverSession.AttachedEntity!.Value;
        var clientEnt = clientSession!.AttachedEntity!.Value;

        await pair.RunTicksSync(5);

        EntityUid sMagboots = default;
        EntityUid cMagboots = default;

        await server.WaitPost(() =>
        {
            var proto = sProtoMan.Index<EntityPrototype>("ClothingShoesBootsMag");
            sMagboots = sEntMan.SpawnEntity(proto.ID, sEntMan.GetComponent<TransformComponent>(serverEnt).Coordinates);
            Assert.That(sEntMan.HasComponent<MagbootsComponent>(sMagboots));
        });

        await pair.RunTicksSync(5);

        cMagboots = cEntMan.GetNetEntity(sEntMan.GetNetEntity(sMagboots)).Value;
        Assert.That(cEntMan.EntityExists(cMagboots));

        await server.WaitPost(() =>
        {
            Assert.That(sInventorySystem.TryEquip(serverEnt, sMagboots, "shoes", force: true));
        });

        await pair.RunTicksSync(5);

        var sQuery = sEntMan.GetEntityQuery<InstantActionComponent>();
        var cQuery = cEntMan.GetEntityQuery<InstantActionComponent>();

        var sActions = sActionSystem.GetActions(serverEnt).ToArray();
        var cActions = cActionSystem.GetActions(clientEnt).ToArray();

        Assert.That(sActions.Length, Is.GreaterThan(0), "Server should have at least one action after equipping magboots");
        Assert.That(cActions.Length, Is.GreaterThan(0), "Client should have at least one action after equipping magboots");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestGhostToggleFovActionAdded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var sActionSystem = server.System<SharedActionsSystem>();
        var cActionSystem = client.System<SharedActionsSystem>();
        var serverSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        EntityUid serverGhost = default;

        await server.WaitPost(() =>
        {
            serverGhost = sEntMan.SpawnEntity("MobObserver", default);
            server.PlayerMan.SetAttachedEntity(serverSession, serverGhost);
        });

        await pair.RunTicksSync(5);

        var clientSession = client.Session;
        var clientGhost = clientSession!.AttachedEntity!.Value;

        Assert.That(sEntMan.EntityExists(serverGhost));
        Assert.That(cEntMan.EntityExists(clientGhost));
        Assert.That(sEntMan.HasComponent<GhostComponent>(serverGhost));
        Assert.That(cEntMan.HasComponent<GhostComponent>(clientGhost));
        Assert.That(sEntMan.HasComponent<ActionsComponent>(serverGhost));
        Assert.That(cEntMan.HasComponent<ActionsComponent>(clientGhost));

        var evType = typeof(ToggleFoVActionEvent);

        var sQuery = sEntMan.GetEntityQuery<InstantActionComponent>();
        var cQuery = cEntMan.GetEntityQuery<InstantActionComponent>();
        var sActions = sActionSystem.GetActions(serverGhost).Where(
            ent => sQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();
        var cActions = cActionSystem.GetActions(clientGhost).Where(
            ent => cQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();

        Assert.That(sActions.Length, Is.EqualTo(1), "Server ghost should have exactly one ToggleFoV action");
        Assert.That(cActions.Length, Is.EqualTo(1), "Client ghost should have exactly one ToggleFoV action");

        var sAct = sActions[0];
        var cAct = cActions[0];

        Assert.That(sAct.Comp, Is.Not.Null);
        Assert.That(cAct.Comp, Is.Not.Null);

        Assert.That(ReferenceEquals(sAct.Comp, cAct.Comp), Is.False);
        Assert.That(ReferenceEquals(sQuery.GetComponent(sAct).Event, cQuery.GetComponent(cAct).Event), Is.False);

        await pair.CleanReturnAsync();
    }
}
