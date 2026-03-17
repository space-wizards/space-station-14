#nullable enable
using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostRoleTests
{
    private const string GhostRoleProtoId = "GhostRoleTestEntity";
    private const string TestMobProtoId = "GhostRoleTestMob";

    [TestPrototypes]
    private const string Prototypes = $"""
        - type: entity
          id: {GhostRoleProtoId}
          components:
          - type: MindContainer
          - type: GhostRole
          - type: GhostTakeoverAvailable
          - type: MobState

        - type: entity
          id: {TestMobProtoId}
          components:
          - type: MobState # MobState is required for correct determination of if the player can return to body or not
        """;

    /// <summary>
    /// This is a simple test that just checks if a player can take a ghost role and then regain control of their
    /// original entity without encountering errors.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task TakeRoleAndReturn(bool adminGhost)
    {
        var ghostCommand = adminGhost ? "aghost" : "ghost";

        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true
        });
        var server = pair.Server;
        var client = pair.Client;

        var mapData = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var sPlayerMan = server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var conHost = client.ResolveDependency<IConsoleHost>();
        var mindSystem = entMan.System<SharedMindSystem>();
        var session = sPlayerMan.Sessions.Single();
        var originalPlayerMindId = session.ContentData()!.Mind!.Value;

        // Check that there are no ghosts
        Assert.That(entMan.Count<GhostComponent>(), Is.Zero);

        // Spawn player entity & attach
        EntityUid originalPlayerMob = default;
        await server.WaitPost(() =>
        {
            originalPlayerMob = entMan.SpawnEntity(TestMobProtoId, mapData.GridCoords);
            mindSystem.TransferTo(originalPlayerMindId, originalPlayerMob, true);
        });

        await pair.RunTicksSync(10);
        var originalPlayerMind = entMan.GetComponent<MindComponent>(originalPlayerMindId);
        Assert.Multiple(() =>
        {
            // Check player got attached.
            Assert.That(session.AttachedEntity, Is.EqualTo(originalPlayerMob));
            Assert.That(originalPlayerMind.OwnedEntity, Is.EqualTo(originalPlayerMob));
            Assert.That(originalPlayerMind.VisitingEntity, Is.Null);
            Assert.That(originalPlayerMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));

            // Check that there are still no ghosts
            Assert.That(entMan.Count<GhostComponent>(), Is.Zero);
        });

        // Use the ghost command
        conHost.ExecuteCommand(ghostCommand);
        await pair.RunTicksSync(10);
        var ghostOne = session.AttachedEntity;
        Assert.Multiple(() =>
        {
            // Assert that the ghost is a new entity with a new mind
            Assert.That(entMan.HasComponent<GhostComponent>(ghostOne));
            Assert.That(ghostOne, Is.Not.EqualTo(originalPlayerMob));
            Assert.That(session.ContentData()?.Mind, Is.EqualTo(originalPlayerMindId));
            if (adminGhost)
            {
                // aghost, so the player mob should still own the mind, but the mind is visiting the ghost.
                Assert.That(originalPlayerMind.OwnedEntity, Is.EqualTo(originalPlayerMob));
                Assert.That(originalPlayerMind.VisitingEntity, Is.EqualTo(ghostOne));
                Assert.That(originalPlayerMind.UserId, Is.EqualTo(session.UserId));
            }
            else
            {
                // player ghost, can't return. The mind is owned by the ghost, and is not visiting.
                Assert.That(originalPlayerMind.OwnedEntity, Is.EqualTo(ghostOne));
                Assert.That(originalPlayerMind.VisitingEntity, Is.Null);
            }

            // Check that we're tracking the original owner for round end screen
            Assert.That(originalPlayerMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));

            // Check that there is only one ghost
            Assert.That(entMan.Count<GhostComponent>(), Is.EqualTo(1));
        });

        // Spawn ghost takeover entity.
        EntityUid ghostRole = default;
        await server.WaitPost(() => ghostRole = entMan.SpawnEntity(GhostRoleProtoId, mapData.GridCoords));

        // Take the ghost role
        await server.WaitPost(() =>
        {
            var id = entMan.GetComponent<GhostRoleComponent>(ghostRole).Identifier;
            entMan.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
        });

        // Check player got attached to ghost role.
        await pair.RunTicksSync(10);
        var ghostRoleMindId = session.ContentData()!.Mind!.Value;
        var ghostRoleMind = entMan.GetComponent<MindComponent>(ghostRoleMindId);
        Assert.Multiple(() =>
        {
            // Check that the ghost role mind is new
            Assert.That(ghostRoleMindId, Is.Not.EqualTo(originalPlayerMindId));

            // Check that the session and mind are properly attached to the ghost role
            Assert.That(session.AttachedEntity, Is.EqualTo(ghostRole));
            Assert.That(ghostRoleMind.OwnedEntity, Is.EqualTo(ghostRole));
            Assert.That(ghostRoleMind.VisitingEntity, Is.Null);

            // Original mind should be unaffected, but the ghost will have deleted itself.
            if (adminGhost)
            {
                // aghost case, the original player mob should still own the mind, and that mind is not visiting.
                Assert.That(originalPlayerMind.OwnedEntity, Is.EqualTo(originalPlayerMob));
            }
            else
            {
                // player ghost case, the original mind is disconnected and not owned by an entity.
                // This mind cannot be returned to
                Assert.That(originalPlayerMind.OwnedEntity, Is.Null);
            }

            // In either case the original player mind is not visiting anything, not connected to any user.
            Assert.That(originalPlayerMind.VisitingEntity, Is.Null);
            Assert.That(originalPlayerMind.UserId, Is.Null);

            // Now the original owner of both minds should permanently be set to this session.
            Assert.That(originalPlayerMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));
            Assert.That(ghostRoleMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));

            // Make sure that the ghost was deleted
            Assert.That(entMan.Deleted(ghostOne));

            // Check that there is are no lingereing ghosts
            Assert.That(entMan.Count<GhostComponent>(), Is.Zero);
        });

        // Ghost again.
        conHost.ExecuteCommand(ghostCommand);
        await pair.RunTicksSync(10);
        var ghostTwo = session.AttachedEntity;
        Assert.Multiple(() =>
        {
            // Check that the new ghost is a new entity
            Assert.That(entMan.HasComponent<GhostComponent>(ghostTwo));
            Assert.That(ghostTwo, Is.Not.EqualTo(originalPlayerMob));
            Assert.That(ghostTwo, Is.Not.EqualTo(ghostRole));
            Assert.That(session.ContentData()?.Mind, Is.EqualTo(ghostRoleMindId));

            if(adminGhost)
            {
                // aghost case, the ghost role mind should be owned by the ghost role entity,
                // the ghost role mind is visiting the new ghost
                Assert.That(ghostRoleMind.OwnedEntity, Is.EqualTo(ghostRole));
                Assert.That(ghostRoleMind.VisitingEntity, Is.EqualTo(ghostTwo));
            }
            else
            {
                // player ghost, can't return. The mind is owned by the ghost, and is not visiting.
                Assert.That(ghostRoleMind.OwnedEntity, Is.EqualTo(ghostTwo));
                Assert.That(ghostRoleMind.VisitingEntity, Is.Null);
            }

            // Check that the original mind is still not attached to a user
            Assert.That(originalPlayerMind.UserId, Is.Null);

            // Check that original owners of other minds are still tracked
            Assert.That(originalPlayerMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));
            Assert.That(ghostRoleMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));

            // Check that there is exactly one ghost
            Assert.That(entMan.Count<GhostComponent>(), Is.EqualTo(1));
        });

        if (!adminGhost)
        {
            // End of the normal player ghost role test
            await pair.CleanReturnAsync();
            return;
        }

        // Next, control the original entity again:
        await server.WaitPost(() => mindSystem.SetUserId(originalPlayerMindId, session.UserId));
        await pair.RunTicksSync(10);

        Assert.Multiple(() =>
        {
            // Check that we are attached
            Assert.That(session.AttachedEntity, Is.EqualTo(originalPlayerMob));

            // Check the ownership of the original mind
            Assert.That(originalPlayerMind.OwnedEntity, Is.EqualTo(originalPlayerMob));
            Assert.That(originalPlayerMind.VisitingEntity, Is.Null);
            Assert.That(originalPlayerMind.UserId, Is.EqualTo(session.UserId));

            // Check that the ghost-role mind is unaffected
            Assert.That(ghostRoleMind.OwnedEntity, Is.EqualTo(ghostRole));
            Assert.That(ghostRoleMind.VisitingEntity, Is.Null);

            // Check that the second ghost is deleted
            Assert.That(entMan.Deleted(ghostTwo));

            // Check that the original owners of the previous minds are still tracked
            Assert.That(originalPlayerMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));
            Assert.That(ghostRoleMind.OriginalOwnerUserId, Is.EqualTo(session.UserId));

            // Check that there is are no lingereing ghosts
            Assert.That(entMan.Count<GhostComponent>(), Is.Zero);
        });

        await pair.CleanReturnAsync();
    }
}
