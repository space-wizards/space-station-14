using Robust.Shared.GameObjects;

namespace Content.Shared.Placeable;

public sealed class ItemParenterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemParenterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ItemParenterComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnItemPlaced(EntityUid uid, ItemParenterComponent comp, ref ItemPlacedEvent args)
    {
        Log.Debug($"error - DO NOT COMMIT - placed {args.OtherEntity} onto {uid}");
        _transform.SetParent(args.OtherEntity, uid);
    }

    private void OnItemRemoved(EntityUid uid, ItemParenterComponent comp, ref ItemRemovedEvent args)
    {
        Log.Debug($"error - DO NOT COMMIT - removed {args.OtherEntity} from {uid}");
        var xform = Transform(uid);
        _transform.SetParent(args.OtherEntity, xform.ParentUid);
    }
}
