using Content.Shared.Disposal.Unit;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Attached to entities that are used as an entrance into the disposal system.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DisposalTubeSystem), typeof(SharedDisposalUnitSystem))]
public sealed partial class DisposalEntryComponent : Component
{

}
