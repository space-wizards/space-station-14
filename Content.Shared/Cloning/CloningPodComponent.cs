using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cloning;

[RegisterComponent]
public sealed partial class CloningPodComponent : Component
{
    [ValidatePrototypeId<SinkPortPrototype>]
    public const string PodPort = "CloningPodReceiver";

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// How long the cloning has been going on for.
    /// </summary>
    [ViewVariables]
    public float CloningProgress = 0;

    [ViewVariables]
    public int UsedBiomass = 70;

    [ViewVariables]
    public bool FailedClone = false;

    /// <summary>
    /// The material that is used to clone entities.
    /// </summary>
    [DataField("requiredMaterial"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Biomass";

    /// <summary>
    /// The base amount of time it takes to clone a body
    /// </summary>
    [DataField("baseCloningTime")]
    public float BaseCloningTime = 30f;

    /// <summary>
    /// The multiplier for cloning duration
    /// </summary>
    [DataField("partRatingSpeedMultiplier")]
    public float PartRatingSpeedMultiplier = 0.75f;

    /// <summary>
    /// The machine part that affects cloning speed
    /// </summary>
    [DataField("machinePartCloningSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MachinePartPrototype> MachinePartCloningSpeed = "Manipulator";

    /// <summary>
    /// The current amount of time it takes to clone a body
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float CloningTime = 30f;

    /// <summary>
    /// The mob to spawn on emag
    /// </summary>
    [DataField("mobSpawnId"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId MobSpawnId = "MobAbomination";

    /// <summary>
    /// Emag sound effects.
    /// </summary>
    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };

    // TODO: Remove this from here when cloning and/or zombies are refactored
    [DataField("screamSound")]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("ZombieScreams")
    {
        Params = AudioParams.Default.WithVolume(4),
    };

    /// <summary>
    /// The machine part that affects how much biomass is needed to clone a body.
    /// </summary>
    [DataField("partRatingMaterialMultiplier")]
    public float PartRatingMaterialMultiplier = 0.85f;

    /// <summary>
    /// The current multiplier on the body weight, which determines the
    /// amount of biomass needed to clone.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float BiomassRequirementMultiplier = 1;

    /// <summary>
    /// The machine part that decreases the amount of material needed for cloning
    /// </summary>
    [DataField("machinePartMaterialUse"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MachinePartPrototype> MachinePartMaterialUse = "MatterBin";

    [ViewVariables(VVAccess.ReadWrite)]
    public CloningPodStatus Status;

    [ViewVariables]
    public EntityUid? ConnectedConsole;
}

[Serializable, NetSerializable]
public enum CloningPodVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum CloningPodStatus : byte
{
    Idle,
    Cloning,
    Gore,
    NoMind
}

/// <summary>
/// Raised after a new mob got spawned when cloning a humanoid
/// </summary>
[ByRefEvent]
public struct CloningEvent
{
    public bool NameHandled = false;

    public readonly EntityUid Source;
    public readonly EntityUid Target;

    public CloningEvent(EntityUid source, EntityUid target)
    {
        Source = source;
        Target = target;
    }
}
