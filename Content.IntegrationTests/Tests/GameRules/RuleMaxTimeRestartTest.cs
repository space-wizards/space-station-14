using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
[TestOf(typeof(MaxTimeRestartRuleSystem))]
public sealed class RuleMaxTimeRestartTest
{
    [Test]
    public async Task RestartTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var server = pair.Server;

        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();

        ticker.StartGameRule("MaxTimeRestart", out var ruleEntity);
        Assert.That(entMan.TryGetComponent<MaxTimeRestartRuleComponent>(ruleEntity, out var maxTime));

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        maxTime.RoundMaxTime = TimeSpan.FromSeconds(3);
        await server.WaitPost(() => ticker.StartRound());

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        await pair.RunSeconds((float)maxTime.RoundMaxTime.TotalSeconds * 1.1f);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
        await pair.RunSeconds((float)maxTime.RoundEndDelay.TotalSeconds * 1.1f);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        await pair.CleanReturnAsync();
    }
}
