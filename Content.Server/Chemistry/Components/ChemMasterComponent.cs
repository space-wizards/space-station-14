using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Contains all the server-side logic for chem masters. See also <see cref="SharedChemMasterComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedChemMasterComponent))]
    public class ChemMasterComponent : SharedChemMasterComponent, IActivate
    {
        [ViewVariables]
        private bool _bufferModeTransfer = true;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private Solution BufferSolution => _bufferSolution ??= EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner.Uid, SolutionName);

        private Solution? _bufferSolution;

        [ViewVariables]
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ChemMasterUiKey.Key);

        [DataField("clickSound")]
        private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");


        /// <summary>
        /// Called once per instance of this component. Gets references to any other components needed
        /// by this component and initializes it's UI and other data.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bufferSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner.Uid, SolutionName);
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage:
                    OnPowerChanged();
                    break;
            }
        }

        private void OnPowerChanged()
        {
            UpdateUserInterface();
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            var msg = (UiActionMessage) obj.Message;
            var needsPower = msg.action switch
            {
                UiAction.Eject => false,
                _ => true,
            };

            if (!PlayerCanUseChemMaster(obj.Session.AttachedEntity, needsPower))
                return;

            switch (msg.action)
            {
                case UiAction.Eject:
                    EntitySystem.Get<ItemSlotsSystem>().TryEjectToHands(OwnerUid, BeakerSlot, obj.Session.AttachedEntityUid);
                    break;
                case UiAction.ChemButton:
                    TransferReagent(msg.id, msg.amount, msg.isBuffer);
                    break;
                case UiAction.Transfer:
                    _bufferModeTransfer = true;
                    UpdateUserInterface();
                    break;
                case UiAction.Discard:
                    _bufferModeTransfer = false;
                    UpdateUserInterface();
                    break;
                case UiAction.CreatePills:
                case UiAction.CreateBottles:
                    TryCreatePackage(obj.Session.AttachedEntity, msg.action, msg.pillAmount, msg.bottleAmount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ClickSound();
        }

        /// <summary>
        /// Checks whether the player entity is able to use the chem master.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <param name="needsPower">whether the device requires power</param>
        /// <returns>Returns true if the entity can use the chem master, and false if it cannot.</returns>
        private bool PlayerCanUseChemMaster(IEntity? playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the chem master
            if (playerEntity == null)
                return false;

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            //Check if player can interact in their current state
            if (!actionBlocker.CanInteract(playerEntity.Uid) || !actionBlocker.CanUse(playerEntity.Uid))
                return false;
            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="SharedChemMasterComponent.ChemMasterBoundUserInterfaceState"/></returns>
        private ChemMasterBoundUserInterfaceState GetUserInterfaceState()
        {
            var beaker = BeakerSlot.Item;
            if (beaker is null || !beaker.TryGetComponent(out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker.Uid, fits.Solution, out var beakerSolution))
            {
                return new ChemMasterBoundUserInterfaceState(Powered, false, FixedPoint2.New(0), FixedPoint2.New(0),
                    "", Owner.Name, new List<Solution.ReagentQuantity>(), BufferSolution.Contents, _bufferModeTransfer,
                    BufferSolution.TotalVolume);
            }

            return new ChemMasterBoundUserInterfaceState(Powered, true, beakerSolution.CurrentVolume,
                beakerSolution.MaxVolume,
                beaker.Name, Owner.Name, beakerSolution.Contents, BufferSolution.Contents, _bufferModeTransfer,
                BufferSolution.TotalVolume);
        }

        public void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        private void TransferReagent(string id, FixedPoint2 amount, bool isBuffer)
        {
            if (!BeakerSlot.HasItem && _bufferModeTransfer) return;
            var beaker = BeakerSlot.Item;

            if (beaker is null || !beaker.TryGetComponent(out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker.Uid, fits.Solution, out var beakerSolution))
                return;

            if (isBuffer)
            {
                foreach (var reagent in BufferSolution.Contents)
                {
                    if (reagent.ReagentId == id)
                    {
                        FixedPoint2 actualAmount;
                        if (
                            amount == FixedPoint2
                                .New(-1)) //amount is FixedPoint2.New(-1) when the client sends a message requesting to remove all solution from the container
                        {
                            actualAmount = FixedPoint2.Min(reagent.Quantity, beakerSolution.AvailableVolume);
                        }
                        else
                        {
                            actualAmount = FixedPoint2.Min(reagent.Quantity, amount, beakerSolution.AvailableVolume);
                        }


                        BufferSolution.RemoveReagent(id, actualAmount);
                        if (_bufferModeTransfer)
                        {
                            EntitySystem.Get<SolutionContainerSystem>()
                                .TryAddReagent(beaker.Uid, beakerSolution, id, actualAmount, out var _);
                            // beakerSolution.Solution.AddReagent(id, actualAmount);
                        }

                        break;
                    }
                }
            }
            else
            {
                foreach (var reagent in beakerSolution.Contents)
                {
                    if (reagent.ReagentId == id)
                    {
                        FixedPoint2 actualAmount;
                        if (amount == FixedPoint2.New(-1))
                        {
                            actualAmount = reagent.Quantity;
                        }
                        else
                        {
                            actualAmount = FixedPoint2.Min(reagent.Quantity, amount);
                        }

                        EntitySystem.Get<SolutionContainerSystem>().TryRemoveReagent(beaker.Uid, beakerSolution, id, actualAmount);
                        BufferSolution.AddReagent(id, actualAmount);
                        break;
                    }
                }
            }

            UpdateUserInterface();
        }

        private void TryCreatePackage(IEntity user, UiAction action, int pillAmount, int bottleAmount)
        {
            if (BufferSolution.TotalVolume == 0)
                return;

            if (action == UiAction.CreateBottles)
            {
                var individualVolume = BufferSolution.TotalVolume / FixedPoint2.New(bottleAmount);
                if (individualVolume < FixedPoint2.New(1))
                    return;

                var actualVolume = FixedPoint2.Min(individualVolume, FixedPoint2.New(30));
                for (int i = 0; i < bottleAmount; i++)
                {
                    var bottle = Owner.EntityManager.SpawnEntity("ChemistryEmptyBottle01", Owner.Transform.Coordinates);

                    var bufferSolution = BufferSolution.SplitSolution(actualVolume);
                    var bottleSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(bottle.Uid, "drink");

                    EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(bottle.Uid, bottleSolution, bufferSolution);

                    //Try to give them the bottle
                    if (user.TryGetComponent<HandsComponent>(out var hands) &&
                        bottle.TryGetComponent<ItemComponent>(out var item))
                    {
                        if (hands.CanPutInHand(item))
                        {
                            hands.PutInHand(item);
                            continue;
                        }
                    }

                    //Put it on the floor
                    bottle.Transform.Coordinates = user.Transform.Coordinates;
                    //Give it an offset
                    bottle.RandomOffset(0.2f);
                }
            }
            else //Pills
            {
                var individualVolume = BufferSolution.TotalVolume / FixedPoint2.New(pillAmount);
                if (individualVolume < FixedPoint2.New(1))
                    return;

                var actualVolume = FixedPoint2.Min(individualVolume, FixedPoint2.New(50));
                for (int i = 0; i < pillAmount; i++)
                {
                    var pill = Owner.EntityManager.SpawnEntity("pill", Owner.Transform.Coordinates);

                    var bufferSolution = BufferSolution.SplitSolution(actualVolume);

                    var pillSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(pill.Uid, "food");
                    EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(pill.Uid, pillSolution, bufferSolution);

                    //Try to give them the bottle
                    if (user.TryGetComponent<HandsComponent>(out var hands) &&
                        pill.TryGetComponent<ItemComponent>(out var item))
                    {
                        if (hands.CanPutInHand(item))
                        {
                            hands.PutInHand(item);
                            continue;
                        }
                    }

                    //Put it on the floor
                    pill.Transform.Coordinates = user.Transform.Coordinates;
                    //Give it an offset
                    pill.RandomOffset(0.2f);
                }
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!args.User.TryGetComponent(out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("chem-master-component-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                UserInterface?.Open(actor.PlayerSession);
            }
        }

        private void ClickSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
