using System;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IAttackBy))]
    public class ReagentDispenserComponent : SharedReagentDispenserComponent, IActivate, IAttackBy
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private ContainerSlot _beakerContainer;
        private string _packPrototypeId;

        public bool HasBeaker => _beakerContainer.ContainedEntity != null;
        public int DispenseAmount = 10;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _packPrototypeId, "pack", string.Empty);
        }

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ReagentDispenserUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;

            _beakerContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-reagentContainerContainer", Owner);

            InitializeFromPrototype();
            UpdateUserInterface();
        }

        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) return;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex(_packPrototypeId, out ReagentDispenserInventoryPrototype packPrototype))
            {
                return;
            }

            foreach (var entry in packPrototype.Inventory)
            {
                Inventory.Add(new ReagentDispenserInventoryEntry(entry));
            }
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (UiButtonPressedMessage)obj.Message;
            switch (msg.Button)
            {
                case UiButton.Eject:
                    TryEject();
                    break;
                case UiButton.Clear:
                    TryClear();
                    break;
                case UiButton.SetDispenseAmount1:
                    DispenseAmount = 1;
                    break;
                case UiButton.SetDispenseAmount5:
                    DispenseAmount = 5;
                    break;
                case UiButton.SetDispenseAmount10:
                    DispenseAmount = 10;
                    break;
                case UiButton.SetDispenseAmount25:
                    DispenseAmount = 25;
                    break;
                case UiButton.SetDispenseAmount50:
                    DispenseAmount = 50;
                    break;
                case UiButton.SetDispenseAmount100:
                    DispenseAmount = 100;
                    break;
                case UiButton.Dispense:
                    if (HasBeaker)
                    {
                        TryDispense(msg.DispenseIndex, obj.Session.AttachedEntity);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ReagentDispenserBoundUserInterfaceState GetUserInterfaceState()
        {
            var beaker = _beakerContainer.ContainedEntity;
            if (beaker == null)
            {
                return new ReagentDispenserBoundUserInterfaceState(false, 0,0, "", Inventory, Owner.Name, null);
            }

            var solution = beaker.GetComponent<SolutionComponent>();
            return new ReagentDispenserBoundUserInterfaceState(true, solution.CurrentVolume, solution.MaxVolume, beaker.Name,
                Inventory, Owner.Name, solution.ReagentList.ToList());
        }

        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            _userInterface.SetState(state);
        }

        private void TryEject()
        {
            if(!HasBeaker) return;
            _beakerContainer.Remove(_beakerContainer.ContainedEntity);

            UpdateUserInterface();
        }

        private void TryClear()
        {
            if (!HasBeaker) return;
            var solution = _beakerContainer.ContainedEntity.GetComponent<SolutionComponent>();
            solution.RemoveAllSolution();

            UpdateUserInterface();
        }

        private void TryDispense(int dispenseIndex, IEntity user)
        {
            var solution = _beakerContainer.ContainedEntity.GetComponent<SolutionComponent>();
            solution.TryAddReagent(Inventory[dispenseIndex].ID, DispenseAmount, out int acceptedQuantity);

            UpdateUserInterface();
        }

        //Called when you click it with an empty hand
        public void Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            if (!args.User.TryGetComponent(out IHandsComponent hands))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User, _localizationManager.GetString("You have no hands."));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                _userInterface.Open(actor.playerSession);
            }
        }

        //Calls when you click it with something in your hand
        public bool AttackBy(AttackByEventArgs args)
        {
            if (!args.User.TryGetComponent(out IHandsComponent hands))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User, _localizationManager.GetString("You have no hands."));
                return true;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity?.HasComponent<SolutionComponent>() == true)
            {
                if (HasBeaker)
                {
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User, _localizationManager.GetString("This dispenser already has a container in it."));
                }
                else
                {
                    _beakerContainer.Insert(activeHandEntity);
                    UpdateUserInterface();
                }
            }
            else
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User, _localizationManager.GetString("You can't put this in the dispenser."));
            }

            return true;
        }
    }
}
