#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestOf(typeof(AdminLogSystem))]
public sealed class QueryTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [SidedDependency(Side.Server)] private IAdminLogManager _sLogManager = default!;
    [SidedDependency(Side.Server)] private IPlayerManager _sPlayerManager = default!;
    [SidedDependency(Side.Server)] private GameTicker _sTicker = default!;

    [Test]
    public async Task QuerySingleLog()
    {
        var date = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        ICommonSession player = default!;

        await Server.WaitPost(() =>
        {
            player = _sPlayerManager.Sessions.First();

            _sLogManager.Add(LogType.Unknown, $"{player.AttachedEntity:Entity} test log: {guid}");
        });

        var filter = new LogFilter
        {
            Round = _sTicker.RoundId,
            Search = guid.ToString(),
            Types = [LogType.Unknown],
            After = date,
            AnyPlayers = [player.UserId.UserId]
        };

        await PoolManager.WaitUntil(Server, async () =>
        {
            foreach (var _ in await _sLogManager.All(filter))
            {
                return true;
            }

            return false;
        });
    }
}
