#nullable enable
using System.Linq;
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
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { ExtraPrototypes = Prototypes });
        var server = pairTracker.Pair.Server;
        var client = pairTracker.Pair.Client;

        var entMan = server.ResolveDependency<IEntityManager>();
        var sPlayerMan = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var conHost = client.ResolveDependency<IConsoleHost>();
        var mindSystem = entMan.System<MindSystem>();
        var session = sPlayerMan.ServerSessions.Single();

        // Spawn player entity & attach
        EntityUid originalMob = default;
        await server.WaitPost(() =>
        {
            originalMob = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mindSystem.TransferTo(session.ContentData()!.Mind!, originalMob, true);
        });

        // Check player got attached.
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.EqualTo(originalMob));

        // Use the ghost command
        conHost.ExecuteCommand("ghost");
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.Not.EqualTo(originalMob));

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
        Assert.That(session.AttachedEntity, Is.EqualTo(ghostRole));

        // Ghost again.
        conHost.ExecuteCommand("ghost");
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.Not.EqualTo(originalMob));
        Assert.That(session.AttachedEntity, Is.Not.EqualTo(ghostRole));

        // Next, control the original entity again:
        await server.WaitPost(() =>
        {
            mindSystem.TransferTo(session.ContentData()!.Mind!, originalMob, true);
        });
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);
        Assert.That(session.AttachedEntity, Is.EqualTo(originalMob));

        await pairTracker.CleanReturnAsync();
    }
}
