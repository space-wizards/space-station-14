using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.ParcelWrap.Systems;

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
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

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
    public bool IsWrappable(Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        return
            // Wrapping cannot wrap itself
            wrapper.Owner != target &&
            // Wrapper should never be empty, but may as well make sure.
            !_charges.IsEmpty(wrapper.Owner) &&
            _whitelist.IsWhitelistPass(wrapper.Comp.Whitelist, target) &&
            _whitelist.IsBlacklistFail(wrapper.Comp.Blacklist, target);
    }
}
