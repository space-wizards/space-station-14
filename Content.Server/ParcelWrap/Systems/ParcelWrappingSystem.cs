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

    protected override void WrapInternal(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target)
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
        }

        // Consume a `use` on the wrapper.
        wrapper.Comp.Uses -= 1;
        if (wrapper.Comp.Uses <= 0)
        {
            QueueDel(wrapper);
        }
        else
        {
            Dirty(wrapper);
        }

        // Play a wrapping sound.
        Audio.PlayPredicted(wrapper.Comp.WrapSound, target, user);
    }

    protected override EntityUid? UnwrapInternal(EntityUid? user, Entity<WrappedParcelComponent> parcel)
    {
        var parcelTransform = Transform(parcel);

        var containedEntity = parcel.Comp.Contents.ContainedEntity;
        if (containedEntity is { } parcelContents)
        {
            Container.Remove(parcelContents,
                parcel.Comp.Contents,
                true,
                true,
                parcelTransform.Coordinates);

            // If the parcel is in a container, try to put the unwrapped contents in that container.
            if (Container.TryGetContainingContainer((parcel, null, null), out var outerContainer))
            {
                // Make space in the container for the parcel contents.
                Container.Remove((parcel, null, null), outerContainer, force: true);
                Container.InsertOrDrop((parcelContents, null, null), outerContainer);
            }
        }

        // Make some trash and play an unwrapping sound.
        Audio.PlayPredicted(parcel.Comp.UnwrapSound, parcel, user);
        if (parcel.Comp.UnwrapTrash is { } trashProto)
        {
            var trash = Spawn(trashProto, parcelTransform.Coordinates);
            _transform.DropNextTo((trash, null), (parcel, parcelTransform));
        }

        QueueDel(parcel);

        return containedEntity;
    }
}
