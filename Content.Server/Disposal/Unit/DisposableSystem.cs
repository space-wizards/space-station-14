using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Unit;

/// <inheritdoc/>
public sealed partial class DisposableSystem : SharedDisposableSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    /// <inheritdoc/>
    protected override void ExpelAtmos(Entity<DisposalHolderComponent> ent)
    {
        if (_atmos.GetContainingMixture(ent.Owner, false, true) is { } environment)
        {
            _atmos.Merge(environment, ent.Comp.Air);
            ent.Comp.Air.Clear();
        }
    }
}
