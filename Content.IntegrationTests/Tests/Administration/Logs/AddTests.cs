#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class AddTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [Test]
    public async Task AddAndGetSingleLog()
    {
        var pair = Pair;
        var server = pair.Server;
        var sEntities = server.ResolveDependency<IEntityManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await pair.CreateTestMap();
        var coordinates = pair.TestMap!.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {guid}",
                new { entity = (int) entity });
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = sAdminLogSystem.CurrentRoundJson(new LogFilter
            {
                Search = guid.ToString()
            });

            await foreach (var json in logs)
            {
                var root = json.RootElement;

                Assert.That(root.TryGetProperty("entity", out _), Is.True);

                json.Dispose();

                return true;
            }

            return false;
        });
    }

    [Test]
    public async Task AddAndGetUnformattedLog()
    {
        var pair = Pair;
        var server = pair.Server;

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}",
                new { entity = (int) entity, guid = guid });
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            log = logs.First();
            return true;
        });

        var filter = new LogFilter
        {
            Round = sGamerTicker.RoundId,
            Search = log.Message,
            Types = new HashSet<LogType> { log.Type },
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.Multiple(() =>
            {
                Assert.That(root.TryGetProperty("entity", out _), Is.True);
                Assert.That(root.TryGetProperty("guid", out _), Is.True);
            });

            json.Dispose();
        }
    }

    [Test]
    [TestCase(500)]
    public async Task BulkAddLogs(int amount)
    {
        var pair = Pair;
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            for (var i = 0; i < amount; i++)
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
            }
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var messages = await sAdminLogSystem.CurrentRoundLogs();
            return messages.Count >= amount;
        });
    }

    [Test]
    public async Task AddPlayerSessionLog()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        Guid playerGuid = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.First();
            playerGuid = player.UserId;

            Assert.DoesNotThrow(() =>
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{player:Player} test log.",
                    players: new Guid[] { player.UserId });
            });
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var filter = new LogFilter { Types = new HashSet<LogType> { LogType.Unknown } };
            var logs = await sAdminLogSystem.CurrentRoundLogs(filter);
            // Find the specific log that has our player
            var match = logs.FirstOrDefault(l => l.Players.Contains(playerGuid));
            return match.Id != 0;
        });
    }

    [Test]
    public async Task DuplicatePlayerDoesNotThrowTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player} {player} test log: {guid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            return true;
        });
    }

    [Test]
    public async Task DuplicatePlayerIdDoesNotThrowTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player:first} {player:second} test log: {guid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            return true;
        });
    }

    [Test]
    public async Task ActorVictimToolHelperPreservesSelfActionEntityRoles()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        AdminLogExplicitSemantics semantics = default;
        Guid playerGuid = default;
        EntityUid actor = default;
        EntityUid tool = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            actor = player.AttachedEntity!.Value;
            tool = sEntities.SpawnEntity(null, coordinates);
            playerGuid = player.UserId;

            semantics = AdminLogHelpers.GetActorVictimToolSemantics(sPlayers, actor, actor, tool);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Is.Not.Null);
            Assert.That(semantics.PlayerRoles, Is.Not.Null);
            Assert.That(semantics.Entities, Is.Not.Null);

            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(playerGuid));

            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![playerGuid], Is.EqualTo(AdminLogEntityRole.Actor));

            Assert.That(semantics.Entities, Has.Count.EqualTo(3));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == tool && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ActorVictimToolHelperBuildsExpectedNonSelfSemantics()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        AdminLogExplicitSemantics semantics = default;
        Guid playerGuid = default;
        EntityUid actor = default;
        EntityUid victim = default;
        EntityUid tool = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            actor = player.AttachedEntity!.Value;
            victim = sEntities.SpawnEntity(null, coordinates);
            tool = sEntities.SpawnEntity(null, coordinates);
            playerGuid = player.UserId;

            semantics = AdminLogHelpers.GetActorVictimToolSemantics(sPlayers, actor, victim, tool);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Is.Not.Null);
            Assert.That(semantics.PlayerRoles, Is.Not.Null);
            Assert.That(semantics.Entities, Is.Not.Null);

            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(playerGuid));

            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![playerGuid], Is.EqualTo(AdminLogEntityRole.Actor));

            Assert.That(semantics.Entities, Has.Count.EqualTo(3));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == victim && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == tool && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SelfActionExplicitSemanticsPreserveActorAndVictimEntities()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        Guid playerGuid = default;
        int actorUid = default;
        int toolUid = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            var actor = player.AttachedEntity!.Value;
            var tool = sEntities.SpawnEntity(null, coordinates);
            var semantics = AdminLogHelpers.GetActorVictimToolSemantics(sPlayers, actor, actor, tool);

            playerGuid = player.UserId;
            actorUid = (int) actor;
            toolUid = (int) tool;

            sAdminLogSystem.Add(
                LogType.Ingestion,
                $"{actor:actor} performed a self-action with {tool:tool} {guid}",
                payload: new { guid },
                players: semantics.Players,
                entities: semantics.Entities,
                playerRoles: semantics.PlayerRoles);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter { Search = guid.ToString() });
            var match = logs.FirstOrDefault();
            if (match.Id == 0)
                return false;

            log = match;
            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(log.Players, Has.Length.EqualTo(1));
            Assert.That(log.Players.Single(), Is.EqualTo(playerGuid));

            Assert.That(log.Entities.Count(e => e.EntityUid == actorUid && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(log.Entities.Count(e => e.EntityUid == actorUid && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(log.Entities.Count(e => e.EntityUid == toolUid && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var victimLogs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString(),
                AnyEntities = [actorUid],
                EntityRoles = new HashSet<AdminLogEntityRole> { AdminLogEntityRole.Victim }
            });

            return victimLogs.Any(l => l.Id == log.Id);
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var actorLogs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString(),
                AnyEntities = [actorUid],
                EntityRoles = new HashSet<AdminLogEntityRole> { AdminLogEntityRole.Actor }
            });

            return actorLogs.Any(l => l.Id == log.Id);
        });
    }

    [Test]
    public async Task ExplicitEntitiesSuppressAutoDetectedRolesByUid()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        int actorUid = default;
        int toolUid = default;

        await server.WaitPost(() =>
        {
            var actor = sPlayers.Sessions.Single().AttachedEntity!.Value;
            var tool = sEntities.SpawnEntity(null, coordinates);

            actorUid = (int) actor;
            toolUid = (int) tool;

            sAdminLogSystem.Add(
                LogType.Unknown,
                $"{actor:actor} used {tool:tool} {guid}",
                entities:
                [
                    new AdminLogEntityRef(actor, AdminLogEntityRole.Victim),
                    new AdminLogEntityRef(tool, AdminLogEntityRole.Subject),
                ]);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter { Search = guid.ToString() });
            var match = logs.FirstOrDefault();
            if (match.Id == 0)
                return false;

            log = match;
            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(log.Entities.Count(e => e.EntityUid == actorUid && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(log.Entities.Count(e => e.EntityUid == actorUid && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(0));
            Assert.That(log.Entities.Count(e => e.EntityUid == toolUid && e.Role == AdminLogEntityRole.Subject), Is.EqualTo(1));
            Assert.That(log.Entities.Count(e => e.EntityUid == toolUid && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ExplicitPlayersMergeWithAutoDetectedPlayersWithoutDuplicates()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        Guid playerGuid = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            var actor = player.AttachedEntity!.Value;
            playerGuid = player.UserId;

            sAdminLogSystem.Add(
                LogType.Unknown,
                $"{actor:actor} test log {guid}",
                players: [playerGuid]);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter { Search = guid.ToString() });
            var match = logs.FirstOrDefault();
            if (match.Id == 0)
                return false;

            log = match;
            return true;
        });

        Assert.That(log.Players, Is.EqualTo(new[] { playerGuid }));
    }

    [Test]
    public async Task SessionSemanticsIncludePlayerAndOptionalAttachedEntity()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        AdminLogExplicitSemantics semantics = default;
        Guid playerGuid = default;
        EntityUid attached = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            playerGuid = player.UserId;
            attached = player.AttachedEntity!.Value;
            semantics = AdminLogHelpers.GetSessionSemantics(player, AdminLogEntityRole.Actor, AdminLogEntityRole.Actor);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(playerGuid));
            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![playerGuid], Is.EqualTo(AdminLogEntityRole.Actor));
            Assert.That(semantics.Entities, Has.Count.EqualTo(1));
            Assert.That(semantics.Entities!.Single().Entity, Is.EqualTo(attached));
            Assert.That(semantics.Entities!.Single().Role, Is.EqualTo(AdminLogEntityRole.Actor));
        });
    }

    [Test]
    public async Task SessionSemanticsCanTrackPlayerWithoutExplicitPlayerRole()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        AdminLogExplicitSemantics semantics = default;
        Guid playerGuid = default;
        EntityUid attached = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            playerGuid = player.UserId;
            attached = player.AttachedEntity!.Value;
            semantics = AdminLogHelpers.GetSessionSemantics(player, attachedEntityRole: AdminLogEntityRole.Victim);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(playerGuid));
            Assert.That(semantics.PlayerRoles, Is.Null);
            Assert.That(semantics.Entities, Has.Count.EqualTo(1));
            Assert.That(semantics.Entities!.Single().Entity, Is.EqualTo(attached));
            Assert.That(semantics.Entities!.Single().Role, Is.EqualTo(AdminLogEntityRole.Victim));
        });
    }

    [Test]
    public async Task ActorSubjectVictimSemanticsPreserveSubjectAndOptionalTool()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        AdminLogExplicitSemantics semantics = default;
        Guid playerGuid = default;
        EntityUid actor = default;
        EntityUid subject = default;
        EntityUid victim = default;
        EntityUid tool = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            actor = player.AttachedEntity!.Value;
            subject = sEntities.SpawnEntity(null, coordinates);
            victim = sEntities.SpawnEntity(null, coordinates);
            tool = sEntities.SpawnEntity(null, coordinates);
            playerGuid = player.UserId;

            semantics = AdminLogHelpers.GetActorSubjectVictimSemantics(sPlayers, actor, subject, victim, tool);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(playerGuid));
            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![playerGuid], Is.EqualTo(AdminLogEntityRole.Actor));
            Assert.That(semantics.Entities, Has.Count.EqualTo(4));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == subject && e.Role == AdminLogEntityRole.Subject), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == victim && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == tool && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ActorSubjectVictimSemanticsPersistQueryableSubjectRole()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        int projectileUid = default;

        await server.WaitPost(() =>
        {
            var actor = sPlayers.Sessions.Single().AttachedEntity!.Value;
            var projectile = sEntities.SpawnEntity(null, coordinates);
            var victim = sEntities.SpawnEntity(null, coordinates);
            var tool = sEntities.SpawnEntity(null, coordinates);
            var semantics = AdminLogHelpers.GetActorSubjectVictimSemantics(sPlayers, actor, projectile, victim, tool);
            projectileUid = (int) projectile;

            sAdminLogSystem.Add(
                LogType.BulletHit,
                $"{actor:actor} shot {projectile:subject} into {victim:victim} {guid}",
                payload: new { guid },
                players: semantics.Players,
                entities: semantics.Entities,
                playerRoles: semantics.PlayerRoles);
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var subjectLogs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString(),
                AnyEntities = [projectileUid],
                EntityRoles = new HashSet<AdminLogEntityRole> { AdminLogEntityRole.Subject }
            });

            return subjectLogs.Count == 1;
        });
    }

    [Test]
    public async Task ActorVictimsToolSemanticsPreserveEveryVictimAndDistinctPlayers()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        AdminLogExplicitSemantics semantics = default;
        Guid actorGuid = default;
        EntityUid actor = default;
        EntityUid victimOne = default;
        EntityUid victimTwo = default;
        EntityUid tool = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            actor = player.AttachedEntity!.Value;
            actorGuid = player.UserId;
            victimOne = sEntities.SpawnEntity(null, coordinates);
            victimTwo = sEntities.SpawnEntity(null, coordinates);
            tool = sEntities.SpawnEntity(null, coordinates);

            semantics = AdminLogHelpers.GetActorVictimsToolSemantics(sPlayers, actor, [victimOne, victimTwo], tool);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(actorGuid));
            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![actorGuid], Is.EqualTo(AdminLogEntityRole.Actor));
            Assert.That(semantics.Entities, Has.Count.EqualTo(4));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == tool && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == victimOne && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == victimTwo && e.Role == AdminLogEntityRole.Victim), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ActorVictimsSemanticsPersistQueryableVictimRoleForMultipleTargets()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        int victimOneUid = default;
        int victimTwoUid = default;

        await server.WaitPost(() =>
        {
            var actor = sPlayers.Sessions.Single().AttachedEntity!.Value;
            var victimOne = sEntities.SpawnEntity(null, coordinates);
            var victimTwo = sEntities.SpawnEntity(null, coordinates);
            var tool = sEntities.SpawnEntity(null, coordinates);
            var semantics = AdminLogHelpers.GetActorVictimsToolSemantics(sPlayers, actor, [victimOne, victimTwo], tool);
            victimOneUid = (int) victimOne;
            victimTwoUid = (int) victimTwo;

            sAdminLogSystem.Add(
                LogType.MeleeHit,
                $"{actor:actor} hit 2 targets with {tool:tool} {guid}",
                payload: new { guid },
                players: semantics.Players,
                entities: semantics.Entities,
                playerRoles: semantics.PlayerRoles);
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var victimLogs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString(),
                AnyEntities = [victimOneUid, victimTwoUid],
                EntityRoles = new HashSet<AdminLogEntityRole> { AdminLogEntityRole.Victim }
            });

            if (victimLogs.Count != 1)
                return false;

            var log = victimLogs.Single();
            return log.Entities.Count(e => e.EntityUid == victimOneUid && e.Role == AdminLogEntityRole.Victim) == 1
                && log.Entities.Count(e => e.EntityUid == victimTwoUid && e.Role == AdminLogEntityRole.Victim) == 1;
        });
    }

    [Test]
    public async Task ActorToolSemanticsPreserveActorToolAndActorPlayer()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();

        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        AdminLogExplicitSemantics semantics = default;
        Guid actorGuid = default;
        EntityUid actor = default;
        EntityUid tool = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();
            actor = player.AttachedEntity!.Value;
            actorGuid = player.UserId;
            tool = sEntities.SpawnEntity(null, coordinates);

            semantics = AdminLogHelpers.GetActorSemantics(sPlayers, actor, tool);
        });

        Assert.Multiple(() =>
        {
            Assert.That(semantics.Players, Has.Count.EqualTo(1));
            Assert.That(semantics.Players!.Single(), Is.EqualTo(actorGuid));
            Assert.That(semantics.PlayerRoles, Has.Count.EqualTo(1));
            Assert.That(semantics.PlayerRoles![actorGuid], Is.EqualTo(AdminLogEntityRole.Actor));
            Assert.That(semantics.Entities, Has.Count.EqualTo(2));
            Assert.That(semantics.Entities!.Count(e => e.Entity == actor && e.Role == AdminLogEntityRole.Actor), Is.EqualTo(1));
            Assert.That(semantics.Entities!.Count(e => e.Entity == tool && e.Role == AdminLogEntityRole.Tool), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ActorToolSemanticsPersistQueryableToolRole()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        int toolUid = default;

        await server.WaitPost(() =>
        {
            var actor = sPlayers.Sessions.Single().AttachedEntity!.Value;
            var tool = sEntities.SpawnEntity(null, coordinates);
            var semantics = AdminLogHelpers.GetActorSemantics(sPlayers, actor, tool);
            toolUid = (int) tool;

            sAdminLogSystem.Add(
                LogType.Explosion,
                $"{actor:actor} triggered {tool:tool} {guid}",
                payload: new { guid },
                players: semantics.Players,
                entities: semantics.Entities,
                playerRoles: semantics.PlayerRoles);
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var toolLogs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString(),
                AnyEntities = [toolUid],
                EntityRoles = new HashSet<AdminLogEntityRole> { AdminLogEntityRole.Tool }
            });

            return toolLogs.Count == 1;
        });
    }
}
