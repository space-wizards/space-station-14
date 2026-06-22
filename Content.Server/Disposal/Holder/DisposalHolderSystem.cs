using Content.Server.Atmos.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Content.Shared.Maps;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Holder;

/// <inheritdoc/>
public sealed partial class DisposalHolderSystem : SharedDisposalHolderSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedDisposalUnitSystem _disposalUnit = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private TileSystem _tile = default!;

    private EntityQuery<DisposalUnitComponent> _disposalUnitQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _disposalUnitQuery = GetEntityQuery<DisposalUnitComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

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
    public override void Exit(Entity<DisposalHolderComponent> ent)
    {
        if (Terminating(ent))
            return;

        if (ent.Comp.IsExiting)
            return;

        ent.Comp.IsExiting = true;
        Dirty(ent);

        // Get the holder and grid transforms
        var xform = _xformQuery.GetComponent(ent);
        var gridUid = xform.GridUid;
        _xformQuery.TryGetComponent(gridUid, out var gridXform);

        // Determine the exit angle of the ejected entities
        var exitDirection = ent.Comp.CurrentDirection;
        Angle? exitAngle = exitDirection != Direction.Invalid ? exitDirection.ToAngle() : null;

        // Check for a disposal unit to throw them into and then eject them from it.
        // *This ejection also makes the target not collide with the unit.*
        // *This is on purpose.*

        Entity<DisposalUnitComponent>? unit = null;

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            foreach (var contentUid in _maps.GetLocal(gridUid.Value, grid, xform.Coordinates))
            {
                if (_disposalUnitQuery.TryGetComponent(contentUid, out var disposalUnit))
                {
                    unit = new(contentUid, disposalUnit);
                    break;
                }
            }

            // If no disposal unit was found, this exit will be a little messy
            if (unit == null && _net.IsServer)
            {
                // Pry up the tile that the pipe was under
                var tileRef = _maps.GetTileRef((gridUid.Value, grid), xform.Coordinates);
                _tile.PryTile(tileRef);

                // Also pry up the tile infront of the pipe
                if (exitAngle != null)
                {
                    tileRef = _maps.GetTileRef((gridUid.Value, grid), xform.Coordinates.Offset(exitAngle.Value.ToWorldVec()));
                    _tile.PryTile(tileRef);
                }
            }
        }

        // Update the exit angle here to account for the grid's rotation
        if (exitAngle != null && gridXform != null)
        {
            exitAngle += _xform.GetWorldRotation(gridXform);
        }

        // We're purposely iterating over all the holder's children
        // because the holder might have something teleported into it,
        // outside the usual container insertion logic.
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var held))
        {
            DetachEntity(held);

            var heldMeta = _metaQuery.GetComponent(held);
            var heldXform = _xformQuery.GetComponent(held);

            // Insert the child into the found disposal unit, then pop them out
            if (unit != null && unit.Value.Comp.Container != null && _container.Insert((held, heldXform, heldMeta), unit.Value.Comp.Container))
            {
                _disposalUnit.Remove(unit.Value, held);
            }
            else
            {
                // Otherwise remove the child from the holder and prepare to throw it
                if (ent.Comp.Container != null && ent.Comp.Container.Contains(held))
                {
                    _container.Remove((held, null, heldMeta), ent.Comp.Container, force: true);
                }

                _xform.AttachToGridOrMap(held, heldXform);

                // Knockdown the entity
                _stun.TryKnockdown(held, ent.Comp.ExitStunDuration, force: true);

                // Throw the entity
                if (exitAngle != null && heldXform.ParentUid.IsValid())
                {
                    _throwing.TryThrow(held, exitAngle.Value.ToWorldVec() * ent.Comp.ExitDistanceMultiplier, ent.Comp.TraversalSpeed * ent.Comp.ExitSpeedMultiplier);
                }
            }
        }

        ExpelAtmos(ent);
        Del(ent.Owner);
    }

    /// <inheritdoc/>
    protected override bool TryEscaping(Entity<DisposalHolderComponent> ent, Entity<DisposalTubeComponent> tube)
    {
        // Check if the entity should have a chance to escape yet
        if (!ent.Comp.CanEscape)
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
