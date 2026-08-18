#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class FailAndStartPresetTest : GameTest
{
    private const string TestPreset = "TestPreset";
    private const string TestPresetTenPlayers = "TestPresetTenPlayers";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: gamePreset
  id: {TestPreset}
  alias:
    - nukeops
  name: Test Preset
  description: """"
  showInVote: false
  rules:
  - TestRule

- type: gamePreset
  id: {TestPresetTenPlayers}
  alias:
    - nukeops
  name: Test Preset 10 players
  description: """"
  showInVote: false
  rules:
  - TestRuleTenPlayers

- type: entity
  id: TestRule
  parent: BaseGameRule
  categories: [ GameRules ]
  components:
  - type: GameRule
    minPlayers: 0
  - type: TestRule

- type: entity
  id: TestRuleTenPlayers
  parent: BaseGameRule
  categories: [ GameRules ]
  components:
  - type: GameRule
    minPlayers: 10
  - type: TestRule
";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    [SidedDependency(Side.Server)] private GameTicker _sTicker = default!;

    /// <summary>
    ///     Test that a nuke ops gamemode can start after failing to start once.
    /// </summary>
    [Test]
    [Description("Tests that a nuke ops gamemode can start after failing to start once.")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), true)]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameLobbyFallbackEnabled), false)]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameLobbyDefaultPreset), TestPreset)]
    public async Task FailAndStartTest()
    {
        using (Assert.EnterMultipleScope())
        {
            // Initially in the lobby
            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(Client.AttachedEntity, Is.Null);
            Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        }

        // Try to start nukeops without readying up
        await Pair.WaitCommand($"setgamepreset {TestPresetTenPlayers} 9999");
        await Pair.WaitCommand("startround");
        await RunTicksSync(10);

        using (Assert.EnterMultipleScope())
        {
            // Game should not have started
            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
            Assert.That(CEntMan.EntityExists(Client.AttachedEntity), Is.False);
            var player = ServerSession!.AttachedEntity;
            Assert.That(SEntMan.EntityExists(player), Is.False);
        }

        // Ready up and start nukeops
        await Pair.WaitClientCommand("toggleready True");
        Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await Pair.WaitCommand($"setgamepreset {TestPreset} 9999");
        await Pair.WaitCommand("startround");
        await RunTicksSync(10);

        using (Assert.EnterMultipleScope())
        {
            // Game should have started
            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
            Assert.That(CEntMan.EntityExists(Client.AttachedEntity));
            var player = ServerSession!.AttachedEntity!.Value;
            Assert.That(SEntMan.EntityExists(player));
        }

        // Clear the preset override
        _sTicker.SetGamePreset((GamePresetPrototype?)null);
    }
}

public sealed partial class TestRuleSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStartAttempt);
    }

    private void OnRoundStartAttempt(RoundStartAttemptEvent args)
    {
        if (args.Forced || args.Cancelled)
            return;

        var query = EntityQueryEnumerator<TestRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out _, out _, out var gameRule))
        {
            var minPlayers = gameRule.MinPlayers;
            if (!gameRule.CancelPresetOnTooFewPlayers)
                continue;
            if (args.Players.Length >= minPlayers)
                continue;

            args.Cancel();
        }
    }
}

[RegisterComponent]
public sealed partial class TestRuleComponent : Component;
