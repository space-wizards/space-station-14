#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Arcade;
using Content.Shared.GameObjects.Components.VendingMachines;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
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
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SpaceVillainArcadeUiKey.Key);
        [ViewVariables] private bool _overflowFlag;
        [ViewVariables] private bool _playerInvincibilityFlag;
        [ViewVariables] private bool _enemyInvincibilityFlag;
        [ViewVariables] private SpaceVillainGame _game = null!;

        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleFightVerbs = null!;
        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleFirstEnemyNames = null!;
        [ViewVariables(VVAccess.ReadWrite)] private List<string> _possibleLastEnemyNames = null!;

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

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged += UpdatePower;
                TrySetVisualState(receiver.Powered ? SpaceVillainArcadeVisualState.Normal : SpaceVillainArcadeVisualState.Off);
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= UpdatePower;
            }

            base.OnRemove();
        }

        private void UpdatePower(object? sender, PowerStateEventArgs args)
        {
            var state = args.Powered ? SpaceVillainArcadeVisualState.Normal : SpaceVillainArcadeVisualState.Off;
            TrySetVisualState(state);
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
            /// Disables Max Health&Mana
            /// </summary>
            Overflow,
            /// <summary>
            /// Makes Player Invincible
            /// </summary>
            PlayerInvincible,
            /// <summary>
            /// Makes Enemy Invincible
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

        private void TrySetVisualState(SpaceVillainArcadeVisualState state)
        {
            var finalState = state;
            if (!Powered)
            {
                finalState = SpaceVillainArcadeVisualState.Off;
            }

            //todo set visuals
        }

        public string GenerateFightVerb()
        {
            return IoCManager.Resolve<IRobustRandom>().Pick(_possibleFightVerbs);
        }

        public string GenerateEnemyName()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            return $"{random.Pick(_possibleFirstEnemyNames)} {random.Pick(_possibleLastEnemyNames)}";
        }

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

            public SpaceVillainGame(SpaceVillainArcadeComponent owner) : this(owner, owner.GenerateFightVerb(), owner.GenerateEnemyName()){}

            public SpaceVillainGame(SpaceVillainArcadeComponent owner, string fightVerb, string enemyName)
            {
                IoCManager.InjectDependencies(this);
                Owner = owner;
                //todo defeat the curse secret game mode
                _fightVerb = fightVerb;
                _enemyName = enemyName;
            }

            private void ValidateVars()
            {
                if(Owner._overflowFlag) return;

                if (_playerHp > _playerHpMax) _playerHp = _playerHpMax;
                if (_playerMp > _playerMpMax) _playerMp = _playerMpMax;
                if (_enemyHp > _enemyHpMax) _enemyHp = _enemyHpMax;
                if (_enemyMp > _enemyMpMax) _enemyMp = _enemyMpMax;
            }

            public void ExecutePlayerAction(PlayerAction action)
            {
                if (!_running) return;

                var actionMessage = "";
                switch (action)
                {
                    case PlayerAction.Attack:
                        var attackAmount = _random.Next(2, 6);
                        actionMessage = $"You attack {_enemyName} for {attackAmount}!";
                        if(!Owner._enemyInvincibilityFlag) _enemyHp -= attackAmount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                    case PlayerAction.Heal:
                        var pointAmount = _random.Next(1, 3);
                        var healAmount = _random.Next(6, 8);
                        actionMessage = $"You use {pointAmount} magic to heal for {healAmount} damage!";
                        if(!Owner._playerInvincibilityFlag) _playerMp -= pointAmount;
                        _playerHp += healAmount;
                        _turtleTracker++;
                        break;
                    case PlayerAction.Recharge:
                        var charge_amount = _random.Next(4, 7);
                        actionMessage = $"You regain {charge_amount} points";
                        _playerMp += charge_amount;
                        _turtleTracker -= _turtleTracker > 0 ? 1 : 0;
                        break;
                }

                CheckGameConditions();

                ValidateVars();
                var enemyActionMessage = ExecuteAiAction();

                if (!CheckGameConditions())
                {
                    _running = false;
                    return;
                }
                ValidateVars();
                UpdateUi(actionMessage, enemyActionMessage);
            }

            private bool CheckGameConditions()
            {
                if ((_enemyHp <= 0 || _enemyMp <= 0) && (_playerHp > 0 && _playerMp > 0))
                {
                    UpdateUi("You won!", $"{_enemyName} dies.");
                    return false;
                }
                if ((_playerHp <= 0 || _playerMp <= 0) && _enemyHp > 0 && _enemyMp > 0)
                {
                    UpdateUi("You lost!", $"{_enemyName} cheers.");
                    return false;
                }
                if ((_playerHp <= 0 || _playerMp <= 0) && (_enemyHp <= 0 || _enemyMp <= 0))
                {
                    UpdateUi("You lost!", $"{_enemyName} dies, but takes you with him.");
                    return false;
                }

                return true;
            }

            private void UpdateUi(string playerActionMessage, string enemyActionMessage)
            {
                Owner.UserInterface?.SendMessage(GenerateUpdateMessage(playerActionMessage, enemyActionMessage));
            }

            private string ExecuteAiAction()
            {
                var actionMessage = "";
                if (_turtleTracker >= 4)
                {
                    var boomAmount = _random.Next(5, 10);
                    actionMessage = $"{_enemyName} throws a bomb, exploding you for {boomAmount} damage!";
                    if (Owner._playerInvincibilityFlag) return actionMessage;
                    _playerHp -= boomAmount;
                    _turtleTracker--;
                }else if (_enemyMp <= 5 && _random.Prob(0.7f))
                {
                    var stealAmount = _random.Next(2, 3);
                    actionMessage = $"{_enemyName} steals {stealAmount} of your power!";
                    if (Owner._playerInvincibilityFlag) return actionMessage;
                    _playerMp -= stealAmount;
                    _enemyMp += stealAmount;
                }else if (_enemyHp <= 10 && _enemyMp > 4)
                {
                    _enemyHp += 4;
                    _enemyMp -= 4;
                    actionMessage = $"{_enemyName} heals for 4 health!";
                }
                else
                {
                    var attackAmount = _random.Next(3, 6);
                    actionMessage = $"{_enemyName} attacks you for {attackAmount} damage!";
                    if (Owner._playerInvincibilityFlag) return actionMessage;
                    _playerHp -= attackAmount;
                }

                return actionMessage;
            }

            public SpaceVillainArcadeMetaDataUpdateMessage GenerateMetaDataMessage()
            {
                return new SpaceVillainArcadeMetaDataUpdateMessage(_playerHp, _playerMp, _enemyHp, _enemyMp, Name);
            }

            public SpaceVillainArcadeDataUpdateMessage
                GenerateUpdateMessage(string playerAction = "", string enemyAction = "")
            {
                return new SpaceVillainArcadeDataUpdateMessage(_playerHp, _playerMp, _enemyHp, _enemyMp, playerAction,
                    enemyAction);
            }
        }
    }
}
