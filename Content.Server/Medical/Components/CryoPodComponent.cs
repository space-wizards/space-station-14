using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Containers;

namespace Content.Server.Medical.Components;

[RegisterComponent]
public sealed class CryoPodComponent: Component, IGasMixtureHolder
{
    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("port")]
    public string PortName { get; set; } = "port";

    /// <summary>
    /// Local air buffer that will be mixed with the pipenet, if one exists, per tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gasMixture")]
    public GasMixture Air { get; set; } = new(Atmospherics.OneAtmosphere);

    /// <summary>
    /// Container for mobs inserted in the pod.
    /// </summary>
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solutionContainerName")]
    public string SolutionContainerName { get; set; } = "beakerSlot";

    /// <summary>
    /// Accumulator used to keep track of when to inject chemicals from the inserted beaker to the mob inside the pod.
    /// </summary>
    [DataField("accumulator")]
    public float Accumulator = 0f;

    /// <summary>
    /// How often (seconds) are chemicals transferred from the beaker to the body?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("beakerTransferTime")]
    public float BeakerTransferTime = 1f;

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

    public bool IsPrying { get; set; }
}
