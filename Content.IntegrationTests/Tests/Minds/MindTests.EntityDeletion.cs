using System.Linq;
using Content.Server.Players;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

// Tests various scenarios where an entity that is associated with a player's mind is deleted.
public sealed partial class MindTests
{
    // This test will do the following:
    // - spawn a  player
    // - visit some entity
    // - delete the entity being visited
    // - assert that player returns to original entity
    [Test]
    public async Task TestDeleteVisiting()
    {
        await using var pair = await SetupPair();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

        EntityUid playerEnt = default;
        EntityUid visitEnt = default;
        EntityUid mindId = default!;
        MindComponent mind = default!;
        await server.WaitAssertion(() =>
        {
            var player = playerMan.ServerSessions.Single();

            playerEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            visitEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            mindId = mindSystem.CreateMind(player.UserId);
            mind = entMan.GetComponent<MindComponent>(mindId);
            mindSystem.TransferTo(mindId, playerEnt);
            mindSystem.Visit(mindId, visitEnt);

            Assert.Multiple(() =>
            {
                Assert.That(player.AttachedEntity, Is.EqualTo(visitEnt));
                Assert.That(mind.VisitingEntity, Is.EqualTo(visitEnt));
            });
        });

        await pair.RunTicksSync(5);
        await server.WaitPost(() => entMan.DeleteEntity(visitEnt));
        await pair.RunTicksSync(5);

#pragma warning disable NUnit2045 // Interdependent assertions.
        Assert.That(mind.VisitingEntity, Is.Null);
        Assert.That(entMan.EntityExists(mind.OwnedEntity));
        Assert.That(mind.OwnedEntity, Is.EqualTo(playerEnt));
#pragma warning restore NUnit2045

        // This used to throw so make sure it doesn't.
        await server.WaitPost(() => entMan.DeleteEntity(mind.OwnedEntity!.Value));
        await pair.RunTicksSync(5);

        await pair.CleanReturnAsync();
    }

    // this is a variant of TestGhostOnDelete that just deletes the whole map.
    [Test]
    public async Task TestGhostOnDeleteMap()
    {
        await using var pair = await SetupPair(dirty: true);
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();

        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

        EntityUid playerEnt = default;
        EntityUid mindId = default!;
        MindComponent mind = default!;
        await server.WaitAssertion(() =>
        {
            playerEnt = entMan.SpawnEntity(null, coordinates);
            mindId = player.ContentData()!.Mind!.Value;
            mind = entMan.GetComponent<MindComponent>(mindId);
            mindSystem.TransferTo(mindId, playerEnt);

            Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
        });

        await pair.RunTicksSync(5);
        await server.WaitPost(() => mapManager.DeleteMap(testMap.MapId));
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
#pragma warning disable NUnit2045 // Interdependent assertions.
            Assert.That(entMan.EntityExists(mind.CurrentEntity), Is.True);
            Assert.That(mind.CurrentEntity, Is.Not.EqualTo(playerEnt));
#pragma warning restore NUnit2045
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGhostOnDelete()
    {
        // Client is needed to spawn session
        await using var pair = await SetupPair(dirty: true);
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var player = playerMan.ServerSessions.Single();

        Assert.That(!entMan.HasComponent<GhostComponent>(player.AttachedEntity), "Player was initially a ghost?");

        // Delete entity
        await server.WaitPost(() => entMan.DeleteEntity(player.AttachedEntity!.Value));
        await pair.RunTicksSync(5);

        Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity), "Player did not become a ghost");

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that when the original mob gets deleted, the visited ghost does not get deleted.
    /// And that the visited ghost becomes the main mob.
    /// 1. Visit ghost
    /// 2. Delete original mob
    /// 3. Assert is ghost
    /// 4. Assert was not deleted
    /// 5. Assert is main mob
    /// </summary>
    [Test]
    public async Task TestOriginalDeletedWhileGhostingKeepsGhost()
    {
        // Client is needed to spawn session
        await using var pair = await SetupPair();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();
        var mind = GetMind(pair);

        var player = playerMan.ServerSessions.Single();
#pragma warning disable NUnit2045 // Interdependent assertions.
        Assert.That(player.AttachedEntity, Is.Not.Null);
        Assert.That(entMan.EntityExists(player.AttachedEntity));
#pragma warning restore NUnit2045
        var originalEntity = player.AttachedEntity.Value;

        EntityUid ghost = default!;
        await server.WaitAssertion(() =>
        {
            ghost = entMan.SpawnEntity("MobObserver", MapCoordinates.Nullspace);
            mindSystem.Visit(mind.Id, ghost);
        });

        Assert.Multiple(() =>
        {
            Assert.That(player.AttachedEntity, Is.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity), "player is not a ghost");
            Assert.That(mind.Comp.VisitingEntity, Is.EqualTo(player.AttachedEntity));
            Assert.That(mind.Comp.OwnedEntity, Is.EqualTo(originalEntity));
        });

