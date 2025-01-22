using Content.Shared.ParcelWrap.Components;
using Content.Shared.ParcelWrap.Systems;

namespace Content.Client.ParcelWrap;

/// <inheritdoc/>
public sealed class ParcelWrappingSystem : SharedParcelWrappingSystem
{
    protected override void WrapInternal(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        // Consume a `use` on the wrapper and play a wrapping sound.
        wrapper.Comp.Uses -= 1;
        Audio.PlayPredicted(wrapper.Comp.WrapSound, target, user);
    }

    protected override EntityUid? UnwrapInternal(EntityUid? user, Entity<WrappedParcelComponent> parcel)
    {
        var parcelTransform = Transform(parcel);

        var containedEntity = parcel.Comp.Contents.ContainedEntity;
        if (containedEntity is { } parcelContents)
        {
            Container.Remove(parcelContents, parcel.Comp.Contents, true, true, parcelTransform.Coordinates);

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

        return containedEntity;
    }
}
