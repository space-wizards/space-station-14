// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Commands;
using Robust.Server.Console;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.DeadSpace.RoundEnd;

[TestFixture]
[TestOf(typeof(DelayRoundEndCommand))]
public sealed class DelayRoundEndCommandTests
{
    [Test]
    public async Task CanPauseAndShortenEndBeforeManifest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Dirty = true,
        });
        var server = pair.Server;
        var console = server.ResolveDependency<IServerConsoleHost>();
        var ticker = server.System<GameTicker>();
        var roundEnd = server.System<RoundEndSystem>();
        var timing = server.ResolveDependency<IGameTiming>();

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            roundEnd.StartRoundEndTimer(TimeSpan.FromSeconds(3));
            console.ExecuteCommand("delayroundend");
        });

        await pair.RunTicksSync(timing.TickRate * 4);
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound)));

        await server.WaitAssertion(() =>
        {
            console.ExecuteCommand("delayroundend -2");
            console.ExecuteCommand("delayroundend");
        });

        await pair.RunTicksSync((int) Math.Ceiling(timing.TickRate * 0.5f));
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound)));

        await pair.RunTicksSync(timing.TickRate * 2);
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound)));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CanPauseAndShortenRestartAfterManifest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Dirty = true,
        });
        var server = pair.Server;
        var console = server.ResolveDependency<IServerConsoleHost>();
        var ticker = server.System<GameTicker>();
        var roundEnd = server.System<RoundEndSystem>();
        var timing = server.ResolveDependency<IGameTiming>();

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            roundEnd.EndRound(TimeSpan.FromSeconds(3));
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            console.ExecuteCommand("delayroundend");
        });

        await pair.RunTicksSync(timing.TickRate * 4);
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound)));

        await server.WaitAssertion(() =>
        {
            console.ExecuteCommand("delayroundend -2");
            console.ExecuteCommand("delayroundend");
        });

        await pair.RunTicksSync((int) Math.Ceiling(timing.TickRate * 0.5f));
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound)));

        await pair.RunTicksSync(timing.TickRate * 2);
        await server.WaitAssertion(() => Assert.That(ticker.RunLevel, Is.Not.EqualTo(GameRunLevel.PostRound)));

        await pair.CleanReturnAsync();
    }
}
