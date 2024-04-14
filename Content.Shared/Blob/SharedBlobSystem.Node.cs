using Content.Shared.Blob.Components;
using Robust.Shared.Map;

namespace Content.Shared.Blob;

public abstract partial class SharedBlobSystem
{
    public void InitializeNode()
    {
        SubscribeLocalEvent<BlobNodeComponent, MoveEvent>(OnNodeMove);
    }

    private void OnNodeMove(Entity<BlobNodeComponent> ent, ref MoveEvent args)
    {
        SetNearby(ent, args.OldPosition, false);
        SetNearby(ent, args.NewPosition, true);
    }

    protected void SetNearby(Entity<BlobNodeComponent> ent, EntityCoordinates coordinates, bool val)
    {
        var nearby = _lookup.GetEntitiesInRange<BlobStructureComponent>(coordinates, ent.Comp.PulseRange);
        foreach (var blob in nearby)
        {
            SetBlobStructurePulsed(blob, val);
        }
    }

    public void SetBlobStructurePulsed(Entity<BlobStructureComponent> ent, bool val)
    {
        if (ent.Comp.Pulsed == val)
            return;
        ent.Comp.Pulsed = val;
        Dirty(ent, ent.Comp);

        var ev = new BlobPulsedSetEvent(val);
        RaiseLocalEvent(ent, ref ev);
    }

    public bool HasNodeNearby(EntityCoordinates coords)
    {
        var query = EntityQueryEnumerator<BlobNodeComponent, TransformComponent>();
        while (query.MoveNext(out _, out var node, out var xform))
        {
            if (!xform.Coordinates.TryDelta(EntityManager, _transform, coords, out var delta))
                continue;

            if (delta.LengthSquared() > node.PulseRangeSquared)
                continue;

            return true;
        }

        return false;
    }
}
