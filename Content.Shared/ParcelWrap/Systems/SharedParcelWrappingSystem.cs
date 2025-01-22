using Content.Shared.DoAfter;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.ParcelWrap.Systems;

/// <summary>
/// This system handles things related to package wrap, both wrapping items to create parcels, and unwrapping existing
/// parcels.
/// </summary>
/// <seealso cref="ParcelWrapComponent"/>
/// <seealso cref="WrappedParcelComponent"/>
public abstract partial class SharedParcelWrappingSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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
            // Wrapper should never have non-positive uses, but may as well make sure.
            wrapper.Comp.Uses > 0 &&
            _whitelist.IsWhitelistPass(wrapper.Comp.Whitelist, target) &&
            _whitelist.IsBlacklistFail(wrapper.Comp.Blacklist, target);
    }
}
