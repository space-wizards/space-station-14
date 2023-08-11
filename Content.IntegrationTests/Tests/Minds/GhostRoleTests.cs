#nullable enable
using System.Linq;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Players;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostRoleTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: GhostRoleTestEntity
  components:
  - type: MindContainer
  - type: GhostRole
  - type: GhostTakeoverAvailable
";

    /// <summary>
    /// This is a simple test that just checks if a player can take a ghost roll and then regain control of their
    /// original entity without encountering errors.
    /// </summary>
    [Test]
    public async Task TakeRoleAndReturn()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true
        });
        var server = pairTracker.Pair.Server;
        var client = pairTracker.Pair.Client;

        var entMan = server.ResolveDependency<IEntityManager>();
        var sPlayerMan = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var conHost = client.ResolveDependency<IConsoleHost>();
        var mindSystem = entMan.System<MindSystem>();
        var session = sPlayerMan.ServerSessions.Single();
        var originalMind = session.ContentData()!.Mind!;

        // Spawn player entity & attach
        EntityUid originalMob = default;
        await server.WaitPost(() =>
        {
            originalMob = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mindSystem.TransferTo(originalMind, originalMob, true);
        });

        // Check player got attached.
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.EqualTo(originalMob));
        Assert.That(originalMind.OwnedEntity, Is.EqualTo(originalMob));
        Assert.Null(originalMind.VisitingEntity);

        // Use the ghost command
        conHost.ExecuteCommand("ghost");
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        var ghost = session.AttachedEntity;
        Assert.That(entMan.HasComponent<GhostComponent>(ghost));
        Assert.That(ghost, Is.Not.EqualTo(originalMob));
        Assert.That(session.ContentData()?.Mind, Is.EqualTo(originalMind));
        Assert.That(originalMind.OwnedEntity, Is.EqualTo(originalMob));
        Assert.That(originalMind.VisitingEntity, Is.EqualTo(ghost));

        // Spawn ghost takeover entity.
        EntityUid ghostRole = default;
        await server.WaitPost(() => ghostRole = entMan.SpawnEntity("GhostRoleTestEntity", MapCoordinates.Nullspace));

        // Take the ghost role
        await server.WaitPost(() =>
        {
            var id = entMan.GetComponent<GhostRoleComponent>(ghostRole).Identifier;
            entMan.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
        });

        // Check player got attached to ghost role.
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        var newMind = session.ContentData()!.Mind!;
        Assert.That(newMind, Is.Not.EqualTo(originalMind));
        Assert.That(session.AttachedEntity, Is.EqualTo(ghostRole));
        Assert.That(newMind.OwnedEntity, Is.EqualTo(ghostRole));
        Assert.Null(newMind.VisitingEntity);

        // Original mind should be unaffected, but the ghost will have deleted itself.
        Assert.That(originalMind.OwnedEntity, Is.EqualTo(originalMob));
        Assert.Null(originalMind.VisitingEntity);
        Assert.That(entMan.Deleted(ghost));

        // Ghost again.
        conHost.ExecuteCommand("ghost");
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        var otherGhost = session.AttachedEntity;
        Assert.That(entMan.HasComponent<GhostComponent>(otherGhost));
        Assert.That(otherGhost, Is.Not.EqualTo(originalMob));
        Assert.That(otherGhost, Is.Not.EqualTo(ghostRole));
        Assert.That(session.ContentData()?.Mind, Is.EqualTo(newMind));
        Assert.That(newMind.OwnedEntity, Is.EqualTo(ghostRole));
        Assert.That(newMind.VisitingEntity, Is.EqualTo(session.AttachedEntity));

        // Next, control the original entity again:
        await server.WaitPost(() => mindSystem.SetUserId(originalMind, session.UserId));
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.EqualTo(originalMob));
        Assert.That(originalMind.OwnedEntity, Is.EqualTo(originalMob));
        Assert.Null(originalMind.VisitingEntity);

        // the ghost-role mind is unaffected, though the ghost will have deleted itself
        Assert.That(newMind.OwnedEntity, Is.EqualTo(ghostRole));
        Assert.Null(newMind.VisitingEntity);
        Assert.That(entMan.Deleted(otherGhost));

        await pairTracker.CleanReturnAsync();
    }
}
