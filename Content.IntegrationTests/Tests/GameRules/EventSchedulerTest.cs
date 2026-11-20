using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed partial class EventSchedulerTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        Map = PoolManager.TestStation
    };

    [SidedDependency(Side.Server)] private EventManagerSystem _eventMan = default!;
    [SidedDependency(Side.Server)] private GameTicker _gameTicker = default!;

    private static readonly string[] AntagGameRules = GameDataScrounger.EntitiesWithComponent("StationEvent");

    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(EventSchedulerTest)), TestOf(typeof(GameRuleComponent)),
     TestOf(typeof(StationEventComponent))]
    [TestCaseSource(nameof(AntagGameRules))]
    [Description($"Ensures all GameRule entities with {nameof(GameRuleComponent)} run only when they're supposed to!")]
    [RunOnSide(Side.Server)]
    public void TestEventScheduler(string ruleId)
    {
        var ruleEv = new EntProtoId[] { ruleId };
        var rule = SProtoMan.Index<EntityPrototype>(ruleId);
        Assert.That(rule.TryComp<GameRuleComponent>(out var ruleComp, SEntMan.ComponentFactory), Is.True);
        Assert.That(rule.TryComp<StationEventComponent>(out var eventComp, SEntMan.ComponentFactory), Is.True);
        // TODO: This is A LOT of duplicate data we don't need. GameRules refactor some day...
        var time = TimeSpan.Compare(ruleComp!.ActivatedAt, TimeSpan.FromMinutes(eventComp!.EarliestStart)) > 0
            ? ruleComp.ActivatedAt
            : TimeSpan.FromMinutes(eventComp!.EarliestStart);
        var players = Math.Max(ruleComp.MinPlayers, eventComp.MinimumPlayers);

        if (time > TimeSpan.Zero || players > 1)
        {
            Assert.That(!_eventMan.TryBuildLimitedEvents(ruleEv, out _),
                $"Rule: {ruleId} was able to be added despite not meeting player and time requirements!");
        }

        Assert.That(_eventMan.TryBuildLimitedEvents(ruleEv, out var events, time, players),
            $"Rule: {ruleId} was not able to be added despite meeting player and time requirements!");
        AddRule(time);

        if (eventComp!.MaxOccurrences is not { } maxOccurrences)
        {
            TestReoccurance();
            return;
        }

        for (var i = 1; i < maxOccurrences; i++)
        {
            TestReoccurance();
        }

        Assert.That(!_eventMan.TryBuildLimitedEvents(ruleEv, out _, playerCount: players), $"Rule {ruleId} was added despite exceeding max occurrences {maxOccurrences}!");

        void AddRule(TimeSpan curTime)
        {
            var selected = _eventMan.FindEvent(events!.Value);
            Assert.That(selected == ruleId,
                $"Rule {ruleId} was not selected despite being the only item available. {selected} was chosen instead!");
            Assert.That(_gameTicker.StartGameRule(selected, out var ruleEnt), $"GameTicker failed to add game rule!");
            SEntMan.EnsureComponent<ActiveGameRuleComponent>(ruleEnt!.Value);
            ruleEnt!.Value.Comp.ActivatedAt = curTime;
        }

        void TestReoccurance()
        {
            // Only check if it actually can be on cooldown :P
            if (eventComp!.ReoccurrenceDelay != 0)
            {
                Assert.That(!_eventMan.TryBuildLimitedEvents(ruleEv, out _, time, players), $"Rule {ruleId} was added despite being on cooldown!");
                time += TimeSpan.FromMinutes(eventComp.ReoccurrenceDelay);
            }

            Assert.That(_eventMan.TryBuildLimitedEvents(ruleEv, out events, time, players),
                $"Rule {ruleId} was unable to be added despite being off cooldown!");
            AddRule(time);
        }
    }
}
