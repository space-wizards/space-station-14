using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using NetCord;
using Robust.Server.GameStates;

namespace Content.Server.Disposal.Tube;

/// <inheritdoc/>
public sealed partial class DisposalTubeSystem : SharedDisposalTubeSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    /// <inheritdoc/>
    protected override void IntakeAtmos(Entity<DisposalHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        _atmos.Merge(ent.Comp.Air, unit.Comp.Air);
        unit.Comp.Air.Clear();
    }

    /// <inheritdoc/>
    protected override void AddPVSOverride(Entity<DisposalHolderComponent> ent)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
