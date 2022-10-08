using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components
{
    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class ReagentGrinderComponent : Component
    {
        public readonly string BeakerSlotId = "ReagentGrinder-reagentContainerContainer";

        /// <summary>
        /// Can be null since we won't always have a beaker in the grinder.
        /// </summary>
        [ViewVariables]
        public Solution? BeakerSolution;

        /// <summary>
        /// Contains the things that are going to be ground or juiced.
        /// </summary>
        [ViewVariables]
        public Container Chamber = default!;

        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        [ViewVariables]
        public bool Busy;

        //YAML serialization vars
        [ViewVariables(VVAccess.ReadWrite)] [DataField("chamberCapacity")] public int StorageCap = 16;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("workTime")] public int WorkTime = 3500; //3.5 seconds, completely arbitrary for now.
        [DataField("clickSound")] public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
        [DataField("grindSound")] public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");
        [DataField("juiceSound")] public SoundSpecifier JuiceSound = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

        [DataField("beakerSlot")]
        public ItemSlot BeakerSlot = new();

        //Visualizer states
        [DataField("emptyState")] public string EmptyState = "juicer0";
        [DataField("beakerState")] public string BeakerState = "juicer1";
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderGrindStartMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderJuiceStartMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderEjectChamberAllMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderEjectChamberContentMessage : BoundUserInterfaceMessage
    {
        public EntityUid EntityId;
        public ReagentGrinderEjectChamberContentMessage(EntityUid entityId)
        {
            EntityId = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderWorkStartedMessage : BoundUserInterfaceMessage
    {
        public GrinderProgram GrinderProgram;
        public ReagentGrinderWorkStartedMessage(GrinderProgram grinderProgram)
        {
            GrinderProgram = grinderProgram;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderWorkCompleteMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum ReagentGrinderVisualState : byte
    {
        BeakerAttached
    }

    [NetSerializable, Serializable]
    public enum ReagentGrinderUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum GrinderProgram : byte
    {
        Grind,
        Juice
    }

    [NetSerializable, Serializable]
    public sealed class ReagentGrinderInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool HasBeakerIn;
        public bool Powered;
        public bool CanJuice;
        public bool CanGrind;
        public EntityUid[] ChamberContents;
        public Solution.ReagentQuantity[]? ReagentQuantities;
        public ReagentGrinderInterfaceState(bool isBusy, bool hasBeaker, bool powered, bool canJuice, bool canGrind, EntityUid[] chamberContents, Solution.ReagentQuantity[]? heldBeakerContents)
        {
            IsBusy = isBusy;
            HasBeakerIn = hasBeaker;
            Powered = powered;
            CanJuice = canJuice;
            CanGrind = canGrind;
            ChamberContents = chamberContents;
            ReagentQuantities = heldBeakerContents;
        }
    }
}
