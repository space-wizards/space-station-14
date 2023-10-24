using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class QueryTests
{
    [Test]
    public async Task QuerySingleLog()
    {
        await using var pair = await PoolManager.GetServerClient(AddTests.LogTestSettings);
        var server = pair.Server;

        var sSystems = server.ResolveDependency<IEntitySystemManager>();
        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
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
            Types = new HashSet<LogType> { LogType.Unknown },
            After = date,
            AnyPlayers = new[] { player.UserId.UserId }
        };

        await PoolManager.WaitUntil(server, async () =>
        {
            foreach (var _ in await sAdminLogSystem.All(filter))
            {
                return true;
            }

            return false;
        });

        await pair.CleanReturnAsync();
    }
}
