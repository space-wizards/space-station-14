using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mind;
using Content.Server.SpaceArena;
using Content.Server.SpaceArena.Components;
using Content.Server.Station.Systems;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction.Components;
using Content.Shared.SpaceArena;
using Content.Shared.SpaceArena.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.SpaceArena;

[TestFixture]
public sealed class SpaceArenaMatchTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        Dirty = true,
    };

    [Test]
    public async Task CreatesConfiguredMatchWithoutLoadingArena()
    {
        var server = Pair.Server;
        var system = server.System<SpaceArenaMatchSystem>();
        var match = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            Assert.That(system.TryCreateMatch("SpaceArenaDeathMatch", "SpaceArenaIce", out match), Is.True);
            Assert.That(server.EntMan.EntityExists(match), Is.True);
            Assert.That(server.EntMan.HasComponent<SpaceArenaMatchRuntimeComponent>(match), Is.True);

            var component = server.EntMan.GetComponent<SpaceArenaMatchComponent>(match);
            Assert.That(component.State, Is.EqualTo(SpaceArenaMatchState.Waiting));
            Assert.That(component.Arena?.Id, Is.EqualTo("SpaceArenaIce"));
            Assert.That(component.PlayerCount, Is.Zero);

            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(match);
            Assert.That(runtime.Map, Is.EqualTo(MapId.Nullspace));

            Assert.That(system.TryCreateMatch("SpaceArenaDeathMatch", "Saltern", out _), Is.False);
            server.EntMan.QueueDeleteEntity(match);
        });

        await Pair.RunTicksSync(1);
        Assert.That(server.EntMan.EntityExists(match), Is.False);
    }

    [Test]
    public async Task CanvasFreeplayHasNoTimerAndCleansUpAfterPlayerLeaves()
    {
        var pair = Pair;
        var server = pair.Server;
        var hubMap = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(1);
        await pair.RunTicksSync(5);
        var mapSystem = server.System<MapSystem>();
        var matchSystem = server.System<SpaceArenaMatchSystem>();
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var stationSystem = server.System<StationSystem>();
        var activityMap = MapId.Nullspace;
        var lobby = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            var station = server.EntMan.SpawnEntity("SpaceArenaHubStation", MapCoordinates.Nullspace);
            stationSystem.AddGridToStation(station, hubMap.Grid.Owner);
            server.EntMan.SpawnEntity("SpaceArenaSpawnPoint", hubMap.GridCoords);
            server.EntMan.SpawnEntity("SpawnPointPassenger", hubMap.GridCoords);

            var session = sessions[0];
            var body = server.EntMan.SpawnEntity(null, hubMap.GridCoords);
            Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
            mindSystem.TransferTo(mind, body, mind: mind.Comp);
            server.PlayerMan.SetAttachedEntity(session, body);

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDrawing", "SpaceArenaCanvas", session, out lobby),
                Is.True);
            Assert.That(lobbySystem.TryStartLobby(lobby, session), Is.True);

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);
            activityMap = runtime.Map;
            Assert.That(activityMap, Is.Not.EqualTo(MapId.Nullspace));
            Assert.That(match.TimeLimit, Is.Null);
            Assert.That(session.AttachedEntity, Is.Not.Null);

        });

        await pair.RunTicksSync(3);
        await server.WaitPost(() =>
        {
            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Active));
            Assert.That(match.StateEndsAt, Is.Null);
            Assert.That(matchSystem.TryLeaveMatch(sessions[0]), Is.True);
            Assert.That(matchSystem.TryGetPlayerMatch(sessions[0].UserId, out _), Is.False);
        });

        await pair.RunTicksSync(5);
        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.EntityExists(lobby), Is.False);
            Assert.That(mapSystem.MapExists(activityMap), Is.False);
        });
    }

    [Test]
    public async Task CanvasFreeplayCleansUpWhenParticipantDisconnects()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var hubMap = await pair.CreateTestMap();
        await pair.RunTicksSync(5);
        var clientNet = client.ResolveDependency<IClientNetManager>();
        var mapSystem = server.System<MapSystem>();
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var stationSystem = server.System<StationSystem>();
        var participant = pair.Player;
        var lobby = EntityUid.Invalid;
        var activityMap = MapId.Nullspace;

        Assert.That(participant, Is.Not.Null);
        var username = participant!.Name;

        await server.WaitPost(() =>
        {
            var station = server.EntMan.SpawnEntity("SpaceArenaHubStation", MapCoordinates.Nullspace);
            stationSystem.AddGridToStation(station, hubMap.Grid.Owner);
            server.EntMan.SpawnEntity("SpaceArenaSpawnPoint", hubMap.GridCoords);
            server.EntMan.SpawnEntity("SpawnPointPassenger", hubMap.GridCoords);

            var body = server.EntMan.SpawnEntity(null, hubMap.GridCoords);
            Entity<MindComponent> mind = mindSystem.CreateMind(participant.UserId, participant.Name);
            mindSystem.TransferTo(mind, body, mind: mind.Comp);
            server.PlayerMan.SetAttachedEntity(participant, body);

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDrawing", "SpaceArenaCanvas", participant, out lobby),
                Is.True);
            Assert.That(lobbySystem.TryStartLobby(lobby, participant), Is.True);
            activityMap = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby).Map;
        });

        await pair.RunTicksSync(3);
        await client.WaitPost(() => clientNet.ClientDisconnect("SpaceArena drawing disconnect test."));
        await pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.EntityExists(lobby), Is.False);
            Assert.That(mapSystem.MapExists(activityMap), Is.False);
        });

        client.SetConnectTarget(server);
        await client.WaitPost(() => clientNet.ClientConnect(null!, 0, username));
        await pair.RunTicksSync(5);
    }

    [Test]
    public async Task SpectatorCanLeaveAndReturnsAutomaticallyWhenMatchEnds()
    {
        var pair = Pair;
        var server = pair.Server;
        var hubMap = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(3);
        await pair.RunTicksSync(5);
        var matchSystem = server.System<SpaceArenaMatchSystem>();
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var stationSystem = server.System<StationSystem>();
        var lobby = EntityUid.Invalid;
        var oldSpectatorBody = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            var station = server.EntMan.SpawnEntity("SpaceArenaHubStation", MapCoordinates.Nullspace);
            stationSystem.AddGridToStation(station, hubMap.Grid.Owner);
            server.EntMan.SpawnEntity("SpaceArenaSpawnPoint", hubMap.GridCoords);
            server.EntMan.SpawnEntity("SpawnPointPassenger", hubMap.GridCoords);

            for (var i = 0; i < sessions.Length; i++)
            {
                var session = sessions[i];
                var body = server.EntMan.SpawnEntity(null, hubMap.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
                if (i == 2)
                    oldSpectatorBody = body;
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaIce", sessions[0], out lobby),
                Is.True);
            Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[1]), Is.True);
            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);
            Assert.That(matchSystem.TrySpectateMatch(lobby, sessions[2]), Is.True);

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);
            var ghost = sessions[2].AttachedEntity;
            Assert.Multiple(() =>
            {
                Assert.That(match.PlayerCount, Is.EqualTo(2));
                Assert.That(runtime.Spectators, Has.Count.EqualTo(1));
                Assert.That(ghost, Is.Not.Null);
                Assert.That(server.EntMan.HasComponent<GhostComponent>(ghost!.Value), Is.True);
                Assert.That(server.EntMan.HasComponent<SpaceArenaSpectatorComponent>(ghost.Value), Is.True);
                Assert.That(server.EntMan.GetComponent<TransformComponent>(ghost.Value).MapID, Is.EqualTo(runtime.Map));
                Assert.That(server.EntMan.IsQueuedForDeletion(oldSpectatorBody), Is.True);
                Assert.That(matchSystem.TryGetSpectatedMatch(sessions[2].UserId, out var spectated), Is.True);
                Assert.That(spectated, Is.EqualTo(lobby));
            });

            Assert.That(matchSystem.TrySpectateMatch(lobby, sessions[2]), Is.False);

            Assert.That(matchSystem.TryLeaveSpectating(sessions[2]), Is.True);
            var returnedBody = sessions[2].AttachedEntity;
            Assert.Multiple(() =>
            {
                Assert.That(returnedBody, Is.Not.Null);
                Assert.That(server.EntMan.HasComponent<GhostComponent>(returnedBody!.Value), Is.False);
                Assert.That(
                    server.EntMan.GetComponent<TransformComponent>(returnedBody.Value).MapID,
                    Is.EqualTo(hubMap.MapId));
                Assert.That(runtime.Spectators, Is.Empty);
                Assert.That(matchSystem.TryGetSpectatedMatch(sessions[2].UserId, out _), Is.False);
            });

            Assert.That(matchSystem.TrySpectateMatch(lobby, sessions[2]), Is.True);
            Assert.That(matchSystem.FinishMatch(lobby), Is.True);
            var automaticallyReturned = sessions[2].AttachedEntity;
            Assert.Multiple(() =>
            {
                Assert.That(automaticallyReturned, Is.Not.Null);
                Assert.That(server.EntMan.HasComponent<GhostComponent>(automaticallyReturned!.Value), Is.False);
                Assert.That(
                    server.EntMan.GetComponent<TransformComponent>(automaticallyReturned.Value).MapID,
                    Is.EqualTo(hubMap.MapId));
                Assert.That(runtime.Spectators, Is.Empty);
                Assert.That(matchSystem.TryGetSpectatedMatch(sessions[2].UserId, out _), Is.False);
            });

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
    }

    [Test]
    public async Task PlayerLobbyEnforcesHostAndCleansUpEmptyRoom()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(4);
        await pair.RunTicksSync(5);
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var lobby = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            foreach (var session in sessions)
            {
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaIce", sessions[0], out lobby),
                Is.True);
            Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[1]), Is.True);
            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[1]), Is.False, "A non-host started the lobby.");

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            Assert.That(match.PlayerCount, Is.EqualTo(2));
            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Waiting));
            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);
            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Preparing));
            server.EntMan.QueueDeleteEntity(lobby);

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaIce", sessions[2], out lobby),
                Is.True);
            Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[3]), Is.True);

            Assert.That(lobbySystem.TryLeaveLobby(sessions[2]), Is.True);
            var playerLobby = server.EntMan.GetComponent<SpaceArenaPlayerLobbyComponent>(lobby);
            Assert.That(playerLobby.Host, Is.EqualTo(sessions[3].UserId));
            var secondMatch = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            Assert.That(secondMatch.PlayerCount, Is.EqualTo(1));

            Assert.That(lobbySystem.TryLeaveLobby(sessions[3]), Is.True);
            Assert.That(server.EntMan.IsQueuedForDeletion(lobby), Is.True);
        });

        await pair.RunTicksSync(1);
        Assert.That(server.EntMan.EntityExists(lobby), Is.False);
    }

    [Test]
    public async Task MatchReplacesHubBodyAndCreatesFreshBodyOnReturn()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(2);
        await pair.RunTicksSync(5);
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var matchSystem = server.System<SpaceArenaMatchSystem>();
        var stationSystem = server.System<StationSystem>();
        var oldHubBodies = new EntityUid[sessions.Length];
        var matchBodies = new EntityUid[sessions.Length];
        var station = EntityUid.Invalid;
        var lobby = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            station = server.EntMan.SpawnEntity("SpaceArenaHubStation", MapCoordinates.Nullspace);
            stationSystem.AddGridToStation(station, map.Grid.Owner);
            server.EntMan.SpawnEntity("SpaceArenaSpawnPoint", map.GridCoords);
            server.EntMan.SpawnEntity("SpawnPointPassenger", map.GridCoords);

            for (var i = 0; i < sessions.Length; i++)
            {
                var session = sessions[i];
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
                oldHubBodies[i] = body;
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaIce", sessions[0], out lobby),
                Is.True);
            Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[1]), Is.True);
            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);

            var deathMatch = server.EntMan.GetComponent<SpaceArenaDeathMatchComponent>(lobby);
            for (var i = 0; i < sessions.Length; i++)
            {
                Assert.That(server.EntMan.IsQueuedForDeletion(oldHubBodies[i]), Is.True);
                Assert.That(sessions[i].AttachedEntity, Is.Not.Null);
                matchBodies[i] = sessions[i].AttachedEntity!.Value;
                Assert.That(matchBodies[i], Is.Not.EqualTo(oldHubBodies[i]));
                Assert.That(server.EntMan.HasComponent<BlockMovementComponent>(matchBodies[i]), Is.False);
                Assert.That(deathMatch.PlayerLoadouts[sessions[i].UserId].Id, Is.EqualTo("ArenaLoadoutWinter"));

            }

            Assert.That(matchSystem.TryLeaveMatch(sessions[0]), Is.False);
        });

        await pair.RunTicksSync(1);
        await server.WaitPost(() =>
        {
            foreach (var body in oldHubBodies)
                Assert.That(server.EntMan.EntityExists(body), Is.False);

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
        await server.WaitPost(() =>
        {
            for (var i = 0; i < sessions.Length; i++)
            {
                Assert.That(sessions[i].AttachedEntity, Is.Not.Null);
                var returnedBody = sessions[i].AttachedEntity!.Value;
                Assert.That(returnedBody, Is.Not.EqualTo(matchBodies[i]));
                Assert.That(server.EntMan.EntityExists(returnedBody), Is.True);
                Assert.That(server.EntMan.GetComponent<TransformComponent>(returnedBody).MapID, Is.EqualTo(map.MapId));
                Assert.That(stationSystem.GetOwningStation(returnedBody), Is.EqualTo(station));
            }
        });
    }

    [Test]
    public async Task TeamArenaAssignsLegacyLoadoutCompositionToEachTeam()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(6);
        await pair.RunTicksSync(5);
        var mindSystem = server.System<MindSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var lobby = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            foreach (var session in sessions)
            {
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaKnights3v3", sessions[0], out lobby),
                Is.True);
            for (var i = 1; i < sessions.Length; i++)
                Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[i]), Is.True);

            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);

            var deathMatch = server.EntMan.GetComponent<SpaceArenaDeathMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);
            var teamAKnights = 0;
            var teamAArchers = 0;
            var teamBKnights = 0;
            var teamBArchers = 0;

            foreach (var (player, data) in runtime.Players)
            {
                var loadout = deathMatch.PlayerLoadouts[player].Id;
                if (data.SpawnGroup == SpaceArenaSpawnGroups.TeamA)
                {
                    teamAKnights += loadout == "ArenaLoadoutKnight" ? 1 : 0;
                    teamAArchers += loadout == "ArenaLoadoutArcher" ? 1 : 0;
                }
                else if (data.SpawnGroup == SpaceArenaSpawnGroups.TeamB)
                {
                    teamBKnights += loadout == "ArenaLoadoutKnight" ? 1 : 0;
                    teamBArchers += loadout == "ArenaLoadoutArcher" ? 1 : 0;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(teamAKnights, Is.EqualTo(1));
                Assert.That(teamAArchers, Is.EqualTo(2));
                Assert.That(teamBKnights, Is.EqualTo(1));
                Assert.That(teamBArchers, Is.EqualTo(2));
            });

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
    }

    [Test]
    public async Task TeamEliminationEndsMatchWhenLastFighterIsCriticalOrDeleted()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(4);
        await pair.RunTicksSync(5);
        var mindSystem = server.System<MindSystem>();
        var mobState = server.System<MobStateSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var lobby = EntityUid.Invalid;
        List<EntityUid> teamA = [];
        List<EntityUid> teamB = [];

        await server.WaitPost(() =>
        {
            foreach (var session in sessions)
            {
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaLava2v2", sessions[0], out lobby),
                Is.True);
            for (var i = 1; i < sessions.Length; i++)
                Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[i]), Is.True);

            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);

            foreach (var data in runtime.Players.Values)
            {
                if (data.MatchEntity is not { } body)
                    continue;

                if (data.SpawnGroup == SpaceArenaSpawnGroups.TeamA)
                    teamA.Add(body);
                else if (data.SpawnGroup == SpaceArenaSpawnGroups.TeamB)
                    teamB.Add(body);
            }

            Assert.That(teamA, Has.Count.EqualTo(2));
            Assert.That(teamB, Has.Count.EqualTo(2));
            Assert.That(match.RespawnDelay, Is.Null);

            match.State = SpaceArenaMatchState.Active;
            mobState.ChangeMobState(teamB[0], MobState.Critical);
            Assert.That(
                match.State,
                Is.EqualTo(SpaceArenaMatchState.Active),
                "One remaining teammate should keep the match active.");

            server.EntMan.QueueDeleteEntity(teamB[1]);
        });

        await pair.RunTicksSync(1);
        await server.WaitPost(() =>
        {
            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var deathMatch = server.EntMan.GetComponent<SpaceArenaDeathMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);

            Assert.Multiple(() =>
            {
                Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Ending));
                Assert.That(deathMatch.WinningGroup, Is.EqualTo(SpaceArenaSpawnGroups.TeamA));
                Assert.That(deathMatch.ResultAnnounced, Is.True);
                Assert.That(runtime.Respawns, Is.Empty);
                Assert.That(runtime.NextRespawn, Is.Null);
            });

            foreach (var winner in teamA)
            {
                var light = server.EntMan.GetComponent<PointLightComponent>(winner);
                Assert.Multiple(() =>
                {
                    Assert.That(light.Enabled, Is.True);
                    Assert.That(light.Color, Is.EqualTo(Color.Gold));
                    Assert.That(light.Radius, Is.EqualTo(5f));
                    Assert.That(light.Energy, Is.EqualTo(4f));
                });
            }

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
    }

    [Test]
    public async Task TeamEliminationBeforeActiveEndsMatchOnStart()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(2);
        await pair.RunTicksSync(5);
        var mindSystem = server.System<MindSystem>();
        var mobState = server.System<MobStateSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var lobby = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            foreach (var session in sessions)
            {
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaLava2v2", sessions[0], out lobby),
                Is.True);
            Assert.That(lobbySystem.TryJoinLobby(lobby, sessions[1]), Is.True);

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            match.PreparationDuration = TimeSpan.Zero;
            match.CountdownDuration = TimeSpan.Zero;
            Assert.That(lobbySystem.TryStartLobby(lobby, sessions[0]), Is.True);

            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);
            EntityUid victim = EntityUid.Invalid;
            foreach (var data in runtime.Players.Values)
            {
                if (data.SpawnGroup == SpaceArenaSpawnGroups.TeamB && data.MatchEntity is { } body)
                {
                    victim = body;
                    break;
                }
            }

            Assert.That(victim, Is.Not.EqualTo(EntityUid.Invalid));
            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Preparing));
            mobState.ChangeMobState(victim, MobState.Dead);
            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Preparing));
        });

        await pair.RunTicksSync(3);
        await server.WaitPost(() =>
        {
            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var deathMatch = server.EntMan.GetComponent<SpaceArenaDeathMatchComponent>(lobby);
            Assert.Multiple(() =>
            {
                Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Ending));
                Assert.That(deathMatch.WinningGroup, Is.EqualTo(SpaceArenaSpawnGroups.TeamA));
            });

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
    }

    [Test]
    public async Task DisconnectedPlayerForfeitsAfterGracePeriod()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var sessions = await server.AddDummySessions(3);
        await pair.RunTicksSync(5);
        var clientNet = client.ResolveDependency<IClientNetManager>();
        var mindSystem = server.System<MindSystem>();
        var mobState = server.System<MobStateSystem>();
        var lobbySystem = server.System<SpaceArenaLobbySystem>();
        var participant = pair.Player;
        var lobby = EntityUid.Invalid;
        var disconnectedBody = EntityUid.Invalid;
        var disconnectedGroup = string.Empty;

        Assert.That(participant, Is.Not.Null);
        var username = participant!.Name;

        await server.WaitPost(() =>
        {
            var playerBody = server.EntMan.SpawnEntity(null, map.GridCoords);
            Entity<MindComponent> playerMind = mindSystem.CreateMind(participant.UserId, participant.Name);
            mindSystem.TransferTo(playerMind, playerBody, mind: playerMind.Comp);
            server.PlayerMan.SetAttachedEntity(participant, playerBody);

            foreach (var session in sessions)
            {
                var body = server.EntMan.SpawnEntity(null, map.GridCoords);
                Entity<MindComponent> mind = mindSystem.CreateMind(session.UserId, session.Name);
                mindSystem.TransferTo(mind, body, mind: mind.Comp);
                server.PlayerMan.SetAttachedEntity(session, body);
            }

            Assert.That(
                lobbySystem.TryCreateLobby("SpaceArenaDeathMatch", "SpaceArenaLava2v2", participant, out lobby),
                Is.True);
            foreach (var session in sessions)
                Assert.That(lobbySystem.TryJoinLobby(lobby, session), Is.True);

            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            match.PreparationDuration = TimeSpan.Zero;
            match.CountdownDuration = TimeSpan.Zero;
            match.DisconnectGracePeriod = TimeSpan.Zero;
            Assert.That(lobbySystem.TryStartLobby(lobby, participant), Is.True);

            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);
            foreach (var (player, data) in runtime.Players)
            {
                if (player != participant.UserId || data.MatchEntity is not { } body)
                    continue;

                disconnectedBody = body;
                disconnectedGroup = data.SpawnGroup;
                break;
            }

            Assert.That(disconnectedBody, Is.Not.EqualTo(EntityUid.Invalid));
            foreach (var (player, data) in runtime.Players)
            {
                if (player != participant.UserId &&
                    data.SpawnGroup == disconnectedGroup &&
                    data.MatchEntity is { } body)
                    mobState.ChangeMobState(body, MobState.Dead);
            }

            Assert.That(match.State, Is.EqualTo(SpaceArenaMatchState.Preparing));
        });

        await client.WaitPost(() => clientNet.ClientDisconnect("SpaceArena disconnect forfeit test."));
        await pair.RunTicksSync(5);
        await server.WaitPost(() =>
        {
            var match = server.EntMan.GetComponent<SpaceArenaMatchComponent>(lobby);
            var deathMatch = server.EntMan.GetComponent<SpaceArenaDeathMatchComponent>(lobby);
            var runtime = server.EntMan.GetComponent<SpaceArenaMatchRuntimeComponent>(lobby);

            Assert.Multiple(() =>
            {
                Assert.That(mobState.IsDead(disconnectedBody), Is.True);
                Assert.That(
                    match.State,
                    Is.EqualTo(SpaceArenaMatchState.Ending).Or.EqualTo(SpaceArenaMatchState.Finished));
                Assert.That(deathMatch.WinningGroup, Is.Not.EqualTo(disconnectedGroup));
                Assert.That(runtime.DisconnectForfeits, Is.Empty);
                Assert.That(runtime.NextDisconnectForfeit, Is.Null);
            });

            server.EntMan.QueueDeleteEntity(lobby);
        });

        await pair.RunTicksSync(2);
        client.SetConnectTarget(server);
        await client.WaitPost(() => clientNet.ClientConnect(null!, 0, username));
        await pair.RunTicksSync(5);
    }
}
