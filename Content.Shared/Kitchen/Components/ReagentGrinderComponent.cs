using Content.Shared.Kitchen;
using Content.Shared.Kitchen.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components
{
    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
    [Access(typeof(SharedReagentGrinderSystem))]
    public sealed partial class ReagentGrinderComponent : Component
    {
        [DataField, AutoNetworkedField]
        public int StorageMaxEntities = 6;

        [DataField, AutoNetworkedField]
        public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5); // Roughly matches the grind/juice sounds.

        [DataField, AutoNetworkedField]
        public float WorkTimeMultiplier = 1;

        [DataField]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField]
        public SoundSpecifier GrindSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

        [DataField]
        public SoundSpecifier JuiceSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

        [DataField, AutoNetworkedField]
        public GrinderAutoMode AutoMode = GrinderAutoMode.Off;

        /// <summary>
        /// The time the grinder will finish grinding/juicing.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [AutoNetworkedField, AutoPausedField]
        public TimeSpan? EndTime;

        /// <summary>
        /// The currently active program.
        /// </summary>
        [DataField, AutoNetworkedField]
        public GrinderProgram? Program;

        public static string BeakerSlotId = "beakerSlot";

        public static string InputContainerId = "inputContainer";

        /// <summary>
        /// The cached container for the internal storage.
        /// </summary>
        [ViewVariables]
        public Container InputContainer = default!;

        public EntityUid? AudioStream;
    }

    [RegisterComponent, NetworkedComponent]
    public sealed partial class ActiveReagentGrinderComponent : Component;
}
