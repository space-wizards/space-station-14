using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Holder;

/// <inheritdoc/>
public sealed partial class DisposalHolderSystem : SharedDisposalHolderSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void TransferAtmos(Entity<DisposalHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        _atmos.Merge(ent.Comp.Air, unit.Comp.Air);
        unit.Comp.Air.Clear();
    }

    /// <inheritdoc/>
    protected override void ExpelAtmos(Entity<DisposalHolderComponent> ent)
    {
        if (_atmos.GetContainingMixture(ent.Owner, false, true) is { } environment)
        {
            _atmos.Merge(environment, ent.Comp.Air);
            ent.Comp.Air.Clear();
        }
    }

    /// <inheritdoc/>
    protected override bool TryEscaping(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        // Check if the entity should have a chance to escape yet
        if (ent.Comp.DirectionChangeCount < ent.Comp.DirectionChangeThreshold)
            return false;

        // Check if the holder escaped
        if (_random.NextFloat() > ent.Comp.EscapeChance)
            return false;

        // Unanchor the tube and exit
        var xform = Transform(tube);
        _xform.Unanchor(tube, xform);
        Exit(ent);

        return true;
    }
}
