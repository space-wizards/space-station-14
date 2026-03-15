using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Server.Arcade.Systems;
using Content.Shared.Arcade.SpaceVillain;

namespace Content.Server.Arcade.SpaceVillain;

/// <summary>
/// A Class to handle all the game-logic of the SpaceVillain-game.
/// </summary>
public sealed partial class SpaceVillainGame
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly SharedAudioSystem _audioSystem = default!;
    private readonly UserInterfaceSystem _uiSystem = default!;
    private readonly ArcadeSystem _arcade = default!;

    [ViewVariables]
    private bool _running = true;

    [ViewVariables]
    public string Name => $"{_fightVerb} {_villainName}";

    [ViewVariables]
    private readonly string _fightVerb;

    [ViewVariables]
    public readonly Fighter PlayerChar;

    [ViewVariables]
    private readonly string _villainName;

    [ViewVariables]
    public readonly Fighter VillainChar;

    [ViewVariables]
    private int _turtleTracker = 0;

    [ViewVariables]
    private string _latestPlayerActionMessage = "";

    [ViewVariables]
    private string _latestEnemyActionMessage = "";

    private static readonly LocId WinMessage = "space-villain-game-player-wins-message";
    private static readonly LocId LoseMessage = "space-villain-game-player-loses-message";
    private static readonly LocId EnemyDiesMessage = "space-villain-game-enemy-dies-message";
    private static readonly LocId EnemyWinsMessage = "space-villain-game-enemy-cheers-message";
    private static readonly LocId BothDieMessage = "space-villain-game-enemy-dies-with-player-message";

    public SpaceVillainGame(SpaceVillainArcadeComponent arcade,
        SpaceVillainArcadeSystem svArcade,
        ArcadeSystem arcadeSystem)
        : this(arcadeSystem,
            svArcade.GenerateFightVerb(arcade),
            svArcade.GenerateEnemyName(arcade))
    { }

    public SpaceVillainGame(ArcadeSystem arcade,
        string fightVerb,
        string enemyName)
    {
        IoCManager.InjectDependencies(this);
        _audioSystem = _entityManager.System<SharedAudioSystem>();
        _uiSystem = _entityManager.System<UserInterfaceSystem>();
        _arcade = arcade;

        //todo defeat the curse secret game mode
        _fightVerb = fightVerb;
        _villainName = enemyName;

        PlayerChar = new()
        {
            HpMax = 30,
            Hp = 30,
            MpMax = 10,
            Mp = 10
        };

        VillainChar = new()
        {
            HpMax = 45,
            Hp = 45,
            MpMax = 20,
            Mp = 20
        };
    }

    /// <summary>
    /// Called by the SpaceVillainArcadeComponent when Userinput is received.
    /// </summary>
    /// <param name="uid">The action the user picked.</param>
    /// <param name="action">The action the user picked.</param>
    /// <param name="arcade">The action the user picked.</param>
    public void ExecutePlayerAction(EntityUid uid, SpaceVillainPlayerAction action, SpaceVillainArcadeComponent arcade)
    {
        if (!_running)
            return;

        switch (action)
        {
            case SpaceVillainPlayerAction.Attack:
                var attackAmount = _random.Next(2, 6);
                _latestPlayerActionMessage = Loc.GetString(
                    "space-villain-game-player-attack-message",
                    ("enemyName", _villainName),
                    ("attackAmount", attackAmount)
                );
                _audioSystem.PlayPvs(arcade.PlayerAttackSound, uid, AudioParams.Default.WithVolume(-4f));
                if (!VillainChar.Invincible)
                    VillainChar.Hp -= attackAmount;
                _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                break;
            case SpaceVillainPlayerAction.Heal:
                var pointAmount = _random.Next(1, 3);
                var healAmount = _random.Next(6, 8);
                _latestPlayerActionMessage = Loc.GetString(
                    "space-villain-game-player-heal-message",
                    ("magicPointAmount", pointAmount),
                    ("healAmount", healAmount)
                );
                _audioSystem.PlayPvs(arcade.PlayerHealSound, uid, AudioParams.Default.WithVolume(-4f));
                if (!PlayerChar.Invincible)
                    PlayerChar.Mp -= pointAmount;
                PlayerChar.Hp += healAmount;
                _turtleTracker++;
                break;
            case SpaceVillainPlayerAction.Recharge:
                var chargeAmount = _random.Next(4, 7);
                _latestPlayerActionMessage = Loc.GetString(
                    "space-villain-game-player-recharge-message",
                    ("regainedPoints", chargeAmount)
                );
                _audioSystem.PlayPvs(arcade.PlayerChargeSound, uid, AudioParams.Default.WithVolume(-4f));
                PlayerChar.Mp += chargeAmount;
                _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                break;
        }

        if (!CheckGameConditions(uid, arcade))
            return;

        ExecuteAiAction();

        if (!CheckGameConditions(uid, arcade))
            return;

        UpdateUi(uid);
    }

    /// <summary>
    /// Handles the logic of the AI
    /// </summary>
    private void ExecuteAiAction()
    {
        if (_turtleTracker >= 4)
        {
            var boomAmount = _random.Next(5, 10);
            _latestEnemyActionMessage = Loc.GetString(
                "space-villain-game-enemy-throws-bomb-message",
                ("enemyName", _villainName),
                ("damageReceived", boomAmount)
            );
            if (PlayerChar.Invincible)
                return;
            PlayerChar.Hp -= boomAmount;
            _turtleTracker--;
            return;
        }

        if (VillainChar.Mp <= 5 && _random.Prob(0.7f))
        {
            var stealAmount = _random.Next(2, 3);
            _latestEnemyActionMessage = Loc.GetString(
                "space-villain-game-enemy-steals-player-power-message",
                ("enemyName", _villainName),
                ("stolenAmount", stealAmount)
            );
            if (PlayerChar.Invincible)
                return;
            PlayerChar.Mp -= stealAmount;
            VillainChar.Mp += stealAmount;
            return;
        }

        if (VillainChar.Hp <= 10 && VillainChar.Mp > 4)
        {
            VillainChar.Hp += 4;
            VillainChar.Mp -= 4;
            _latestEnemyActionMessage = Loc.GetString(
                "space-villain-game-enemy-heals-message",
                ("enemyName", _villainName),
                ("healedAmount", 4)
            );
            return;
        }

        var attackAmount = _random.Next(3, 6);
        _latestEnemyActionMessage =
            Loc.GetString(
                "space-villain-game-enemy-attacks-message",
                ("enemyName", _villainName),
                ("damageDealt", attackAmount)
            );
        if (PlayerChar.Invincible)
            return;
        PlayerChar.Hp -= attackAmount;
    }

    /// <summary>
    /// Checks the Game conditions and Updates the Ui & Plays a sound accordingly.
    /// </summary>
    /// <returns>A bool indicating if the game should continue.</returns>
    private bool CheckGameConditions(EntityUid uid, SpaceVillainArcadeComponent arcade)
    {
        switch (
            PlayerChar.Hp > 0 && PlayerChar.Mp > 0,
            VillainChar.Hp > 0 && VillainChar.Mp > 0
        )
        {
            // Both are alive
            case (true, true):
                return true;

            // Player alive, enemy dead: Win game
            case (true, false):
                CompleteGame(uid,
                    Loc.GetString(WinMessage),
                    Loc.GetString(EnemyDiesMessage, ("enemyName", _villainName)),
                    arcade.WinSound);
                _arcade.WinGame(player: null, machine: uid);

                return false;

            // Enemy dead, player alive: Lose game
            case (false, true):
                CompleteGame(uid,
                    Loc.GetString(LoseMessage),
                    Loc.GetString(EnemyWinsMessage, ("enemyName", _villainName)),
                    arcade.GameOverSound);
                _arcade.LoseGame(player: null, machine: uid);

                return false;

            // Player dead, enemy dead: Draw game
            case (false, false):
                CompleteGame(uid,
                    Loc.GetString(LoseMessage),
                    Loc.GetString(BothDieMessage, ("enemyName", _villainName)),
                    arcade.GameOverSound);
                _arcade.DrawGame(player: null, machine: uid);

                return false;
        }
    }

    private void CompleteGame(EntityUid uid, string playerMessage, string enemyMessage, SoundSpecifier sound)
    {
        _running = false;
        UpdateUi(uid, playerMessage, enemyMessage, metadata: true);
        _audioSystem.PlayPvs(sound, uid, AudioParams.Default.WithVolume(-4f));
    }
}
