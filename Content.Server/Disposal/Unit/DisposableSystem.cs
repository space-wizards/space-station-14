using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Unit;

public sealed partial class DisposableSystem : SharedDisposableSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    protected override void MergeAtmos(Entity<DisposalHolderComponent> ent, GasMixture gasMix)
    {
        if (_atmos.GetContainingMixture(ent.Owner, false, true) is { } environment)
        {
            _atmos.Merge(environment, ent.Comp.Air);
            ent.Comp.Air.Clear();
        }
    }
}
