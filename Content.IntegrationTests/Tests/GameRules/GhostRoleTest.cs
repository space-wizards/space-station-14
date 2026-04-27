using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class GhostRoleTest : GameTest
{
    private static string[] _gameRules = GameDataScrounger.EntitiesWithComponent("AntagSelection");

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent))]
    [TestCaseSource(nameof(_gameRules))]
    [Description("Ensures all GameRule entities with ghost roles can properly spawn those roles and they can be taken.")]
    public async Task TestAllGamemodesCanStart(string ruleId)
    {
        await Server.WaitIdleAsync();
        var gameTicker = Server.System<GameTicker>();
        var factory = Server.Resolve<IComponentFactory>();

        var rule = Server.ProtoMan.Index<EntityPrototype>(ruleId);
        Assert.That(rule.TryGetComponent<AntagSelectionComponent>(out var antag, factory));

        await Server.WaitAssertion(() =>
        {
            gameTicker.StartGameRule(ruleId);
        });

        // TODO: THE REST OF THE TEST

        await Server.WaitAssertion(() =>
        {
            // End all rules
            gameTicker.ClearGameRules();
            Assert.That(!gameTicker.GetAddedGameRules().Any());
        });
    }
}
