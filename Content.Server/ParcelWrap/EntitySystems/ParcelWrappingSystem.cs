using Content.Server.ParcelWrap.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.ParcelWrap.EntitySystems;

/// <summary>
/// This system handles things related to package wrap, both wrapping items to create parcels, and unwrapping existing
/// parcels.
/// </summary>
/// <seealso cref="ParcelWrapComponent"/>
/// <seealso cref="WrappedParcelComponent"/>
public sealed partial class ParcelWrappingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParcelWrapComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ParcelWrapComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbsForParcelWrap);

        SubscribeLocalEvent<WrappedParcelComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WrappedParcelComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WrappedParcelComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbsForWrappedParcel);
        SubscribeLocalEvent<WrappedParcelComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<WrappedParcelComponent, GotReclaimedEvent>(OnDestroyed);
    }


    /// <summary>
    /// Returns whether or not <paramref name="wrapper"/> can be used to wrap <paramref name="target"/>.
    /// </summary>
    public bool IsWrappable(Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        return
            // Wrapping cannot wrap itself
            wrapper.Owner != target &&
            _whitelist.IsWhitelistPass(wrapper.Comp.Whitelist, target) &&
            _whitelist.IsBlacklistFail(wrapper.Comp.Blacklist, target);
    }

    /// <summary>
    /// Spawns a WrappedParcel containing <paramref name="target"/>.
    /// </summary>
    /// <param name="user">The entity using <paramref name="wrapper"/> to wrap <paramref name="target"/>.</param>
    /// <param name="wrapper">The wrapping being used. Determines appearance of the spawned parcel.</param>
    /// <param name="target">The entity being wrapped.</param>
    /// <returns>The newly created parcel. Returns null only in exceptional failure cases.</returns>
    private Entity<WrappedParcelComponent>? WrapInternal(EntityUid user, ParcelWrapComponent wrapper, EntityUid target)
    {
        var spawned = Spawn(wrapper.ParcelPrototype, Transform(target).Coordinates);

        // If this wrap maintains the size when wrapping, set the parcel's size to the target's size. Otherwise use the
        // wrap's fallback size.
        ItemComponent? targetItemComp = null;
        var size = wrapper.FallbackItemSize;
        if (wrapper.WrappedItemsMaintainSize && TryComp(target, out targetItemComp))
        {
            size = targetItemComp.Size;
        }

        var item = Comp<ItemComponent>(spawned);
        _item.SetSize(spawned, size, item);
        _appearance.SetData(spawned, WrappedParcelVisuals.Size, size.Id);

        // If this wrap maintains the shape when wrapping and the item has a shape override, copy the shape override to
        // the parcel.
        if (wrapper.WrappedItemsMaintainShape && Resolve(target, ref targetItemComp, logMissing: false) &&
            targetItemComp.Shape is { } shape)
        {
            _item.SetShape(spawned, shape, item);
        }

        // If the target's in a container, try to put the parcel in its place in the container.
        if (_container.TryGetContainingContainer((target, null, null), out var containerOfTarget))
        {
            _container.Remove(target, containerOfTarget);
            _container.InsertOrDrop((spawned, null, null), containerOfTarget);
        }

        // Insert the target into the parcel.
        var parcel = EnsureComp<WrappedParcelComponent>(spawned);
        if (!_container.Insert(target, parcel.Contents))
        {
            DebugTools.Assert(
                $"Failed to insert target entity into newly spawned parcel. target={PrettyPrint.PrintUserFacing(target)}");
            QueueDel(spawned);
            return null;
        }

        // Play a wrapping sound.
        _audio.PlayPvs(wrapper.WrapSound, spawned);

        return (spawned, parcel);
    }

    /// <summary>
    /// Despawns <paramref name="parcel"/>, leaving the contained entity where the parcel was.
    /// </summary>
    /// <param name="parcel">The entity being unwrapped.</param>
    /// <returns>
    /// The newly unwrapped, contained entity. Returns null only in the exceptional case that the parcel contained
    /// nothing, which should be prevented by not creating such parcels.
    /// </returns>
    private EntityUid? UnwrapInternal(Entity<WrappedParcelComponent> parcel)
    {
        var parcelCoords = Transform(parcel).Coordinates;

        var containedEntity = parcel.Comp.Contents.ContainedEntity;
        if (containedEntity is { } parcelContents)
        {
            _container.Remove(parcelContents,
                parcel.Comp.Contents,
                true,
                true,
                parcelCoords);

            // If the parcel is in a container, try to put the unwrapped contents in that container.
            if (_container.TryGetContainingContainer((parcel, null, null), out var outerContainer))
            {
                // Make space in the container for the parcel contents.
                _container.Remove((parcel, null, null), outerContainer, force: true);
                _container.InsertOrDrop((parcelContents, null, null), outerContainer);
            }
        }

        // Make some trash and play an unwrapping sound.
        var trash = Spawn(parcel.Comp.UnwrapTrash, parcelCoords);
        _transform.DropNextTo((trash, null), (parcel, null));
        _audio.PlayPvs(parcel.Comp.UnwrapSound, parcelCoords);

        EntityManager.DeleteEntity(parcel);

        return containedEntity;
    }
}
