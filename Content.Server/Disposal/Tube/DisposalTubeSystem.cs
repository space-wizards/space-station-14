using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Tube;

public sealed partial class DisposalTubeSystem : SharedDisposalTubeSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    protected override void MergeAtmos(Entity<DisposalHolderComponent> ent, GasMixture gasMix)
    {
        _atmos.Merge(ent.Comp.Air, gasMix);
        gasMix.Clear();
    }
}
