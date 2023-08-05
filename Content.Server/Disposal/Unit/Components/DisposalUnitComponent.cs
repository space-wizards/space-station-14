using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;

namespace Content.Server.Disposal.Unit.Components;

// GasMixture life.
[RegisterComponent]
[ComponentReference(typeof(SharedDisposalUnitComponent))]
public sealed class DisposalUnitComponent : SharedDisposalUnitComponent
{
    [DataField("air")]
    public GasMixture Air = new(Atmospherics.CellVolume);
}
