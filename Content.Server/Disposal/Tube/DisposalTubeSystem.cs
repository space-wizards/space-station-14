using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Tube;

/// <inheritdoc/>
public sealed partial class DisposalTubeSystem : SharedDisposalTubeSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    /// <inheritdoc/>
    protected override void IntakeAtmos(Entity<DisposalHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        _atmos.Merge(ent.Comp.Air, unit.Comp.Air);
        unit.Comp.Air.Clear();
    }
}
