using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

// TODO: ECS.

namespace Content.Server.Arcade.Components
{
    [RegisterComponent]
    public sealed class SpaceVillainArcadeComponent : SharedSpaceVillainArcadeComponent
    {
        [Dependency] private readonly IRobustRandom _random = null!;

        [Dependency] private readonly IEntityManager _entityManager = default!;
        private bool Powered => _entityManager.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var powerReceiverComponent) && powerReceiverComponent.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SpaceVillainArcadeUiKey.Key);
        [ViewVariables] public bool OverflowFlag;
        [ViewVariables] public bool PlayerInvincibilityFlag;
        [ViewVariables] public bool EnemyInvincibilityFlag;
        [ViewVariables] public SpaceVillainGame Game = null!;

        [DataField("newGameSound")] private SoundSpecifier _newGameSound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");
        [DataField("playerAttackSound")] private SoundSpecifier _playerAttackSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_attack.ogg");
        [DataField("playerHealSound")] private SoundSpecifier _playerHealSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_heal.ogg");
        [DataField("playerChargeSound")] private SoundSpecifier _playerChargeSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_charge.ogg");
        [DataField("winSound")] private SoundSpecifier _winSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");
        [DataField("gameOverSound")] private SoundSpecifier _gameOverSound = new SoundPathSpecifier("/Audio/Effects/Arcade/gameover.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("possibleFightVerbs")]
        private List<string> _possibleFightVerbs = new List<string>()
            {"Defeat", "Annihilate", "Save", "Strike", "Stop", "Destroy", "Robust", "Romance", "Pwn", "Own"};
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("possibleFirstEnemyNames")]
        private List<string> _possibleFirstEnemyNames = new List<string>(){
            "the Automatic", "Farmer", "Lord", "Professor", "the Cuban", "the Evil", "the Dread King",
            "the Space", "Lord", "the Great", "Duke", "General"
        };
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("possibleLastEnemyNames")]
        private List<string> _possibleLastEnemyNames = new List<string>()
        {
            "Melonoid", "Murdertron", "Sorcerer", "Ruin", "Jeff", "Ectoplasm", "Crushulon", "Uhangoid",
            "Vhakoid", "Peteoid", "slime", "Griefer", "ERPer", "Lizard Man", "Unicorn"
        };
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("possibleRewards", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
        private List<string> _possibleRewards = new List<string>()
        {
            "ToyMouse", "ToyAi", "ToyNuke", "ToyAssistant", "ToyGriffin", "ToyHonk", "ToyIan",
            "ToyMarauder", "ToyMauler", "ToyGygax", "ToyOdysseus", "ToyOwlman", "ToyDeathRipley",
            "ToyPhazon", "ToyFireRipley", "ToyReticence", "ToyRipley", "ToySeraph", "ToyDurand", "ToySkeleton",
            "FoamCrossbow", "RevolverCapGun", "PlushieLizard", "PlushieAtmosian", "PlushieSpaceLizard",
            "PlushieNuke", "PlushieCarp", "PlushieRatvar", "PlushieNar", "PlushieSnake", "Basketball", "Football",
            "PlushieRouny", "PlushieBee", "PlushieSlime", "BalloonCorgi", "ToySword", "CrayonBox", "BoxDonkSoftBox", "BoxCartridgeCap",
            "HarmonicaInstrument", "OcarinaInstrument", "RecorderInstrument", "GunpetInstrument", "BirdToyInstrument", "PlushieXeno"
        };

        [DataField("rewardMinAmount")]
        public int _rewardMinAmount;

        [DataField("rewardMaxAmount")]
        public int _rewardMaxAmount;

        [ViewVariables(VVAccess.ReadWrite)]
        public int _rewardAmount = 0;

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            // Random amount of prizes
            _rewardAmount = new Random().Next(_rewardMinAmount, _rewardMaxAmount + 1);

        }

        public void OnPowerStateChanged(PowerChangedEvent e)
        {
            if (e.Powered) return;

            UserInterface?.CloseAll();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            if (serverMsg.Message is not SpaceVillainArcadePlayerActionMessage msg) return;

            switch (msg.PlayerAction)
            {
                case PlayerAction.Attack:
                    Game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.Heal:
                    Game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.Recharge:
                    Game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.NewGame:
                    SoundSystem.Play(_newGameSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-4f));

                    Game = new SpaceVillainGame(this);
                    UserInterface?.SendMessage(Game.GenerateMetaDataMessage());
                    break;
                case PlayerAction.RequestData:
                    UserInterface?.SendMessage(Game.GenerateMetaDataMessage());
                    break;
            }
        }

        /// <summary>
        /// Called when the user wins the game.
        /// </summary>
        public void ProcessWin()
        {
            if (_rewardAmount > 0)
            {
                _entityManager.SpawnEntity(_random.Pick(_possibleRewards), _entityManager.GetComponent<TransformComponent>(Owner).Coordinates);
                _rewardAmount--;
            }
        }

        /// <summary>
        /// Picks a fight-verb from the list of possible Verbs.
        /// </summary>
        /// <returns>A fight-verb.</returns>
        public string GenerateFightVerb()
        {
            return _random.Pick(_possibleFightVerbs);
        }

        /// <summary>
        /// Generates an enemy-name comprised of a first- and last-name.
        /// </summary>
        /// <returns>An enemy-name.</returns>
        public string GenerateEnemyName()
        {
            return $"{_random.Pick(_possibleFirstEnemyNames)} {_random.Pick(_possibleLastEnemyNames)}";
        }

        /// <summary>
        /// A Class to handle all the game-logic of the SpaceVillain-game.
        /// </summary>
        public sealed class SpaceVillainGame
        {
            [Dependency] private readonly IRobustRandom _random = default!;

            [ViewVariables] private readonly SpaceVillainArcadeComponent _owner;

            [ViewVariables] public string Name => $"{_fightVerb} {_enemyName}";
            [ViewVariables(VVAccess.ReadWrite)] private int _playerHp = 30;
            [ViewVariables(VVAccess.ReadWrite)] private int _playerHpMax = 30;
            [ViewVariables(VVAccess.ReadWrite)] private int _playerMp = 10;
            [ViewVariables(VVAccess.ReadWrite)] private int _playerMpMax = 10;
            [ViewVariables(VVAccess.ReadWrite)] private int _enemyHp = 45;
            [ViewVariables(VVAccess.ReadWrite)] private int _enemyHpMax = 45;
            [ViewVariables(VVAccess.ReadWrite)] private int _enemyMp = 20;
            [ViewVariables(VVAccess.ReadWrite)] private int _enemyMpMax = 20;
            [ViewVariables(VVAccess.ReadWrite)] private int _turtleTracker;

            [ViewVariables(VVAccess.ReadWrite)] private readonly string _fightVerb;
            [ViewVariables(VVAccess.ReadWrite)] private readonly string _enemyName;

            [ViewVariables] private bool _running = true;

            private string _latestPlayerActionMessage = "";
            private string _latestEnemyActionMessage = "";

            public SpaceVillainGame(SpaceVillainArcadeComponent owner) : this(owner, owner.GenerateFightVerb(), owner.GenerateEnemyName()) { }

            public SpaceVillainGame(SpaceVillainArcadeComponent owner, string fightVerb, string enemyName)
            {
                IoCManager.InjectDependencies(this);
                _owner = owner;
                //todo defeat the curse secret game mode
                _fightVerb = fightVerb;
                _enemyName = enemyName;
            }

            /// <summary>
            /// Validates all vars incase they overshoot their max-values.
            /// Does not check if vars surpass 0.
            /// </summary>
            private void ValidateVars()
            {
                if (_owner.OverflowFlag) return;

                if (_playerHp > _playerHpMax) _playerHp = _playerHpMax;
                if (_playerMp > _playerMpMax) _playerMp = _playerMpMax;
                if (_enemyHp > _enemyHpMax) _enemyHp = _enemyHpMax;
                if (_enemyMp > _enemyMpMax) _enemyMp = _enemyMpMax;
            }

            /// <summary>
            /// Called by the SpaceVillainArcadeComponent when Userinput is received.
            /// </summary>
            /// <param name="action">The action the user picked.</param>
            public void ExecutePlayerAction(PlayerAction action)
            {
                if (!_running) return;

                switch (action)
                {
                    case PlayerAction.Attack:
                        var attackAmount = _random.Next(2, 6);
                        _latestPlayerActionMessage = Loc.GetString("space-villain-game-player-attack-message",
                                                                   ("enemyName", _enemyName),
                                                                   ("attackAmount", attackAmount));
                        SoundSystem.Play(_owner._playerAttackSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                        if (!_owner.EnemyInvincibilityFlag)
                            _enemyHp -= attackAmount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                    case PlayerAction.Heal:
                        var pointAmount = _random.Next(1, 3);
                        var healAmount = _random.Next(6, 8);
                        _latestPlayerActionMessage = Loc.GetString("space-villain-game-player-heal-message",
                                                                    ("magicPointAmount", pointAmount),
                                                                    ("healAmount", healAmount));
                        SoundSystem.Play(_owner._playerHealSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                        if (!_owner.PlayerInvincibilityFlag)
                            _playerMp -= pointAmount;
                        _playerHp += healAmount;
                        _turtleTracker++;
                        break;
                    case PlayerAction.Recharge:
                        var chargeAmount = _random.Next(4, 7);
                        _latestPlayerActionMessage = Loc.GetString("space-villain-game-player-recharge-message", ("regainedPoints", chargeAmount));
                        SoundSystem.Play(_owner._playerChargeSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                        _playerMp += chargeAmount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                }

                if (!CheckGameConditions())
                {
                    return;
                }

                ValidateVars();
                ExecuteAiAction();

                if (!CheckGameConditions())
                {
                    return;
                }
                ValidateVars();
                UpdateUi();
            }

            /// <summary>
            /// Checks the Game conditions and Updates the Ui & Plays a sound accordingly.
            /// </summary>
            /// <returns>A bool indicating if the game should continue.</returns>
            private bool CheckGameConditions()
            {
                if ((_playerHp > 0 && _playerMp > 0) && (_enemyHp <= 0 || _enemyMp <= 0))
                {
                    _running = false;
                    UpdateUi(Loc.GetString("space-villain-game-player-wins-message"),
                             Loc.GetString("space-villain-game-enemy-dies-message", ("enemyName", _enemyName)),
                             true);
                    SoundSystem.Play(_owner._winSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                    _owner.ProcessWin();
                    return false;
                }

                if (_playerHp > 0 && _playerMp > 0) return true;

                if ((_enemyHp > 0 && _enemyMp > 0))
                {
                    _running = false;
                    UpdateUi(Loc.GetString("space-villain-game-player-loses-message"),
                             Loc.GetString("space-villain-game-enemy-cheers-message", ("enemyName", _enemyName)),
                             true);
                    SoundSystem.Play(_owner._gameOverSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                    return false;
                }
                if (_enemyHp <= 0 || _enemyMp <= 0)
                {
                    _running = false;
                    UpdateUi(Loc.GetString("space-villain-game-player-loses-message"),
                             Loc.GetString("space-villain-game-enemy-dies-with-player-message ", ("enemyName", _enemyName)),
                             true);
                    SoundSystem.Play(_owner._gameOverSound.GetSound(), Filter.Pvs(_owner.Owner), _owner.Owner, AudioParams.Default.WithVolume(-4f));
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Updates the UI.
            /// </summary>
            private void UpdateUi(bool metadata = false)
            {
                _owner.UserInterface?.SendMessage(metadata ? GenerateMetaDataMessage() : GenerateUpdateMessage());
            }

            private void UpdateUi(string message1, string message2, bool metadata = false)
            {
                _latestPlayerActionMessage = message1;
                _latestEnemyActionMessage = message2;
                UpdateUi(metadata);
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
                    _latestEnemyActionMessage = Loc.GetString("space-villain-game-enemy-throws-bomb-message",
                                                              ("enemyName", _enemyName),
                                                              ("damageReceived", boomAmount));
                    if (_owner.PlayerInvincibilityFlag) return;
                    _playerHp -= boomAmount;
                    _turtleTracker--;
                }
                else if (_enemyMp <= 5 && _random.Prob(0.7f))
                {
                    var stealAmount = _random.Next(2, 3);
                    _latestEnemyActionMessage = Loc.GetString("space-villain-game-enemy-steals-player-power-message",
                                                              ("enemyName", _enemyName),
                                                              ("stolenAmount", stealAmount));
                    if (_owner.PlayerInvincibilityFlag) return;
                    _playerMp -= stealAmount;
                    _enemyMp += stealAmount;
                }
                else if (_enemyHp <= 10 && _enemyMp > 4)
                {
                    _enemyHp += 4;
                    _enemyMp -= 4;
                    _latestEnemyActionMessage = Loc.GetString("space-villain-game-enemy-heals-message",
                                                              ("enemyName", _enemyName),
                                                              ("healedAmount", 4));
                }
                else
                {
                    var attackAmount = _random.Next(3, 6);
                    _latestEnemyActionMessage =
                        Loc.GetString("space-villain-game-enemy-attacks-message",
                                      ("enemyName", _enemyName),
                                      ("damageDealt", attackAmount));
                    if (_owner.PlayerInvincibilityFlag) return;
                    _playerHp -= attackAmount;
                }
            }

            /// <summary>
            /// Generates a Metadata-message based on the objects values.
            /// </summary>
            /// <returns>A Metadata-message.</returns>
            public SpaceVillainArcadeMetaDataUpdateMessage GenerateMetaDataMessage()
            {
                return new(_playerHp, _playerMp, _enemyHp, _enemyMp, _latestPlayerActionMessage, _latestEnemyActionMessage, Name, _enemyName, !_running);
            }

            /// <summary>
            /// Creates an Update-message based on the objects values.
            /// </summary>
            /// <returns>An Update-Message.</returns>
            public SpaceVillainArcadeDataUpdateMessage
                GenerateUpdateMessage()
            {
                return new(_playerHp, _playerMp, _enemyHp, _enemyMp, _latestPlayerActionMessage,
                    _latestEnemyActionMessage);
            }
        }
    }
}
