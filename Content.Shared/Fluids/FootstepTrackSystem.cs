using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

/// <summary>
/// Tracks blood from puddles onto feet or footwear and leaves blood footprints.
/// </summary>
public sealed partial class FootstepTrackSystem : EntitySystem
{
    private static readonly ProtoId<ReagentPrototype> BloodReagent = "Blood";

    [Dependency] private SharedDecalSystem _decals = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;
    [Dependency] private EntityQuery<PuddleComponent> _puddleQuery;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery;

    private ProtoId<ReagentPrototype>[] _bloodReagents = [];

    private List<(DecalIndex Index, Decal Decal)> _tempDecals = new();

    public override void Initialize()
    {
        base.Initialize();

        CacheBloodReagents();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ReagentPrototype>())
            CacheBloodReagents();
    }

    private void CacheBloodReagents()
    {
        var bloodReagents = new List<ProtoId<ReagentPrototype>>();

        foreach (var reagent in ProtoMan.EnumeratePrototypes<ReagentPrototype>())
        {
            foreach (var parent in ProtoMan.EnumerateParents(reagent, includeSelf: true))
            {
                if (parent.ID != BloodReagent)
                    continue;

                bloodReagents.Add(reagent.ID);
                break;
            }
        }

        _bloodReagents = bloodReagents.ToArray();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FootstepTrackComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            UpdateTracker((uid, tracker));
        }
    }

    public bool TryPickupBloodFromPuddle(Entity<PuddleComponent> puddle, Entity<FootstepTrackComponent> tracker, EntityUid? stepper = null)
    {
        if (!_solutionContainer.ResolveSolution(puddle.Owner, puddle.Comp.SolutionName, ref puddle.Comp.Solution, out var puddleSolution) ||
            !TryGetBloodColor(puddleSolution, out var bloodColor))
        {
            return false;
        }

        return PickupBlood(tracker, bloodColor, stepper);
    }

    private bool PickupBlood(Entity<FootstepTrackComponent> tracker, Color bloodColor, EntityUid? stepper)
    {
        var comp = tracker.Comp;
        if (comp.MaxSteps == 0)
            return false;

        if (comp.StepsRemaining != comp.MaxSteps)
        {
            comp.StepsRemaining = comp.MaxSteps;
            DirtyField(tracker, comp, nameof(FootstepTrackComponent.StepsRemaining));
        }

        bloodColor = bloodColor.WithAlpha(1f);
        if (comp.BloodColor != bloodColor)
        {
            comp.BloodColor = bloodColor;
            DirtyField(tracker, comp, nameof(FootstepTrackComponent.BloodColor));
        }

        var pickupStepper = stepper ?? tracker.Owner;
        if (TryGetTile(pickupStepper, out var tile, out _, out _))
            SetLastTile(tracker, tile.GridUid, tile.GridIndices);
        else
            ClearLastTile(tracker);

        return true;
    }

    private void UpdateTracker(Entity<FootstepTrackComponent> tracker)
    {
        // You may be wondering, why not XYZ, let me list the other considerations
        // MoveEvent: Figured this was more cache friendly than subscribing to MoveEvents on these entities.
        // StepTrigger: We still need movement tracking after and now we need to handle add / remove component lifetimes.
        // Possibly doable if we change puddle intersectratio as well, but depends if this shows up on a profile.
        var comp = tracker.Comp;

        if (comp.MaxSteps == 0 ||
            !TryGetStepper(tracker, out var stepper) ||
            _gravity.IsWeightless(stepper) ||
            !TryGetTile(stepper, out var tile, out var corner, out var grid))
        {
            return;
        }

        var hadLastTile = comp.HasLastTile;
        var movedTile = !hadLastTile ||
            comp.LastGrid != tile.GridUid ||
            comp.LastTile != tile.GridIndices;

        if (!movedTile)
        {
            TryPickupBloodFromTile(tile, grid, tracker, stepper);
            return;
        }

        TryPickupBloodFromTile(tile, grid, tracker, stepper);

        if (!hadLastTile)
        {
            if (comp.StepsRemaining > 0)
                SetLastTile(tracker, tile.GridUid, tile.GridIndices);
            return;
        }

        if (comp.StepsRemaining == 0)
        {
            return;
        }

        if (HasPuddle(tile, grid))
        {
            ConsumeStep(tracker);
        }
        else
        {
            if (comp.Footprints.Length == 0)
                return;

            LeaveFootprint(tracker, stepper, tile, corner);
        }

        SetLastTile(tracker, tile.GridUid, tile.GridIndices);
    }

    private void LeaveFootprint(
        Entity<FootstepTrackComponent> tracker,
        EntityUid stepper,
        TileRef tile,
        EntityCoordinates coordinates)
    {
        var comp = tracker.Comp;
        var minimumAlpha = comp.MinimumFootprintAlpha / 255f;
        var alpha = Math.Clamp((float) comp.StepsRemaining / comp.MaxSteps, minimumAlpha, 1f);
        var decal = new Decal(
            coordinates.Position,
            GetFootprint(comp),
            comp.BloodColor.WithAlpha(alpha),
            GetFootprintRotation(comp, tile, stepper),
            0,
            cleanable: true);

        // tl;dr duplicate handling. We only add the decal if the alpha is more opaque.
        var existingDecals = _tempDecals;
        existingDecals.Clear();
        _decals.GetDecalsAt(tile.GridUid, decal.Id, decal.Coordinates, decal.Angle, existingDecals);

        if (existingDecals.Count != 0)
        {
            (DecalIndex Index, Decal Decal)? brightest = null;
            foreach (var existing in existingDecals)
            {
                if (brightest == null ||
                    GetAlpha(existing.Decal) > GetAlpha(brightest.Value.Decal))
                {
                    brightest = existing;
                }
            }

            if (brightest != null && GetAlpha(brightest.Value.Decal) >= alpha)
            {
                foreach (var existing in existingDecals)
                {
                    if (existing.Index == brightest.Value.Index)
                        continue;

                    if (!_decals.RemoveDecal(tile.GridUid, existing.Index))
                        return;
                }

                ConsumeStep(tracker);
                return;
            }

            foreach (var existing in existingDecals)
            {
                if (!_decals.RemoveDecal(tile.GridUid, existing.Index))
                    return;
            }
        }

        if (!_decals.TryAddDecal(decal, coordinates, out _))
        {
            return;
        }

        ConsumeStep(tracker);
    }

    private void ConsumeStep(Entity<FootstepTrackComponent> tracker)
    {
        var comp = tracker.Comp;
        if (comp.StepsRemaining > 0)
            comp.StepsRemaining--;
        DirtyField(tracker, comp, nameof(FootstepTrackComponent.StepsRemaining));

        if (comp.Footprints.Length == 0)
            return;

        var footprintCount = Math.Min(comp.Footprints.Length, byte.MaxValue + 1);
        comp.NextFootprintIndex = (byte) ((comp.NextFootprintIndex + 1) % footprintCount);
        DirtyField(tracker, comp, nameof(FootstepTrackComponent.NextFootprintIndex));
    }

    private static ProtoId<DecalPrototype> GetFootprint(FootstepTrackComponent comp)
    {
        var footprintCount = Math.Min(comp.Footprints.Length, byte.MaxValue + 1);
        var index = comp.NextFootprintIndex % footprintCount;
        return comp.Footprints[index];
    }

    private static float GetAlpha(Decal decal)
    {
        return decal.Color?.A ?? 1f;
    }

    private Angle GetFootprintRotation(FootstepTrackComponent tracker, TileRef tile, EntityUid stepper)
    {
        if (!tracker.HasLastTile || tracker.LastGrid != tile.GridUid)
            return _transform.GetWorldRotation(stepper);

        var delta = tile.GridIndices - tracker.LastTile!.Value;
        if (delta == Vector2i.Zero)
            return _transform.GetWorldRotation(stepper);

        return Angle.FromWorldVec(delta);
    }

    private bool TryGetStepper(Entity<FootstepTrackComponent> tracker, out EntityUid stepper)
    {
        if (_container.TryGetContainingContainer((tracker.Owner, null, null), out var container) &&
            _inventory.TryGetContainingSlot((tracker.Owner, null, null), out var slot) &&
            (slot.SlotFlags & SlotFlags.FEET) != 0)
        {
            stepper = container.Owner;
            return true;
        }

        if (_inventory.TryGetSlotEntity(tracker.Owner, "shoes", out _))
        {
            stepper = default;
            return false;
        }

        stepper = tracker.Owner;
        return true;
    }

    // TODO: Decals moment, I really wish they were centered.
    private bool TryGetTile(EntityUid stepper, out TileRef tile, out EntityCoordinates corner, out MapGridComponent grid)
    {
        tile = default;
        corner = default;
        grid = default!;

        if (!_xformQuery.TryComp(stepper, out var xform) ||
            xform.GridUid == null ||
            !_gridQuery.TryComp(xform.GridUid.Value, out var gridComp) ||
            !_map.TryGetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates, out tile))
        {
            return false;
        }

        grid = gridComp;
        corner = new EntityCoordinates(tile.GridUid, (Vector2) tile.GridIndices * grid.TileSize);
        return true;
    }

    private bool TryPickupBloodFromTile(
        TileRef tile,
        MapGridComponent grid,
        Entity<FootstepTrackComponent> tracker,
        EntityUid stepper)
    {
        foreach (var ent in _map.GetAnchoredEntities(tile.GridUid, grid, tile.GridIndices))
        {
            if (!_puddleQuery.TryComp(ent, out var puddle))
                continue;

            if (TryPickupBloodFromPuddle((ent, puddle), tracker, stepper))
                return true;
        }

        return false;
    }

    private bool HasPuddle(TileRef tile, MapGridComponent grid)
    {
        foreach (var ent in _map.GetAnchoredEntities(tile.GridUid, grid, tile.GridIndices))
        {
            if (_puddleQuery.HasComp(ent))
                return true;
        }

        return false;
    }

    private void SetLastTile(Entity<FootstepTrackComponent> tracker, EntityUid gridUid, Vector2i tile)
    {
        var comp = tracker.Comp;
        if (comp.HasLastTile &&
            comp.LastGrid == gridUid &&
            comp.LastTile == tile)
        {
            return;
        }

        comp.LastGrid = gridUid;
        comp.LastTile = tile;
        DirtyField(tracker, comp, nameof(FootstepTrackComponent.LastGrid));
        DirtyField(tracker, comp, nameof(FootstepTrackComponent.LastTile));
    }

    private void ClearLastTile(Entity<FootstepTrackComponent> tracker)
    {
        if (!tracker.Comp.HasLastTile)
            return;

        tracker.Comp.LastTile = null;
        DirtyField(tracker, tracker.Comp, nameof(FootstepTrackComponent.LastTile));
    }

    private bool TryGetBloodColor(Solution solution, out Color color)
    {
        if (solution.GetTotalPrototypeQuantity(_bloodReagents) <= 0)
        {
            color = default;
            return false;
        }

        color = solution.GetColorWithOnly(ProtoMan, _bloodReagents);
        return true;
    }
}
