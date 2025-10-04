using Content.Server.Atmos.EntitySystems;
using Content.Shared.Conduit;
using Content.Shared.Conduit.Holder;
using Content.Shared.Disposal.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Conduit.Holder;

/// <inheritdoc/>
public sealed partial class ConduitHolderSystem : SharedConduitHolderSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void TransferAtmos(Entity<ConduitHolderComponent> ent, Entity<DisposalUnitComponent> unit)
    {
        _atmos.Merge(ent.Comp.Air, unit.Comp.Air);
        unit.Comp.Air.Clear();
    }

    /// <inheritdoc/>
    protected override void ExpelAtmos(Entity<ConduitHolderComponent> ent)
    {
        if (_atmos.GetContainingMixture(ent.Owner, false, true) is { } environment)
        {
            _atmos.Merge(environment, ent.Comp.Air);
            ent.Comp.Air.Clear();
        }
    }

    /// <inheritdoc/>
    protected override bool TryEscaping(Entity<ConduitHolderComponent> ent, Entity<ConduitComponent> conduit)
    {
        // Check if the entity should have a chance to escape yet
        if (ent.Comp.DirectionChangeCount < ent.Comp.DirectionChangeThreshold &&
            _timing.CurTime < ent.Comp.EndTime)
            return false;

        // Check if the holder escaped
        if (_random.NextFloat() > ent.Comp.EscapeChance)
            return false;

        // Unanchor the conduit and exit
        var xform = Transform(conduit);
        _xformSystem.Unanchor(conduit, xform);
        Exit(ent);

        return true;
    }
}
