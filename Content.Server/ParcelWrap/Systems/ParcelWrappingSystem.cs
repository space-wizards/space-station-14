using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.ParcelWrap.Systems;
using Robust.Shared.Utility;

namespace Content.Server.ParcelWrap.Systems;

/// <inheritdoc/>
public sealed class ParcelWrappingSystem : SharedParcelWrappingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override Entity<WrappedParcelComponent>? SpawnParcelAndInsertTarget(EntityUid user,
        Entity<ParcelWrapComponent> wrapper,
        EntityUid target)
    {
        var spawned = Spawn(wrapper.Comp.ParcelPrototype, Transform(target).Coordinates);

        // If this wrap maintains the size when wrapping, set the parcel's size to the target's size. Otherwise use the
        // wrap's fallback size.
        TryComp(target, out ItemComponent? targetItemComp);
        var size = wrapper.Comp.FallbackItemSize;
        if (wrapper.Comp.WrappedItemsMaintainSize && targetItemComp is not null)
        {
            size = targetItemComp.Size;
        }

        // ParcelWrap's spawned entity should always have an `ItemComp`. As of writing, the only use has it hardcoded on
        // its prototype.
        var item = Comp<ItemComponent>(spawned);
        _item.SetSize(spawned, size, item);
        _appearance.SetData(spawned, WrappedParcelVisuals.Size, size.Id);

        // If this wrap maintains the shape when wrapping and the item has a shape override, copy the shape override to
        // the parcel.
        if (wrapper.Comp.WrappedItemsMaintainShape && targetItemComp is { Shape: { } shape })
        {
            _item.SetShape(spawned, shape, item);
        }

        // If the target's in a container, try to put the parcel in its place in the container.
        if (Container.TryGetContainingContainer((target, null, null), out var containerOfTarget))
        {
            Container.Remove(target, containerOfTarget);
            Container.InsertOrDrop((spawned, null, null), containerOfTarget);
        }

        // Insert the target into the parcel.
        var parcel = EnsureComp<WrappedParcelComponent>(spawned);
        if (!Container.Insert(target, parcel.Contents))
        {
            DebugTools.Assert(
                $"Failed to insert target entity into newly spawned parcel. target={PrettyPrint.PrintUserFacing(target)}");
            QueueDel(spawned);
            return null;
        }

        return (spawned, parcel);
    }

    protected override void SpawnUnwrapTrash(Entity<WrappedParcelComponent, TransformComponent> parcel)
    {
        if (parcel.Comp1.UnwrapTrash is { } trashProto)
        {
            var trash = Spawn(trashProto, parcel.Comp2.Coordinates);
            _transform.DropNextTo((trash, null), (parcel, parcel.Comp2));
        }
    }
}
