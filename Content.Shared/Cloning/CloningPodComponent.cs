using Content.Shared.DeviceLinking;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cloning;

/// <summary>
/// Component for cloning pods; manages cloning process, state, and cloning pod interactions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CloningPodComponent : Component
{
    /// <summary>
    /// DeviceLink sink port identifier for this pod.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> PodPort = "CloningPodReceiver";

    /// <summary>
    /// Container slot for a body being cloned.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// How long the cloning has been going on for.
    /// </summary>
    [ViewVariables]
    public float CloningProgress = 0;

    /// <summary>
    /// Amount of biomass used in cloning.
    /// </summary>
    [ViewVariables]
    public int UsedBiomass = 70;

    /// <summary>
    /// Was the clone process failed.
    /// </summary>
    [ViewVariables]
    public bool FailedClone = false;

    /// <summary>
    /// The material that is used to clone entities.
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Biomass";

    /// <summary>
    /// How long it takes to clone a body.
    /// </summary>
    [DataField]
    public TimeSpan CloningTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The mob to spawn on emag.
    /// </summary>
    [DataField]
    public EntProtoId MobSpawnId = "MobAbomination";

    /// <summary>
    /// The sound played when a mob is spawned from an emagged cloning pod.
    /// </summary>
    [DataField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("ZombieScreams")
    {
        Params = AudioParams.Default.WithVolume(4),
    };

    /// <summary>
    /// Status of the cloning pod.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public CloningPodStatus Status;

    /// <summary>
    /// Reference to the connected console entity (if any).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ConnectedConsole;
}

/// <summary>
/// Visual states for the cloning pod (e.g. appearance updates).
/// </summary>
[Serializable, NetSerializable]
public enum CloningPodVisuals : byte
{
    Status
}

/// <summary>
/// Status states of the cloning pod and its process.
/// </summary>
[Serializable, NetSerializable]
public enum CloningPodStatus : byte
{
    Idle,
    Cloning,
    Gore,
    NoMind
}
