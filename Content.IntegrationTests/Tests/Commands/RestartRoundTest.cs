#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Shared.CCVar;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands;

[TestOf(typeof(RestartRoundNowCommand))]
public sealed class RestartRoundNowTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Dirty = true
    };

    [SidedDependency(Side.Server)] private GameTicker _sTicker = default!;

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task RestartRoundAfterStart(bool lobbyEnabled)
    {
        GameTick tickBeforeRestart = default;

        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.CfgMan.GetCVar(CCVars.GameLobbyEnabled), Is.False);
            Server.CfgMan.SetCVar(CCVars.GameLobbyEnabled, lobbyEnabled);

            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

            tickBeforeRestart = SEntMan.CurrentTick;

            _sTicker.RestartRound();

            if (lobbyEnabled)
            {
                Assert.That(_sTicker.RunLevel, Is.Not.EqualTo(GameRunLevel.InRound));
            }
        });

        await Pair.RunTicksSync(15);

        await Server.WaitAssertion(() =>
        {
            var tickAfterRestart = SEntMan.CurrentTick;

            Assert.That(tickBeforeRestart, Is.LessThan(tickAfterRestart));
        });

        await Pair.RunUntilSynced();
    }
}
