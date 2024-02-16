using Content.Shared.DeviceLinking;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    /// The current amount of time it takes to clone a body
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
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
