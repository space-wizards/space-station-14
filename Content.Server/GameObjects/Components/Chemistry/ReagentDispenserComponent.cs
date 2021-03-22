#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry.ReagentDispenser;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers. See also <see cref="SharedReagentDispenserComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class ReagentDispenserComponent : SharedReagentDispenserComponent, IActivate, IInteractUsing, ISolutionChange
    {
        private static ReagentInventoryComparer _comparer = new();

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables] private ContainerSlot _beakerContainer = default!;
        [ViewVariables] [DataField("pack")] private string _packPrototypeId = "";

        [ViewVariables] private bool HasBeaker => _beakerContainer.ContainedEntity != null;
        [ViewVariables] private ReagentUnit _dispenseAmount = ReagentUnit.New(10);
        [UsedImplicitly] [ViewVariables] private SolutionContainerComponent? Solution => _beakerContainer.ContainedEntity?.GetComponent<SolutionContainerComponent>();

        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ReagentDispenserUiKey.Key);

        /// <summary>
        /// Called once per instance of this component. Gets references to any other components needed
        /// by this component and initializes it's UI and other data.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _beakerContainer =
                ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-reagentContainerContainer");

            InitializeFromPrototype();
            UpdateUserInterface();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerChanged(powerChanged);
                    break;
            }
        }

        /// <summary>
        /// Checks to see if the <c>pack</c> defined in this components yaml prototype
        /// exists. If so, it fills the reagent inventory list.
        /// </summary>
        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) return;

            if (!_prototypeManager.TryIndex(_packPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                return;
            }

            foreach (var entry in packPrototype.Inventory)
            {
                Inventory.Add(new ReagentDispenserInventoryEntry(entry));
            }

            Inventory.Sort(_comparer);
        }

        private void OnPowerChanged(PowerChangedMessage e)
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

            var msg = (UiButtonPressedMessage) obj.Message;
            var needsPower = msg.Button switch
            {
                UiButton.Eject => false,
                _ => true,
            };

            if(!PlayerCanUseDispenser(obj.Session.AttachedEntity, needsPower))
                return;

            switch (msg.Button)
            {
                case UiButton.Eject:
                    TryEject(obj.Session.AttachedEntity);
                    break;
                case UiButton.Clear:
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
                case UiButton.SetDispenseAmount15:
                    _dispenseAmount = ReagentUnit.New(15);
                    break;
                case UiButton.SetDispenseAmount20:
                    _dispenseAmount = ReagentUnit.New(20);
                    break;
                case UiButton.SetDispenseAmount25:
                    _dispenseAmount = ReagentUnit.New(25);
                    break;
                case UiButton.SetDispenseAmount30:
                    _dispenseAmount = ReagentUnit.New(30);
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

                    break;
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
        private bool PlayerCanUseDispenser(IEntity? playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == null)
                return false;
            //Check if player can interact in their current state
            if (!ActionBlockerSystem.CanInteract(playerEntity) || !ActionBlockerSystem.CanUse(playerEntity))
                return false;
            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="SharedReagentDispenserComponent.ReagentDispenserBoundUserInterfaceState"/></returns>
        private ReagentDispenserBoundUserInterfaceState GetUserInterfaceState()
        {
            var beaker = _beakerContainer.ContainedEntity;
            if (beaker == null)
            {
                return new ReagentDispenserBoundUserInterfaceState(Powered, false, ReagentUnit.New(0), ReagentUnit.New(0),
                    string.Empty, Inventory, Owner.Name, null, _dispenseAmount);
            }

            var solution = beaker.GetComponent<SolutionContainerComponent>();
            return new ReagentDispenserBoundUserInterfaceState(Powered, true, solution.CurrentVolume, solution.MaxVolume,
                beaker.Name, Inventory, Owner.Name, solution.ReagentList.ToList(), _dispenseAmount);
        }

        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionContainerComponent"/>, eject it.
        /// Tries to eject into user's hands first, then ejects onto dispenser if both hands are full.
        /// </summary>
        private void TryEject(IEntity user)
        {
            if (!HasBeaker)
                return;

            var beaker = _beakerContainer.ContainedEntity;
            if(beaker is null)
                return;

            _beakerContainer.Remove(beaker);
            UpdateUserInterface();

            if(!user.TryGetComponent<HandsComponent>(out var hands) || !beaker.TryGetComponent<ItemComponent>(out var item))
                return;
            if (hands.CanPutInHand(item))
                hands.PutInHand(item);
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionContainerComponent"/>, remove all of it's reagents / solutions.
        /// </summary>
        private void TryClear()
        {
            if (!HasBeaker) return;
            var solution = _beakerContainer.ContainedEntity?.GetComponent<SolutionContainerComponent>();
            if(solution is null)
                return;

            solution.RemoveAllSolution();

            UpdateUserInterface();
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionContainerComponent"/>, attempt to dispense the specified reagent to it.
        /// </summary>
        /// <param name="dispenseIndex">The index of the reagent in <c>Inventory</c>.</param>
        private void TryDispense(int dispenseIndex)
        {
            if (!HasBeaker) return;

            var solution = _beakerContainer.ContainedEntity?.GetComponent<SolutionContainerComponent>();
            if (solution is null)
                return;

            solution.TryAddReagent(Inventory[dispenseIndex].ID, _dispenseAmount, out _);

            UpdateUserInterface();
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!args.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("You have no hands."));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                UserInterface?.Open(actor.playerSession);
            }
        }

        /// <summary>
        /// Called when you click the owner entity with something in your active hand. If the entity in your hand
        /// contains a <see cref="SolutionContainerComponent"/>, if you have hands, and if the dispenser doesn't already
        /// hold a container, it will be added to the dispenser.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        /// <returns></returns>
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (!args.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("You have no hands."));
                return true;
            }

            if (hands.GetActiveHand == null)
            {
                Owner.PopupMessage(args.User, Loc.GetString("You have nothing on your hand."));
                return false;
            }

            var activeHandEntity = hands.GetActiveHand.Owner;
            if (activeHandEntity.TryGetComponent<SolutionContainerComponent>(out var solution))
            {
                if (HasBeaker)
                {
                    Owner.PopupMessage(args.User, Loc.GetString("This dispenser already has a container in it."));
                }
                else if ((solution.Capabilities & SolutionContainerCaps.FitsInDispenser) == 0)
                {
                    //If it can't fit in the dispenser, don't put it in. For example, buckets and mop buckets can't fit.
                    Owner.PopupMessage(args.User, Loc.GetString("That can't fit in the dispenser."));
                }
                else
                {
                    _beakerContainer.Insert(activeHandEntity);
                    UpdateUserInterface();
                }
            }
            else
            {
                Owner.PopupMessage(args.User, Loc.GetString("You can't put this in the dispenser."));
            }

            return true;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) => UpdateUserInterface();

        private void ClickSound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
        }

        [Verb]
        public sealed class EjectBeakerVerb : Verb<ReagentDispenserComponent>
        {
            protected override void GetData(IEntity user, ReagentDispenserComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject Beaker");
                data.Visibility = component.HasBeaker ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, ReagentDispenserComponent component)
            {
                component.TryEject(user);
            }
        }

        private class ReagentInventoryComparer : Comparer<ReagentDispenserInventoryEntry>
        {
            public override int Compare(ReagentDispenserInventoryEntry x, ReagentDispenserInventoryEntry y)
            {
                return string.Compare(x.ID, y.ID, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
