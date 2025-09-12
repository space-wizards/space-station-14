using Content.Shared.Disposal.Unit;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Attached to entities that are used to insert others into the disposal system.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDisposalTubeSystem), typeof(SharedDisposalUnitSystem))]
public sealed partial class DisposalEntryComponent : Component
{
    /// <summary>
    /// Proto ID of the holder spawned to contain entities that
    /// are inserted into the disposals system.
    /// </summary>
    [DataField]
    public EntProtoId HolderPrototypeId = "DisposalHolder";
}
