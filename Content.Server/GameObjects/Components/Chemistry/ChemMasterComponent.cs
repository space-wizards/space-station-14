using System;
using System.Linq;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Contains all the server-side logic for chem masters. See also <see cref="SharedChemMasterComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class ChemMasterComponent : SharedChemMasterComponent, IActivate, IInteractUsing, ISolutionChange
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        [ViewVariables] private BoundUserInterface _userInterface;
        [ViewVariables] private ContainerSlot _beakerContainer;
        [ViewVariables] private string _packPrototypeId;

        [ViewVariables] private bool HasBeaker => _beakerContainer.ContainedEntity != null;
        [ViewVariables] private ReagentUnit _dispenseAmount = ReagentUnit.New(10);

        [ViewVariables]
        //private SolutionComponent BufferSolution => new SolutionComponent();

        private PowerReceiverComponent _powerReceiver;
        private bool Powered => _powerReceiver.Powered;

        private SolutionComponent BufferSolution = new SolutionComponent();


        /// <summary>
        /// Shows the serializer how to save/load this components yaml prototype.
        /// </summary>
        /// <param name="serializer">Yaml serializer</param>
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _packPrototypeId, "pack", string.Empty);
        }

        /// <summary>
        /// Called once per instance of this component. Gets references to any other components needed
        /// by this component and initializes it's UI and other data.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(ChemMasterUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;

            _beakerContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-reagentContainerContainer", Owner);
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();

            //BufferSolution = Owner.BufferSolution
            BufferSolution.Solution = new Solution();
            BufferSolution.MaxVolume = ReagentUnit.New(5000);

            InitializeFromPrototype();
            UpdateUserInterface();
        }

        /// <summary>
        /// Checks to see if the <c>pack</c> defined in this components yaml prototype
        /// exists. If so, it fills the reagent inventory list.
        /// </summary>
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
                //Inventory.Add(new ReagentDispenserInventoryEntry(entry));
            }
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!PlayerCanUseDispenser(obj.Session.AttachedEntity))
                return;

            var msg = (UiActionMessage) obj.Message;
            switch (msg.action)
            {
                case UiAction.Eject:
                    TryEject(obj.Session.AttachedEntity);
                    break;
                case UiAction.ChemButton:
                    TransferReagent(msg.id, msg.amount, msg.isBuffer);
                    break;
                /*case UiButton.Clear:
                    TryClear();
                    break;
                case UiButton.SetDispenseAmount1:
                    _dispenseAmount = ReagentUnit.New(1);
                    break;
                case UiButton.SetDispenseAmount5:
                    _dispenseAmount = ReagentUnit.New(5);
                    break;
                case UiButton.SetDispenseAmount10:
                    _dispenseAmount = ReagentUnit.New(10);
                    break;
                case UiButton.SetDispenseAmount25:
                    _dispenseAmount = ReagentUnit.New(25);
                    break;
                case UiButton.SetDispenseAmount50:
                    _dispenseAmount = ReagentUnit.New(50);
                    break;
                case UiButton.SetDispenseAmount100:
                    _dispenseAmount = ReagentUnit.New(100);
                    break;
                case UiButton.Dispense:
                    if (HasBeaker)
                    {
                        TryDispense(msg.DispenseIndex);
                    }

                    break;*/
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ClickSound();
        }

        /// <summary>
        /// Checks whether the player entity is able to use the chem dispenser.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <returns>Returns true if the entity can use the dispenser, and false if it cannot.</returns>
        private bool PlayerCanUseDispenser(IEntity playerEntity)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == null)
                return false;
            //Check if player can interact in their current state
            if (!ActionBlockerSystem.CanInteract(playerEntity) || !ActionBlockerSystem.CanUse(playerEntity))
                return false;
            //Check if device is powered
            if (!Powered)
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="SharedReagentDispenserComponent.ChemMasterBoundUserInterfaceState"/></returns>
        private ChemMasterBoundUserInterfaceState GetUserInterfaceState()
        {
            var beaker = _beakerContainer.ContainedEntity;
            if (beaker == null)
            {
                return new ChemMasterBoundUserInterfaceState(false, ReagentUnit.New(0), ReagentUnit.New(0),
                    "", Owner.Name, null, BufferSolution.ReagentList.ToList());
            }

            var solution = beaker.GetComponent<SolutionComponent>();
            return new ChemMasterBoundUserInterfaceState(true, solution.CurrentVolume, solution.MaxVolume,
                beaker.Name, Owner.Name, solution.ReagentList.ToList(), BufferSolution.ReagentList.ToList());
        }

        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            _userInterface.SetState(state);
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionComponent"/>, eject it.
        /// Tries to eject into user's hands first, then ejects onto dispenser if both hands are full.
        /// </summary>
        private void TryEject(IEntity user)
        {
            if (!HasBeaker)
                return;

            var beaker = _beakerContainer.ContainedEntity;
            _beakerContainer.Remove(_beakerContainer.ContainedEntity);
            UpdateUserInterface();

            if(!user.TryGetComponent<HandsComponent>(out var hands) || !beaker.TryGetComponent<ItemComponent>(out var item))
                return;
            if (hands.CanPutInHand(item))
                hands.PutInHand(item);
        }

        private void TransferReagent(string id, ReagentUnit amount, bool isBuffer)
        {
            if (!HasBeaker) return;
            var beaker = _beakerContainer.ContainedEntity;
            var beakerSolution = beaker.GetComponent<SolutionComponent>();
            if (isBuffer)
            {
                foreach (var reagent in BufferSolution.Solution.Contents)
                {
                    //var name = _localizationManager.GetString("Unknown reagent");
                    //Try to the prototype for the given reagent. This gives us it's name.

                    if (reagent.ReagentId == id)
                    {
                        ReagentUnit actualAmount;
                        if (amount == ReagentUnit.New(-1))
                        {
                            actualAmount = ReagentUnit.Min(reagent.Quantity, beakerSolution.EmptyVolume);
                        }
                        else
                        {
                            actualAmount = ReagentUnit.Min(reagent.Quantity, amount, beakerSolution.EmptyVolume);
                        }

                        BufferSolution.Solution.RemoveReagent(id, actualAmount);
                        beakerSolution.Solution.AddReagent(id, actualAmount);
                        break;
                    }

                }
            }
            else
            {
                foreach (var reagent in beakerSolution.Solution.Contents)
                {
                    //var name = _localizationManager.GetString("Unknown reagent");
                    //Try to the prototype for the given reagent. This gives us it's name.

                    if (reagent.ReagentId == id)
                    {
                        ReagentUnit actualAmount;
                        if (amount == ReagentUnit.New(-1))
                        {
                            actualAmount = ReagentUnit.Min(reagent.Quantity, BufferSolution.EmptyVolume);
                        }
                        else
                        {
                            actualAmount = ReagentUnit.Min(reagent.Quantity, amount, BufferSolution.EmptyVolume);
                        }
                        beakerSolution.TryRemoveReagent(id, actualAmount);
                        BufferSolution.Solution.AddReagent(id, actualAmount);
                        break;
                    }

                }
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionComponent"/>, remove all of it's reagents / solutions.
        /// </summary>
        private void TryClear()
        {
            if (!HasBeaker) return;
            var solution = _beakerContainer.ContainedEntity.GetComponent<SolutionComponent>();
            solution.RemoveAllSolution();

            UpdateUserInterface();
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionComponent"/>, attempt to dispense the specified reagent to it.
        /// </summary>
        /// <param name="dispenseIndex">The index of the reagent in <c>Inventory</c>.</param>
        private void TryDispense(int dispenseIndex)
        {
            if (!HasBeaker) return;

            //var solution = _beakerContainer.ContainedEntity.GetComponent<SolutionComponent>();
            //solution.TryAddReagent(Inventory[dispenseIndex].ID, _dispenseAmount, out _);

            UpdateUserInterface();
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            if (!args.User.TryGetComponent(out IHandsComponent hands))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User,
                    _localizationManager.GetString("You have no hands."));
                return;
            }

            if (!Powered)
                return;

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                _userInterface.Open(actor.playerSession);
            }
        }

        /// <summary>
        /// Called when you click the owner entity with something in your active hand. If the entity in your hand
        /// contains a <see cref="SolutionComponent"/>, if you have hands, and if the dispenser doesn't already
        /// hold a container, it will be added to the dispenser.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        /// <returns></returns>
        bool IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (!args.User.TryGetComponent(out IHandsComponent hands))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User,
                    _localizationManager.GetString("You have no hands."));
                return true;
            }

            var activeHandEntity = hands.GetActiveHand.Owner;
            if (activeHandEntity.TryGetComponent<SolutionComponent>(out var solution))
            {
                if (HasBeaker)
                {
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User,
                        _localizationManager.GetString("This dispenser already has a container in it."));
                }
                else if ((solution.Capabilities & SolutionCaps.FitsInDispenser) == 0)
                {
                    //If it can't fit in the dispenser, don't put it in. For example, buckets and mop buckets can't fit.
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User,
                        _localizationManager.GetString("That can't fit in the dispenser."));
                }
                else
                {
                    _beakerContainer.Insert(activeHandEntity);
                    UpdateUserInterface();
                }
            }
            else
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, args.User,
                    _localizationManager.GetString("You can't put this in the dispenser."));
            }

            return true;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => UpdateUserInterface();

        private void ClickSound()
        {

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));

        }


    }
}
