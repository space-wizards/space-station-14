#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Arcade;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Arcade
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class SpaceVillainArcadeComponent : SharedSpaceVillainArcadeComponent, IActivate, IWires
    {
        [Dependency] private IRobustRandom _random = null!;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SpaceVillainArcadeUiKey.Key);
        [ViewVariables] private bool _overflowFlag;
        [ViewVariables] private bool _playerInvincibilityFlag;
        [ViewVariables] private bool _enemyInvincibilityFlag;
        [ViewVariables] private SpaceVillainGame _game = null!;

        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleFightVerbs = null!;
        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleFirstEnemyNames = null!;
        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleLastEnemyNames = null!;
        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleRewards = null!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _possibleFightVerbs, "possibleFightVerbs", new List<string>()
                {"Defeat", "Annihilate", "Save", "Strike", "Stop", "Destroy", "Robust", "Romance", "Pwn", "Own"});
            serializer.DataField(ref _possibleFirstEnemyNames, "possibleFirstEnemyNames", new List<string>(){
                "the Automatic", "Farmer", "Lord", "Professor", "the Cuban", "the Evil", "the Dread King",
                "the Space", "Lord", "the Great", "Duke", "General"
            });
            serializer.DataField(ref _possibleLastEnemyNames, "possibleLastEnemyNames", new List<string>()
            {
                "Melonoid", "Murdertron", "Sorcerer", "Ruin", "Jeff", "Ectoplasm", "Crushulon", "Uhangoid",
                "Vhakoid", "Peteoid", "slime", "Griefer", "ERPer", "Lizard Man", "Unicorn"
            });
            serializer.DataField(ref _possibleRewards, "possibleRewards", new List<string>()
            {
                "ToyMouse", "ToyAi", "ToyNuke", "ToyAssistant", "ToyGriffin", "ToyHonk", "ToyIan",
                "ToyMarauder", "ToyMauler", "ToyGygax", "ToyOdysseus", "ToyOwlman", "ToyDeathRipley",
                "ToyPhazon", "ToyFireRipley", "ToyReticence", "ToyRipley", "ToySeraph", "ToyDurand", "ToySkeleton"
            });

            _game = new SpaceVillainGame(this);
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            if (!Powered)
            {
                return;
            }

            var wires = Owner.GetComponent<WiresComponent>();
            if (wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.playerSession);
            } else
            {
                UserInterface?.Toggle(actor.playerSession);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }


        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            if (!(serverMsg.Message is SpaceVillainArcadePlayerActionMessage msg)) return;

            switch (msg.PlayerAction)
            {
                case PlayerAction.Attack:
                    _game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.Heal:
                    _game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.Recharge:
                    _game?.ExecutePlayerAction(msg.PlayerAction);
                    break;
                case PlayerAction.NewGame:
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/newgame.ogg", Owner, AudioParams.Default.WithVolume(-4f));
                    _game = new SpaceVillainGame(this);
                    UserInterface?.SendMessage(_game.GenerateMetaDataMessage());
                    break;
                case PlayerAction.RequestData:
                    UserInterface?.SendMessage(_game.GenerateMetaDataMessage());
                    break;
            }
        }

        public enum Wires
        {
            /// <summary>
            /// Disables Max Health&Mana for both Enemy and Player.
            /// </summary>
            Overflow,
            /// <summary>
            /// Makes Player Invincible.
            /// </summary>
            PlayerInvincible,
            /// <summary>
            /// Makes Enemy Invincible.
            /// </summary>
            EnemyInvincible
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Overflow);
            builder.CreateWire(Wires.PlayerInvincible);
            builder.CreateWire(Wires.EnemyInvincible);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            var wire = (Wires) args.Identifier;
            var value = args.Action != SharedWiresComponent.WiresAction.Mend;
            switch (wire)
            {
                case Wires.Overflow:
                    _overflowFlag = value;
                    break;
                case Wires.PlayerInvincible:
                    _playerInvincibilityFlag = value;
                    break;
                case Wires.EnemyInvincible:
                    _enemyInvincibilityFlag = value;
                    break;
            }
        }

        /// <summary>
        /// Called when the user wins the game.
        /// </summary>
        public void ProcessWin()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.SpawnEntity(_random.Pick(_possibleRewards), Owner.Transform.MapPosition);
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
        public class SpaceVillainGame
        {
            [Dependency] private readonly IRobustRandom _random = default!;

            [ViewVariables] private SpaceVillainArcadeComponent Owner;

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

            [ViewVariables(VVAccess.ReadWrite)] private string _fightVerb;
            [ViewVariables(VVAccess.ReadWrite)] private string _enemyName;

            [ViewVariables] private bool _running = true;

            private string _latestPlayerActionMessage = "";
            private string _latestEnemyActionMessage = "";

            public SpaceVillainGame(SpaceVillainArcadeComponent owner) : this(owner, owner.GenerateFightVerb(), owner.GenerateEnemyName()){}

            public SpaceVillainGame(SpaceVillainArcadeComponent owner, string fightVerb, string enemyName)
            {
                IoCManager.InjectDependencies(this);
                Owner = owner;
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
                if(Owner._overflowFlag) return;

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
                        _latestPlayerActionMessage = $"You attack {_enemyName} for {attackAmount}!";
                        EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/player_attack.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                        if(!Owner._enemyInvincibilityFlag) _enemyHp -= attackAmount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                    case PlayerAction.Heal:
                        var pointAmount = _random.Next(1, 3);
                        var healAmount = _random.Next(6, 8);
                        _latestPlayerActionMessage = $"You use {pointAmount} magic to heal for {healAmount} damage!";
                        EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/player_heal.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                        if(!Owner._playerInvincibilityFlag) _playerMp -= pointAmount;
                        _playerHp += healAmount;
                        _turtleTracker++;
                        break;
                    case PlayerAction.Recharge:
                        var charge_amount = _random.Next(4, 7);
                        _latestPlayerActionMessage = $"You regain {charge_amount} points";
                        EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/player_charge.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                        _playerMp += charge_amount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                }

                if (!CheckGameConditions())
                {
                    _running = false;
                    return;
                }

                ValidateVars();
                ExecuteAiAction();

                if (!CheckGameConditions())
                {
                    _running = false;
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
                if ((_enemyHp <= 0 || _enemyMp <= 0) && (_playerHp > 0 && _playerMp > 0))
                {
                    UpdateUi("You won!", $"{_enemyName} dies.");
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/win.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                    Owner.ProcessWin();
                    return false;
                }
                if ((_playerHp <= 0 || _playerMp <= 0) && _enemyHp > 0 && _enemyMp > 0)
                {
                    UpdateUi("You lost!", $"{_enemyName} cheers.");
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/gameover.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                    return false;
                }
                if ((_playerHp <= 0 || _playerMp <= 0) && (_enemyHp <= 0 || _enemyMp <= 0))
                {
                    UpdateUi("You lost!", $"{_enemyName} dies, but takes you with him.");
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/Arcade/gameover.ogg", Owner.Owner, AudioParams.Default.WithVolume(-4f));
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Updates the UI.
            /// </summary>
            private void UpdateUi()
            {
                Owner.UserInterface?.SendMessage(GenerateUpdateMessage(_latestPlayerActionMessage, _latestEnemyActionMessage));
            }

            private void UpdateUi(string message1, string message2)
            {
                _latestPlayerActionMessage = message1;
                _latestEnemyActionMessage = message2;
                UpdateUi();
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
                    _latestEnemyActionMessage = $"{_enemyName} throws a bomb, exploding you for {boomAmount} damage!";
                    if (Owner._playerInvincibilityFlag) return;
                    _playerHp -= boomAmount;
                    _turtleTracker--;
                }else if (_enemyMp <= 5 && _random.Prob(0.7f))
                {
                    var stealAmount = _random.Next(2, 3);
                    _latestEnemyActionMessage = $"{_enemyName} steals {stealAmount} of your power!";
                    if (Owner._playerInvincibilityFlag) return;
                    _playerMp -= stealAmount;
                    _enemyMp += stealAmount;
                }else if (_enemyHp <= 10 && _enemyMp > 4)
                {
                    _enemyHp += 4;
                    _enemyMp -= 4;
                    _latestEnemyActionMessage = $"{_enemyName} heals for 4 health!";
                }
                else
                {
                    var attackAmount = _random.Next(3, 6);
                    _latestEnemyActionMessage = $"{_enemyName} attacks you for {attackAmount} damage!";
                    if (Owner._playerInvincibilityFlag) return;
                    _playerHp -= attackAmount;
                }
            }

            /// <summary>
            /// Generates a Metadata-message based on the objects values.
            /// </summary>
            /// <returns>A Metadata-message.</returns>
            public SpaceVillainArcadeMetaDataUpdateMessage GenerateMetaDataMessage()
            {
                return new SpaceVillainArcadeMetaDataUpdateMessage(_playerHp, _playerMp, _enemyHp, _enemyMp, _latestPlayerActionMessage, _latestEnemyActionMessage, Name, _enemyName);
            }

            /// <summary>
            /// Creates an Update-message based on the objects values.
            /// </summary>
            /// <param name="playerAction">Content of the Playeraction-field.</param>
            /// <param name="enemyAction">Content of the Enemyaction-field.</param>
            /// <returns></returns>
            public SpaceVillainArcadeDataUpdateMessage
                GenerateUpdateMessage(string playerAction = "", string enemyAction = "")
            {
                return new SpaceVillainArcadeDataUpdateMessage(_playerHp, _playerMp, _enemyHp, _enemyMp, playerAction,
                    enemyAction);
            }
        }
    }
}
