#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

/// <summary>
/// Tests for pre-round admin log persistence.
/// </summary>
[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class PreRoundAddTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        InLobby = true,
        Dirty = true
    };

    [Test, Explicit("Requires fresh pair in lobby — unreliable with test pool reuse")]
    public async Task PreRoundAddAndGetSingle()
    {
        var pair = Pair;
        var server = pair.Server;

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        // This test requires the server to be in pre-round lobby.
        // If the pair isn't in lobby skip.
        GameRunLevel runLevel = default;
        await server.WaitPost(() => runLevel = sGamerTicker.RunLevel);
        if (runLevel != GameRunLevel.PreRoundLobby)
        {
            Assert.Ignore("Server is not in pre-round lobby — pool pair was not reset correctly.");
        }

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            sAdminLogSystem.AddStructured(LogType.Unknown, $"test log: {guid}",
                payload: JsonSerializer.SerializeToDocument(new { guid = guid.ToString() }));
        });

        await server.WaitPost(() =>
        {
            sGamerTicker.StartRound(true);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Types = new HashSet<LogType> { LogType.Unknown }
            });

            var match = logs.FirstOrDefault(l => l.Message.Contains(guid.ToString()));
            if (match.Id == 0)
                return false;

            log = match;
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

            Assert.That(root.TryGetProperty("guid", out _), Is.True);

            json.Dispose();
        }
    }
}
