using Content.Server.Chemistry.EntitySystems;
using Content.Server.Labels.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

using Robust.Shared.Player;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Contains all the server-side logic for chem masters. See also <see cref="SharedChemMasterComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedChemMasterComponent))]
    public sealed class ChemMasterComponent : SharedChemMasterComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

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

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} player)
                return;

            if (obj.Message is not UiActionMessage msg)
                return;

            if (!PlayerCanUseChemMaster(player, true))
                return;

            switch (msg.Action)
            {
                case UiAction.ChemButton:
                    if (!_bufferModeTransfer)
                    {
                        if (msg.IsBuffer)
                        {
                            DiscardReagent(msg.Id, msg.Amount, BufferSolution);
                        }
                        else if (BeakerSlot.HasItem &&
                                BeakerSlot.Item is {Valid: true} beaker &&
                                _entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) &&
                                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var beakerSolution))
                        {
                            DiscardReagent(msg.Id, msg.Amount, beakerSolution);
                        }
                    }
                    else
                    {
                        TransferReagent(msg.Id, msg.Amount, msg.IsBuffer);
                    }
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

        private void DiscardReagent(string id, FixedPoint2 amount, Solution solution)
        {
            foreach (var reagent in solution.Contents)
            {
                if (reagent.ReagentId == id)
                {
                    FixedPoint2 actualAmount;
                    if (amount == FixedPoint2.New(-1)) //amount is FixedPoint2.New(-1) when the client sends a message requesting to remove all solution from the container
                    {
                        actualAmount = reagent.Quantity;
                    }
                    else
                    {
                        actualAmount = FixedPoint2.Min(reagent.Quantity, amount);
                    }
                    solution.RemoveReagent(id, actualAmount);
                    return;
                }
            }
        }

        private void TransferReagent(string id, FixedPoint2 amount, bool isBuffer)
        {
            if (!BeakerSlot.HasItem ||
                BeakerSlot.Item is not {Valid: true} beaker ||
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
                        EntitySystem.Get<SolutionContainerSystem>()
                            .TryAddReagent(beaker, beakerSolution, id, actualAmount, out var _);
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

            var handSys = _sysMan.GetEntitySystem<SharedHandsSystem>();
            var solSys = _sysMan.GetEntitySystem<SolutionContainerSystem>();

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
                    var bottleSolution = solSys.EnsureSolution(bottle, "drink");

                    solSys.TryAddSolution(bottle, bottleSolution, bufferSolution);

                    //Try to give them the bottle
                    handSys.PickupOrDrop(user, bottle);
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
                    var pill = _entities.SpawnEntity("Pill", _entities.GetComponent<TransformComponent>(Owner).Coordinates);

                    //Adding label
                    LabelComponent labelComponent = pill.EnsureComponent<LabelComponent>();
                    labelComponent.OriginalName = _entities.GetComponent<MetaDataComponent>(pill).EntityName;
                    string val = _entities.GetComponent<MetaDataComponent>(pill).EntityName + $" ({label})";
                    _entities.GetComponent<MetaDataComponent>(pill).EntityName = val;
                    labelComponent.CurrentLabel = label;

                    var bufferSolution = BufferSolution.SplitSolution(actualVolume);
                    var pillSolution = EntitySystem.Get<SolutionContainerSystem>().EnsureSolution(pill, "food");
                    solSys.TryAddSolution(pill, pillSolution, bufferSolution);

                    //Change pill Sprite component state
                    if (!_entities.TryGetComponent(pill, out SpriteComponent? sprite))
                    {
                        return;
                    }
                    sprite?.LayerSetState(0, "pill" + _pillType);

                    //Try to give them the bottle
                    handSys.PickupOrDrop(user, pill);
                }
            }

            if (_bufferSolution?.Contents.Count == 0)
                _label = "";

            UpdateUserInterface();
        }

        private void ClickSound()
        {
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
