using Content.Shared.Disposal.Unit;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDisposalTubeSystem), typeof(SharedDisposalUnitSystem))]
public sealed partial class DisposalEntryComponent : Component
{
    [DataField]
    public EntProtoId HolderPrototypeId = "DisposalHolder";
}
