using System.Collections.Generic;
using Content.Server.Coordinates.Helpers;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public class CargoConsoleComponent : SharedCargoConsoleComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private CargoBankAccount? _bankAccount;

        [ViewVariables]
        public CargoBankAccount? BankAccount
        {
            get => _bankAccount;
            private set
            {
                if (_bankAccount == value)
                {
                    return;
                }

                if (_bankAccount != null)
                {
                    _bankAccount.OnBalanceChange -= UpdateUIState;
                }

                _bankAccount = value;

                if (value != null)
                {
                    value.OnBalanceChange += UpdateUIState;
                }

                UpdateUIState();
            }
        }

        [DataField("requestOnly")]
        private bool _requestOnly = false;

        [DataField("errorSound")]
        private SoundSpecifier _errorSound = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;
        private CargoConsoleSystem _cargoConsoleSystem = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CargoConsoleUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out GalacticMarketComponent _);
            Owner.EnsureComponentWarn(out CargoOrderDatabaseComponent _);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            _cargoConsoleSystem = EntitySystem.Get<CargoConsoleSystem>();
            BankAccount = _cargoConsoleSystem.StationAccount;
        }

        protected override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnOnReceiveMessage;
            }

            base.OnRemove();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!_entMan.TryGetComponent(Owner, out CargoOrderDatabaseComponent? orders))
            {
                return;
            }

            var message = serverMsg.Message;
            if (orders.Database == null)
                return;
            if (!Powered)
                return;
            switch (message)
            {
                case CargoConsoleAddOrderMessage msg:
                {
                    if (msg.Amount <= 0 || _bankAccount == null)
                    {
                        break;
                    }

                    if (!_cargoConsoleSystem.AddOrder(orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId,
                        msg.Amount, _bankAccount.Id))
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), _errorSound.GetSound(), Owner, AudioParams.Default);
                    }
                    break;
                }
                case CargoConsoleRemoveOrderMessage msg:
                {
                    _cargoConsoleSystem.RemoveOrder(orders.Database.Id, msg.OrderNumber);
                    break;
                }
                case CargoConsoleApproveOrderMessage msg:
                {
                    if (_requestOnly ||
                        !orders.Database.TryGetOrder(msg.OrderNumber, out var order) ||
                        _bankAccount == null)
                    {
                        break;
                    }

                    if (msg.Session.AttachedEntity is not {Valid: true} player)
                        break;

                    PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype? product);
                    if (product == null!)
                        break;
                    var capacity = _cargoConsoleSystem.GetCapacity(orders.Database.Id);
                    if (
                        (capacity.CurrentCapacity == capacity.MaxCapacity
                        || capacity.CurrentCapacity + order.Amount > capacity.MaxCapacity
                        || !_cargoConsoleSystem.CheckBalance(_bankAccount.Id, (-product.PointCost) * order.Amount)
                        || !_cargoConsoleSystem.ApproveOrder(Owner, player, orders.Database.Id, msg.OrderNumber)
                        || !_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                        )
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), _errorSound.GetSound(), Owner, AudioParams.Default);
                        break;
                    }

                    UpdateUIState();
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    //var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(orders.Database);
                    //orders.Database.ClearOrderCapacity();

                    // TODO replace with shuttle code
                    // TEMPORARY loop for spawning stuff on telepad (looks for a telepad adjacent to the console)
                    EntityUid? cargoTelepad = null;
                    var indices = _entMan.GetComponent<TransformComponent>(Owner).Coordinates.ToVector2i(_entMan, _mapManager);
                    var offsets = new Vector2i[] { new Vector2i(0, 1), new Vector2i(1, 1), new Vector2i(1, 0), new Vector2i(1, -1),
                                                   new Vector2i(0, -1), new Vector2i(-1, -1), new Vector2i(-1, 0), new Vector2i(-1, 1), };
                    var adjacentEntities = new List<IEnumerable<EntityUid>>(); //Probably better than IEnumerable.concat
                    foreach (var offset in offsets)
                    {
                        adjacentEntities.Add((indices+offset).GetEntitiesInTileFast(_entMan.GetComponent<TransformComponent>(Owner).GridID));
                    }

                    foreach (var enumerator in adjacentEntities)
                    {
                        foreach (EntityUid entity in enumerator)
                        {
                            if (_entMan.HasComponent<CargoTelepadComponent>(entity) && _entMan.TryGetComponent<ApcPowerReceiverComponent?>(entity, out var powerReceiver) && powerReceiver.Powered)
                            {
                                cargoTelepad = entity;
                                break;
                            }
                        }
                    }
                    if (cargoTelepad != null)
                    {
                        if (_entMan.TryGetComponent<CargoTelepadComponent?>(cargoTelepad.Value, out var telepadComponent))
                        {
                            var approvedOrders = _cargoConsoleSystem.RemoveAndGetApprovedOrders(orders.Database.Id);
                            orders.Database.ClearOrderCapacity();
                            foreach (var order in approvedOrders)
                            {
                                telepadComponent.QueueTeleport(order);
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateUIState()
        {
            if (_bankAccount == null || !_entMan.EntityExists(Owner))
            {
                return;
            }

            var id = _bankAccount.Id;
            var name = _bankAccount.Name;
            var balance = _bankAccount.Balance;
            var capacity = _cargoConsoleSystem.GetCapacity(id);
            UserInterface?.SetState(new CargoConsoleInterfaceState(_requestOnly, id, name, balance, capacity));
        }
    }
}
