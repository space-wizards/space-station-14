using Content.Shared.Kitchen.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
/// think of grinding as a utility to break an object down into its reagents. Think of juicing as
/// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
/// it contained, juice an apple and get "apple juice".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
[Access(typeof(SharedReagentGrinderSystem))]
public sealed partial class ReagentGrinderComponent : Component
{
    /// <summary>
    /// The container slot id for the beaker
    /// </summary>
    public const string BeakerSlotId = "beakerSlot";

    /// <summary>
    /// The container id for the internal storage.
    /// </summary>
    public const string InputContainerId = "inputContainer";

    /// <summary>
    /// The cached container for the internal storage.
    /// </summary>
    [ViewVariables]
    public Container InputContainer = default!;

    /// <summary>
    /// The amount of entities that fit into the container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StorageMaxEntities = 6;

    /// <summary>
    /// The time grinding or juicing takes.
    /// Roughly matches the grind/juice sounds.
    /// </summary>
    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5);

    /// <summary>
    /// Multiplier for WorkTimer, that pitches the audio accordingly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WorkTimeMultiplier = 1.0f;

    /// <summary>
    /// Sound played when pressing a button on the UI.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));

    /// <summary>
    /// Sound played when grinding.
    /// </summary>
    [DataField]
    public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    /// <summary>
    /// Sound played when juicing.
    /// </summary>
    [DataField]
    public SoundSpecifier JuiceSound = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

    /// <summary>
    /// Grind automatically when inserting items?
    /// </summary>
    [DataField, AutoNetworkedField]
    public GrinderAutoMode AutoMode = GrinderAutoMode.Off;

    /// <summary>
    /// The sound currently being played.
    /// </summary>
    [DataField]
    public EntityUid? AudioStream;

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
}

/// <summary>
/// Marker component for active reagent grinders.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveReagentGrinderComponent : Component;
