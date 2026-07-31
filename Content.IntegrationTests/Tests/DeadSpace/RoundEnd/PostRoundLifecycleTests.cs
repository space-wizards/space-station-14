// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using Content.Server.Backmen.Economy.Wage;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.DeadSpace.RoundEnd;

[TestFixture]
[NonParallelizable]
public sealed class PostRoundLifecycleTests
{
    private const string TestRule = "WageScheduler";

    [Test]
    public async Task EndRoundRunsOnceFreezesRulesAndOnlyRebroadcastsSnapshot()
    {
        var settings = new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false,
        };
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;
        var ticker = server.System<GameTicker>();
        var observer = server.System<PostRoundLifecycleTestObserverSystem>();
        WageSchedulerRuleComponent? wageScheduler = null;

        await server.WaitAssertion(() =>
        {
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            observer.ResetCounters();
            Assert.That(ticker.StartGameRule(TestRule, out var rule), Is.True);
            Assert.That(server.EntMan.TryGetComponent(rule, out wageScheduler), Is.True);
#pragma warning disable RA0002
            wageScheduler!.TimeUntilNextWage = 100f;
#pragma warning restore RA0002
        });
        await pair.RunTicksSync(2);

        var trackedWageScheduler = wageScheduler!;
        var wageTimeBeforePostRound = trackedWageScheduler.TimeUntilNextWage;
        Assert.That(wageTimeBeforePostRound, Is.LessThan(100f));

        await server.WaitAssertion(() =>
        {
            ticker.EndRound("first result");
            ticker.EndRound("duplicate result");

            Assert.Multiple(() =>
            {
                Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
                Assert.That(observer.ScoreboardEvents, Is.EqualTo(1));
                Assert.That(observer.RoundEndedEvents, Is.EqualTo(1));
                Assert.That(ticker.RebroadcastRoundEndScoreboard(), Is.True);
                Assert.That(observer.ScoreboardEvents, Is.EqualTo(1));
            });

            var rulesBeforeRejectedAdds = server.EntMan.Count<GameRuleComponent>();
            Assert.Multiple(() =>
            {
                Assert.That(ticker.AddGameRule(TestRule), Is.EqualTo(EntityUid.Invalid));
                Assert.That(ticker.StartGameRule(TestRule, out var rejectedRule), Is.False);
                Assert.That(rejectedRule, Is.EqualTo(EntityUid.Invalid));
                Assert.That(server.EntMan.Count<GameRuleComponent>(), Is.EqualTo(rulesBeforeRejectedAdds));
            });
        });

        await pair.RunTicksSync(3);
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(trackedWageScheduler.TimeUntilNextWage, Is.EqualTo(wageTimeBeforePostRound));
                Assert.That(observer.ScoreboardEvents, Is.EqualTo(1));
                Assert.That(observer.RoundEndedEvents, Is.EqualTo(1));
            });
        });

        await pair.CleanReturnAsync();
    }
}

public sealed class PostRoundLifecycleTestObserverSystem : EntitySystem
{
    public int ScoreboardEvents { get; private set; }
    public int RoundEndedEvents { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndMessageEvent>(_ => ScoreboardEvents++);
        SubscribeLocalEvent<RoundEndedEvent>(_ => RoundEndedEvents++);
    }

    public void ResetCounters()
    {
        ScoreboardEvents = 0;
        RoundEndedEvents = 0;
    }
}
