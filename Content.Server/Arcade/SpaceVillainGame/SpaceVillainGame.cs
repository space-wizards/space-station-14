using static Content.Shared.Arcade.SharedSpaceVillainArcadeComponent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.Arcade.SpaceVillain;


/// <summary>
/// A Class to handle all the game-logic of the SpaceVillain-game.
/// </summary>
public sealed partial class SpaceVillainGame
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SpaceVillainArcadeSystem _svArcade = default!;


    [ViewVariables]
    private readonly EntityUid _owner = default!;

    [ViewVariables]
    private bool _running = true;

    [ViewVariables]
    public string Name => $"{_fightVerb} {_villainName}";

    [ViewVariables]
    private string _fightVerb;

    [ViewVariables]
    private Fighter _playerChar;

    [ViewVariables]
    private string _villainName;

    [ViewVariables]
    private Fighter _villainChar;

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

        _owner = owner;
        //todo defeat the curse secret game mode
        _fightVerb = fightVerb;
        _villainName = enemyName;

        _playerChar = new()
        {
            HpMax = 30,
            Hp = 30,
            MpMax = 10,
            Mp = 10
        };

        _villainChar = new()
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
    /// <param name="action">The action the user picked.</param>
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
                if (!_villainChar.Invincible)
                    _villainChar.Hp -= attackAmount;
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
                if (!_playerChar.Invincible)
                    _playerChar.Mp -= pointAmount;
                _playerChar.Hp += healAmount;
                _turtleTracker++;
                break;
            case PlayerAction.Recharge:
                var chargeAmount = _random.Next(4, 7);
                _latestPlayerActionMessage = Loc.GetString(
                    "space-villain-game-player-recharge-message",
                    ("regainedPoints", chargeAmount)
                );
                _audioSystem.PlayPvs(arcade.PlayerChargeSound, uid, AudioParams.Default.WithVolume(-4f));
                _playerChar.Mp += chargeAmount;
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
    /// <returns>An Enemyaction-message.</returns>
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
            if (_playerChar.Invincible)
                return;
            _playerChar.Hp -= boomAmount;
            _turtleTracker--;
            return;
        }

        if (_villainChar.Mp <= 5 && _random.Prob(0.7f))
        {
            var stealAmount = _random.Next(2, 3);
            _latestEnemyActionMessage = Loc.GetString(
                "space-villain-game-enemy-steals-player-power-message",
                ("enemyName", _villainName),
                ("stolenAmount", stealAmount)
            );
            if (_playerChar.Invincible)
                return;
            _playerChar.Mp -= stealAmount;
            _villainChar.Mp += stealAmount;
            return;
        }

        if (_villainChar.Hp <= 10 && _villainChar.Mp > 4)
        {
            _villainChar.Hp += 4;
            _villainChar.Mp -= 4;
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
        if (_playerChar.Invincible)
            return;
        _playerChar.Hp -= attackAmount;
    }

    /// <summary>
    /// Checks the Game conditions and Updates the Ui & Plays a sound accordingly.
    /// </summary>
    /// <returns>A bool indicating if the game should continue.</returns>
    private bool CheckGameConditions(EntityUid uid, SpaceVillainArcadeComponent arcade)
    {
        switch(
            _playerChar.Hp > 0 && _playerChar.Mp > 0,
            _villainChar.Hp > 0 && _villainChar.Mp > 0
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
                    Loc.GetString("space-villain-game-enemy-dies-with-player-message ", ("enemyName", _villainName)),
                    true
                );
                _audioSystem.PlayPvs(arcade.GameOverSound, uid, AudioParams.Default.WithVolume(-4f));
                return false;
        }
    }
}
