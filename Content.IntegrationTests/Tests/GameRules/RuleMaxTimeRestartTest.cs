#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

[TestOf(typeof(MaxTimeRestartRuleSystem))]
public sealed class RuleMaxTimeRestartTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        InLobby = true
    };

    private static readonly EntProtoId MaxTimeRestartGameRule = "MaxTimeRestart";

    [SidedDependency(Side.Server)] private GameTicker _sGameTicker = default!;

    [Test]
    public async Task RestartTest()
    {
        Assert.That(SEntMan.Count<GameRuleComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<ActiveGameRuleComponent>(), Is.Zero);

        MaxTimeRestartRuleComponent? maxTime = null!;
        await Server.WaitPost(() =>
        {
            _sGameTicker.StartGameRule(MaxTimeRestartGameRule, out var ruleEntity);
            Assert.That(STryComp(ruleEntity, out maxTime));
        });

        Assert.That(SEntMan.Count<GameRuleComponent>(), Is.EqualTo(1));
        Assert.That(SEntMan.Count<ActiveGameRuleComponent>(), Is.EqualTo(1));

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            maxTime.RoundMaxTime = TimeSpan.FromSeconds(3);
            _sGameTicker.StartRound();
        });

        Assert.That(SEntMan.Count<GameRuleComponent>(), Is.EqualTo(1));
        Assert.That(SEntMan.Count<ActiveGameRuleComponent>(), Is.EqualTo(1));

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        });

        var ticks = Server.Timing.TickRate * (int)Math.Ceiling(maxTime.RoundMaxTime.TotalSeconds * 1.1f);
        await RunTicksSync(ticks);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
        });

        ticks = Server.Timing.TickRate * (int)Math.Ceiling(maxTime.RoundEndDelay.TotalSeconds * 1.1f);
        await RunTicksSync(ticks);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        });
    }
}
