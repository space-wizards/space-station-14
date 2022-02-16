using System;
using System.Collections.Generic;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Labels.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables]
        private uint _pillType = 1;

        [ViewVariables]
        private string _label = "";

        [ViewVariables]
        private bool _bufferModeTransfer = true;

        [ViewVariables]
        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private Solution BufferSolution => _bufferSolution ??= EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner, SolutionName);

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

            _bufferSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(Owner, SolutionName);
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
            if (obj.Session.AttachedEntity is not {Valid: true} player)
                return;

            var msg = (UiActionMessage) obj.Message;
            var needsPower = msg.Action switch
            {
                UiAction.Eject => false,
                _ => true,
            };

            if (!PlayerCanUseChemMaster(player, needsPower))
                return;

            switch (msg.Action)
            {
                case UiAction.Eject:
                    EntitySystem.Get<ItemSlotsSystem>().TryEjectToHands(Owner, BeakerSlot, player);
                    break;
                case UiAction.ChemButton:
                    TransferReagent(msg.Id, msg.Amount, msg.IsBuffer);
                    break;
                case UiAction.Transfer:
                    _bufferModeTransfer = true;
                    UpdateUserInterface();
                    break;
                case UiAction.Discard:
                    _bufferModeTransfer = false;
                    UpdateUserInterface();
                    break;
                case UiAction.SetPillType:
                    _pillType = msg.PillType;
                    UpdateUserInterface();
                    break;
                case UiAction.CreatePills:
                case UiAction.CreateBottles:
                    _label = msg.Label;
                    TryCreatePackage(player, msg.Action, msg.Label, msg.PillAmount, msg.BottleAmount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            UpdateUserInterface();
            ClickSound();
        }

        /// <summary>
        /// Checks whether the player entity is able to use the chem master.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <param name="needsPower">whether the device requires power</param>
        /// <returns>Returns true if the entity can use the chem master, and false if it cannot.</returns>
        private bool PlayerCanUseChemMaster(EntityUid playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the chem master
            if (playerEntity == default)
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
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var beakerSolution))
            {
                return new ChemMasterBoundUserInterfaceState(Powered, false, FixedPoint2.New(0), FixedPoint2.New(0),
                    "", _label, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, new List<Solution.ReagentQuantity>(), BufferSolution.Contents, _bufferModeTransfer,
                    BufferSolution.TotalVolume, _pillType);
            }

            return new ChemMasterBoundUserInterfaceState(Powered, true, beakerSolution.CurrentVolume,
                beakerSolution.MaxVolume,
                _entities.GetComponent<MetaDataComponent>(beaker).EntityName, _label, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, beakerSolution.Contents, BufferSolution.Contents, _bufferModeTransfer,
                BufferSolution.TotalVolume, _pillType);
        }

        public void UpdateUserInterface()
        {
            if (!Initialized) return;

            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        private void TransferReagent(string id, FixedPoint2 amount, bool isBuffer)
        {
            if (!BeakerSlot.HasItem && _bufferModeTransfer)
                return;

            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var beakerSolution))
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
                                .TryAddReagent(beaker, beakerSolution, id, actualAmount, out var _);
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

                        EntitySystem.Get<SolutionContainerSystem>().TryRemoveReagent(beaker, beakerSolution, id, actualAmount);
                        BufferSolution.AddReagent(id, actualAmount);
                        break;
                    }
                }
            }
            _label = GenerateLabel();
            UpdateUserInterface();
        }

        /// <summary>
        /// Handles label generation depending from solutions and their amount.
        /// Label is generated by taking the most significant solution name.
        /// </summary>
        private string GenerateLabel()
        {
            if (_bufferSolution == null || _bufferSolution.Contents.Count == 0)
                return "";

            _bufferSolution.Contents.Sort();
            return _bufferSolution.Contents[_bufferSolution.Contents.Count - 1].ReagentId;
        }

        private void TryCreatePackage(EntityUid user, UiAction action, string label, int pillAmount, int bottleAmount)
        {
            if (BufferSolution.TotalVolume == 0)
            {
                user.PopupMessageCursor(Loc.GetString("chem-master-window-buffer-empty-text"));
                return;
            }

            if (action == UiAction.CreateBottles)
            {
                var individualVolume = BufferSolution.TotalVolume / FixedPoint2.New(bottleAmount);
                if (individualVolume < FixedPoint2.New(1))
                {
                    user.PopupMessageCursor(Loc.GetString("chem-master-window-buffer-low-text"));
                    return;
                }

                var actualVolume = FixedPoint2.Min(individualVolume, FixedPoint2.New(30));
                for (int i = 0; i < bottleAmount; i++)
                {
                    var bottle = _entities.SpawnEntity("ChemistryEmptyBottle01", _entities.GetComponent<TransformComponent>(Owner).Coordinates);

                    //Adding label
                    LabelComponent labelComponent = bottle.EnsureComponent<LabelComponent>();
                    labelComponent.OriginalName = _entities.GetComponent<MetaDataComponent>(bottle).EntityName;
                    string val = _entities.GetComponent<MetaDataComponent>(bottle).EntityName + $" ({label})";
                    _entities.GetComponent<MetaDataComponent>(bottle).EntityName = val;
                    labelComponent.CurrentLabel = label;

                    var bufferSolution = BufferSolution.SplitSolution(actualVolume);
                    var bottleSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(bottle, "drink");

                    EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(bottle, bottleSolution, bufferSolution);

                    //Try to give them the bottle
                    if (_entities.TryGetComponent<HandsComponent?>(user, out var hands) &&
                        _entities.TryGetComponent<SharedItemComponent?>(bottle, out var item))
                    {
                        if (hands.CanPutInHand(item))
                        {
                            hands.PutInHand(item);
                            continue;
                        }
                    }

                    //Put it on the floor
                    _entities.GetComponent<TransformComponent>(bottle).Coordinates = _entities.GetComponent<TransformComponent>(user).Coordinates;
                    //Give it an offset
                    bottle.RandomOffset(0.2f);
                }
            }
            else //Pills
            {
                var individualVolume = BufferSolution.TotalVolume / FixedPoint2.New(pillAmount);
                if (individualVolume < FixedPoint2.New(1))
                {
                    user.PopupMessageCursor(Loc.GetString("chem-master-window-buffer-low-text"));
                    return;
                }

                var actualVolume = FixedPoint2.Min(individualVolume, FixedPoint2.New(50));
                for (int i = 0; i < pillAmount; i++)
                {
                    var pill = _entities.SpawnEntity("pill", _entities.GetComponent<TransformComponent>(Owner).Coordinates);

                    //Adding label
                    LabelComponent labelComponent = pill.EnsureComponent<LabelComponent>();
                    labelComponent.OriginalName = _entities.GetComponent<MetaDataComponent>(pill).EntityName;
                    string val = _entities.GetComponent<MetaDataComponent>(pill).EntityName + $" ({label})";
                    _entities.GetComponent<MetaDataComponent>(pill).EntityName = val;
                    labelComponent.CurrentLabel = label;

                    var bufferSolution = BufferSolution.SplitSolution(actualVolume);
                    var pillSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(pill, "food");
                    EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(pill, pillSolution, bufferSolution);

                    //Change pill Sprite component state
                    if (!_entities.TryGetComponent(pill, out SpriteComponent? sprite))
                    {
                        return;
                    }
                    sprite?.LayerSetState(0, "pill" + _pillType);

                    //Try to give them the bottle
                    if (_entities.TryGetComponent<HandsComponent?>(user, out var hands) &&
                        _entities.TryGetComponent<SharedItemComponent?>(pill, out var item))
                    {
                        if (hands.CanPutInHand(item))
                        {
                            hands.PutInHand(item);
                            continue;
                        }
                    }

                    //Put it on the floor
                    _entities.GetComponent<TransformComponent>(pill).Coordinates = _entities.GetComponent<TransformComponent>(user).Coordinates;
                    //Give it an offset
                    pill.RandomOffset(0.2f);
                }
            }

            if (_bufferSolution?.Contents.Count == 0)
                _label = "";

            UpdateUserInterface();
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!_entities.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!_entities.TryGetComponent(args.User, out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("chem-master-component-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.GetActiveHandItem?.Owner;
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
