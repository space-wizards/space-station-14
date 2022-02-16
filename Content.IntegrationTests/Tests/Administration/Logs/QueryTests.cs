using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class QueryTests : ContentIntegrationTest
{
    [Test]
    public async Task QuerySingleLog()
    {
        var serverOptions = new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            }
        };
        var (client, server) = await StartConnectedServerClientPair(serverOptions: serverOptions);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sSystems = server.ResolveDependency<IEntitySystemManager>();
        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var date = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        IPlayerSession player = default;

        await server.WaitPost(() =>
        {
            player = sPlayers.ServerSessions.First();

            sAdminLogSystem.Add(LogType.Unknown, $"{player.AttachedEntity:Entity} test log: {guid}");
        });

        var filter = new LogFilter
        {
            Round = sGamerTicker.RoundId,
            Search = guid.ToString(),
            Types = new HashSet<LogType> {LogType.Unknown},
            After = date,
            AnyPlayers = new[] {player.UserId.UserId}
        };

        await WaitUntil(server, async () =>
        {
            foreach (var _ in await sAdminLogSystem.All(filter))
            {
                return true;
            }

            return false;
        });
    }
}
