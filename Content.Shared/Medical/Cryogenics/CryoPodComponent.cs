using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Cryogenics;

[RegisterComponent, NetworkedComponent]
public sealed partial class CryoPodComponent : Component
{
    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("port")]
    public string PortName { get; set; } = "port";

    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solutionContainerName")]
    public string SolutionContainerName { get; set; } = "beakerSlot";

    /// <summary>
    /// How often (seconds) are chemicals transferred from the beaker to the body?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("beakerTransferTime")]
    public float BeakerTransferTime = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("nextInjectionTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? NextInjectionTime;

    /// <summary>
    /// How many units to transfer per tick from the beaker to the mob?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("beakerTransferAmount")]
    public float BeakerTransferAmount = 1f;

    /// <summary>
    ///     Delay applied when inserting a mob in the pod.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("entryDelay")]
    public float EntryDelay = 2f;

    /// <summary>
    /// Delay applied when trying to pry open a locked pod.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pryDelay")]
    public float PryDelay = 5f;

    /// <summary>
    /// Container for mobs inserted in the pod.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// If true, the eject verb will not work on the pod and the user must use a crowbar to pry the pod open.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("locked")]
    public bool Locked { get; set; }

    /// <summary>
    /// Causes the pod to be locked without being fixable by messing with wires.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permaLocked")]
    public bool PermaLocked { get; set; }

    [Serializable, NetSerializable]
    public enum CryoPodVisuals : byte
    {
        ContainsEntity,
        IsOn
    }
}
