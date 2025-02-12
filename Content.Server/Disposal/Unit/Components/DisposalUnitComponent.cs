using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;

namespace Content.Server.Disposal.Unit.Components;

// GasMixture life.
[RegisterComponent]
public sealed partial class DisposalUnitComponent : SharedDisposalUnitComponent
{
    [DataField]
    public GasMixture Air = new(Atmospherics.CellVolume);
}
