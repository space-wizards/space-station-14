using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers. See also <see cref="SharedReagentDispenserComponent"/>.
    /// This includes initializing the component based on prototype data, and sending and receiving messages from the client.
    /// Messages sent to the client are used to update update the user interface for a component instance.
    /// Messages sent from the client are used to handle ui button presses.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedReagentDispenserComponent))]
    public sealed class ReagentDispenserComponent : SharedReagentDispenserComponent
    {
        private static ReagentInventoryComparer _comparer = new();
        public static string SolutionName = "reagent";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        [ViewVariables] [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))] private string _packPrototypeId = "";

        [ViewVariables] [DataField("emagPack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))] public string EmagPackPrototypeId = "";

        public bool AlreadyEmagged = false;

        [DataField("clickSound")]
        private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables] private FixedPoint2 _dispenseAmount = FixedPoint2.New(10);

        [UsedImplicitly]
        [ViewVariables]
        private Solution? Solution
        {
            get
            {
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution);
                return solution;
            }
        }

        [ViewVariables]
        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ReagentDispenserUiKey.Key);

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

            InitializeFromPrototype();
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

        public void AddFromPrototype(string pack)
        {
            if (string.IsNullOrEmpty(pack)) return;

            if (!_prototypeManager.TryIndex(pack, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                return;
            }

            foreach (var entry in packPrototype.Inventory)
            {
                Inventory.Add(new ReagentDispenserInventoryEntry(entry));
            }

            Inventory.Sort(_comparer);
        }


        public void OnPowerChanged()
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

            if (obj.Message is not UiButtonPressedMessage msg )
                return;

            if (!PlayerCanUseDispenser(obj.Session.AttachedEntity, true))
                return;

            switch (msg.Button)
            {
                case UiButton.Clear:
                    TryClear();
                    break;
                case UiButton.SetDispenseAmount1:
                    _dispenseAmount = FixedPoint2.New(1);
                    break;
                case UiButton.SetDispenseAmount5:
                    _dispenseAmount = FixedPoint2.New(5);
                    break;
                case UiButton.SetDispenseAmount10:
                    _dispenseAmount = FixedPoint2.New(10);
                    break;
                case UiButton.SetDispenseAmount15:
                    _dispenseAmount = FixedPoint2.New(15);
                    break;
                case UiButton.SetDispenseAmount20:
                    _dispenseAmount = FixedPoint2.New(20);
                    break;
                case UiButton.SetDispenseAmount25:
                    _dispenseAmount = FixedPoint2.New(25);
                    break;
                case UiButton.SetDispenseAmount30:
                    _dispenseAmount = FixedPoint2.New(30);
                    break;
                case UiButton.SetDispenseAmount50:
                    _dispenseAmount = FixedPoint2.New(50);
                    break;
                case UiButton.SetDispenseAmount100:
                    _dispenseAmount = FixedPoint2.New(100);
                    break;
                case UiButton.Dispense:
                    if (BeakerSlot.HasItem)
                    {
                        TryDispense(msg.DispenseIndex);
                        // Ew
                        if (BeakerSlot.Item != null)
                            _adminLogger.Add(LogType.ChemicalReaction, LogImpact.Medium,
                                $"{_entities.ToPrettyString(obj.Session.AttachedEntity.Value):player} dispensed {_dispenseAmount}u of {Inventory[msg.DispenseIndex].ID} into {_entities.ToPrettyString(BeakerSlot.Item.Value):entity}");
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
        private bool PlayerCanUseDispenser(EntityUid? playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == null)
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
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution))
            {
                return new ReagentDispenserBoundUserInterfaceState(Powered, false, FixedPoint2.New(0),
                    FixedPoint2.New(0),
                    string.Empty, Inventory, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, null, _dispenseAmount);
            }

            return new ReagentDispenserBoundUserInterfaceState(Powered, true, solution.CurrentVolume,
                solution.MaxVolume,
                _entities.GetComponent<MetaDataComponent>(beaker).EntityName, Inventory, _entities.GetComponent<MetaDataComponent>(Owner).EntityName, solution.Contents.ToList(), _dispenseAmount);
        }

        public void UpdateUserInterface()
        {
            if (!Initialized) return;

            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionHolder"/>, remove all of it's reagents / solutions.
        /// </summary>
        private void TryClear()
        {
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution))
                return;

            EntitySystem.Get<SolutionContainerSystem>().RemoveAllSolution(beaker, solution);

            UpdateUserInterface();
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionHolder"/>, attempt to dispense the specified reagent to it.
        /// </summary>
        /// <param name="dispenseIndex">The index of the reagent in <c>Inventory</c>.</param>
        private void TryDispense(int dispenseIndex)
        {
            if (BeakerSlot.Item is not {Valid: true} beaker ||
                !_entities.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
                !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(beaker, fits.Solution, out var solution)) return;

            EntitySystem.Get<SolutionContainerSystem>()
                .TryAddReagent(beaker, solution, Inventory[dispenseIndex].ID, _dispenseAmount, out _);

            UpdateUserInterface();
        }

        private void ClickSound()
        {
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
        }

        private sealed class ReagentInventoryComparer : Comparer<ReagentDispenserInventoryEntry>
        {
            public override int Compare(ReagentDispenserInventoryEntry x, ReagentDispenserInventoryEntry y)
            {
                return string.Compare(x.ID, y.ID, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
