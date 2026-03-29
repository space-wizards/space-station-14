using static Content.Shared.Arcade.SharedSpaceVillainArcadeComponent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

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
    private readonly SpaceVillainArcadeSystem _svArcade = default!;


    [ViewVariables]
    private readonly EntityUid _owner = default!;

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

    public SpaceVillainGame(EntityUid owner, SpaceVillainArcadeComponent arcade, SpaceVillainArcadeSystem arcadeSystem)
        : this(owner, arcade, arcadeSystem, arcadeSystem.GenerateFightVerb(arcade), arcadeSystem.GenerateEnemyName(arcade))
    {
    }

    public SpaceVillainGame(EntityUid owner, SpaceVillainArcadeComponent arcade, SpaceVillainArcadeSystem arcadeSystem, string fightVerb, string enemyName)
    {
        IoCManager.InjectDependencies(this);
        _audioSystem = _entityManager.System<SharedAudioSystem>();
        _uiSystem = _entityManager.System<UserInterfaceSystem>();
        _svArcade = _entityManager.System<SpaceVillainArcadeSystem>();

        _owner = owner;
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
    public void ExecutePlayerAction(EntityUid uid, PlayerAction action, SpaceVillainArcadeComponent arcade)
    {
        if (!_running)
            return;

        switch (action)
        {
            case PlayerAction.Attack:
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
            case PlayerAction.Heal:
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
            case PlayerAction.Recharge:
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
            case (true, true):
                return true;
            case (true, false):
                _running = false;
                UpdateUi(
                    uid,
                    Loc.GetString("space-villain-game-player-wins-message"),
                    Loc.GetString("space-villain-game-enemy-dies-message", ("enemyName", _villainName)),
                    true
                );
                _audioSystem.PlayPvs(arcade.WinSound, uid, AudioParams.Default.WithVolume(-4f));
                _svArcade.ProcessWin(uid, arcade);
                return false;
            case (false, true):
                _running = false;
                UpdateUi(
                    uid,
                    Loc.GetString("space-villain-game-player-loses-message"),
                    Loc.GetString("space-villain-game-enemy-cheers-message", ("enemyName", _villainName)),
                    true
                );
                _audioSystem.PlayPvs(arcade.GameOverSound, uid, AudioParams.Default.WithVolume(-4f));
                return false;
            case (false, false):
                _running = false;
                UpdateUi(
                    uid,
                    Loc.GetString("space-villain-game-player-loses-message"),
                    Loc.GetString("space-villain-game-enemy-dies-with-player-message", ("enemyName", _villainName)),
                    true
                );
                _audioSystem.PlayPvs(arcade.GameOverSound, uid, AudioParams.Default.WithVolume(-4f));
                return false;
        }
    }
}
