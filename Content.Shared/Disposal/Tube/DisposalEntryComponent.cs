using Content.Shared.Disposal.Unit;

namespace Content.Shared.Disposal.Tube.Components;

[RegisterComponent]
[Access(typeof(SharedDisposalTubeSystem), typeof(SharedDisposalUnitSystem))]
public sealed partial class DisposalEntryComponent : Component
{
    public const string HolderPrototypeId = "DisposalHolder";
}