        await pair.RunTicksSync(5);
        await server.WaitAssertion(() => entMan.DeleteEntity(originalEntity));
        await pair.RunTicksSync(5);
        Assert.That(entMan.Deleted(originalEntity));

        // Check that the player is still in control of the ghost
        mind = GetMind(pair);
        Assert.That(!entMan.Deleted(ghost), "ghost has been deleted");
        Assert.Multiple(() =>
        {
            Assert.That(player.AttachedEntity, Is.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity));
            Assert.That(mind.Comp.VisitingEntity, Is.Null);
            Assert.That(mind.Comp.OwnedEntity, Is.EqualTo(ghost));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that ghosts can become admin ghosts without issue
    /// 1. Become a ghost
    /// 2. visit an admin ghost
    /// 3. original ghost is deleted, player is an admin ghost.
    /// </summary>
    [Test]
    public async Task TestGhostToAghost()
    {
        await using var pair = await SetupPair();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        var player = playerMan.ServerSessions.Single();

        var ghost = await BecomeGhost(pair);

        // Player is a normal ghost (not admin ghost).
        Assert.That(entMan.GetComponent<MetaDataComponent>(player.AttachedEntity!.Value).EntityPrototype?.ID, Is.Not.EqualTo("AdminObserver"));

        // Try to become an admin ghost
        await server.WaitAssertion(() => serverConsole.ExecuteCommand(player, "aghost"));
        await pair.RunTicksSync(5);

        Assert.That(entMan.Deleted(ghost), "old ghost was not deleted");
        Assert.Multiple(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(ghost), "Player is still attached to the old ghost");
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity), "Player did not become a new ghost");
            Assert.That(entMan.GetComponent<MetaDataComponent>(player.AttachedEntity!.Value).EntityPrototype?.ID, Is.EqualTo("AdminObserver"));
        });

        var mindId = player.ContentData()?.Mind;
        Assert.That(mindId, Is.Not.Null);

        var mind = entMan.GetComponent<MindComponent>(mindId.Value);
        Assert.That(mind.VisitingEntity, Is.Null);

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test ghost getting deleted while player is connected spawns another ghost
    /// 1. become ghost
    /// 2. delete ghost
    /// 3. new ghost is spawned
    /// </summary>
    [Test]
    public async Task TestGhostDeletedSpawnsNewGhost()
    {
        // Client is needed to spawn session
        await using var pair = await SetupPair();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        var player = playerMan.ServerSessions.Single();

        EntityUid ghost = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            entMan.DeleteEntity(player.AttachedEntity!.Value);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            // Is player a ghost?
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            ghost = player.AttachedEntity!.Value;
            Assert.That(entMan.HasComponent<GhostComponent>(ghost));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
#pragma warning disable NUnit2045 // Interdependent assertions.
            Assert.That(entMan.Deleted(ghost));
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity!.Value));
#pragma warning restore NUnit2045
        });

        await pair.CleanReturnAsync();
    }
}
