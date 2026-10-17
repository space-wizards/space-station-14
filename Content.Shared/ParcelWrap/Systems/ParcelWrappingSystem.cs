using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.ParcelWrap.Systems;

/// <summary>
/// This system handles things related to package wrap, both wrapping items to create parcels, and unwrapping existing
/// parcels.
/// </summary>
/// <seealso cref="ParcelWrapComponent"/>
/// <seealso cref="WrappedParcelComponent"/>
public sealed partial class ParcelWrappingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<ItemComponent> _itemQuery = default!;
    [Dependency] private EntityQuery<MultiHandedItemComponent> _multiHandedItemQuery = default!;
    [Dependency] private EntityQuery<ParcelWrapOverrideComponent> _parcelWrapOverrideQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<WrappedParcelComponent> _wrappedParcelQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeParcelWrap();
        InitializeWrappedParcel();
    }

    /// <summary>
    /// Returns whether or not <paramref name="wrapper"/> can be used to wrap <paramref name="target"/>.
    /// </summary>
    /// <param name="wrapper">The entity doing the wrapping.</param>
    /// <param name="target">The entity to be wrapped.</param>
    /// <returns>True if <paramref name="wrapper"/> can be used to wrap <paramref name="target"/>, false otherwise.</returns>
    [PublicAPI]
    public bool IsWrappable(Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        return
            // Wrapping cannot wrap itself
            wrapper.Owner != target &&
            // Wrapper should never be empty, but may as well make sure.
            !_charges.IsEmpty(wrapper.Owner) &&
            _whitelist.CheckBoth(target, wrapper.Comp.Blacklist, wrapper.Comp.Whitelist) &&
            // If it can't be moved, it shouldn't be able to go into a parcel and picked up.
            _physicsQuery.TryComp(target, out var physics) && physics.BodyType != BodyType.Static;
    }
}
