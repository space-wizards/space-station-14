using Content.Shared.Damage;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is an entity storage that, when activated, crushes the artifact inside of it and gives artifact fragments.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedArtifactCrusherSystem))]
public sealed partial class ArtifactCrusherComponent : Component
{
    /// <summary>
    /// Whether or not the crusher is currently in the process of crushing something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Crushing;

    /// <summary>
    /// When the current crushing will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan CrushEndTime;

    /// <summary>
    /// The next second. Used to apply damage over time.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan NextSecond;

    /// <summary>
    /// The total duration of the crushing.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan CrushDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A whitelist specifying what items, when crushed, will give fragments.
    /// </summary>
    [DataField]
    public EntityWhitelist CrushingWhitelist = new();

    /// <summary>
    /// The minimum amount of fragments spawned.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int MinFragments = 2;

    /// <summary>
    /// The maximum amount of fragments spawned, non-inclusive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int MaxFragments = 5;

    /// <summary>
    /// The material for the fragments.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StackPrototype> FragmentStackProtoId = "ArtifactFragment";

    /// <summary>
    /// A container used to hold fragments and gibs from crushing.
    /// </summary>
    [ViewVariables]
    public Container OutputContainer;

    /// <summary>
    /// The ID for <see cref="OutputContainer"/>
    /// </summary>
    [DataField]
    public string OutputContainerName = "output_container";

    /// <summary>
    /// Damage dealt each second to entities inside while crushing.
    /// </summary>
    [DataField]
    public DamageSpecifier CrushingDamage = new();

    /// <summary>
    /// Sound played at the end of a successful crush.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? CrushingCompleteSound = new SoundCollectionSpecifier("MetalCrunch");

    /// <summary>
    /// Sound played throughout the entire crushing. Cut off if ended early.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? CrushingSound = new SoundPathSpecifier("/Audio/Effects/hydraulic_press.ogg");

    /// <summary>
    /// Stores entity of <see cref="CrushingSound"/> to allow ending it early.
    /// </summary>
    [DataField]
    public (EntityUid, AudioComponent)? CrushingSoundEntity;

    /// <summary>
    /// When enabled, stops the artifact crusher from being opened when it is being crushed.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool AutoLock = false;
}

[Serializable, NetSerializable]
public enum ArtifactCrusherVisuals : byte
{
    Crushing
}
