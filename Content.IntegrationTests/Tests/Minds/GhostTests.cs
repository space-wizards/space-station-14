using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players;
using NUnit.Framework;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostTests
{
    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGhostOnDelete()
    {
        // Client is needed to spawn session
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        IPlayerSession player = playerMan.ServerSessions.Single();

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            entMan.DeleteEntity(player.AttachedEntity!.Value);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            // Is player a ghost?
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            var entity = player.AttachedEntity!.Value;
            Assert.That(entMan.HasComponent<GhostComponent>(entity));
        });

        await pairTracker.CleanReturnAsync();
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
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var gameTicker = entMan.EntitySysManager.GetEntitySystem<GameTicker>();
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

        IPlayerSession player = playerMan.ServerSessions.Single();

        EntityUid originalEntity = default!;
        EntityUid ghost = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            originalEntity = player.AttachedEntity!.Value;

            Assert.That(mindSystem.TryGetMind(player.UserId, out var mind), "could not find mind");
            ghost = entMan.SpawnEntity("MobObserver", MapCoordinates.Nullspace);
            mindSystem.Visit(mind, ghost);

            Assert.That(player.AttachedEntity, Is.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity), "player is not a ghost");
            Assert.That(mind.VisitingEntity, Is.EqualTo(player.AttachedEntity));
            Assert.That(mind.OwnedEntity, Is.EqualTo(originalEntity));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);
        await server.WaitAssertion(() => entMan.DeleteEntity(originalEntity));
        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            // Is player a ghost?
            Assert.That(!entMan.Deleted(ghost), "ghost has been deleted");
            Assert.That(player.AttachedEntity, Is.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity));

            Assert.That(mindSystem.TryGetMind(player.UserId, out var mind),  "could not find mind");
            Assert.That(mind.UserId, Is.EqualTo(player.UserId));
            Assert.That(mind.Session, Is.EqualTo(player));
            Assert.IsNull(mind.VisitingEntity);
            Assert.That(mind.OwnedEntity, Is.EqualTo(ghost));
        });

        await pairTracker.CleanReturnAsync();
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
        // Client is needed to spawn session
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        IPlayerSession player = playerMan.ServerSessions.Single();

        EntityUid ghost = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            entMan.DeleteEntity(player.AttachedEntity!.Value);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            // Is player a ghost?
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            ghost = player.AttachedEntity!.Value;
            Assert.That(entMan.HasComponent<GhostComponent>(ghost));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(ghost));
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity!.Value));

            var mind = player.ContentData()?.Mind;
            Assert.NotNull(mind);
            Assert.Null(mind.VisitingEntity);
        });

        await pairTracker.CleanReturnAsync();
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
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        IPlayerSession player = playerMan.ServerSessions.Single();

        EntityUid ghost = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            entMan.DeleteEntity(player.AttachedEntity!.Value);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            // Is player a ghost?
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(null));
            ghost = player.AttachedEntity!.Value;
            Assert.That(entMan.HasComponent<GhostComponent>(ghost));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(ghost));
            Assert.That(player.AttachedEntity, Is.Not.EqualTo(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(player.AttachedEntity!.Value));
        });

        await pairTracker.CleanReturnAsync();
    }
}
