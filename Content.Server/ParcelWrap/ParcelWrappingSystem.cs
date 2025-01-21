using Content.Shared.Destructible;
using Content.Shared.Materials;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.ParcelWrap.EntitySystems;
using Content.Shared.Popups;

namespace Content.Server.ParcelWrap;

/// <inheritdoc/>
public sealed class ParcelWrappingSystem : SharedParcelWrappingSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParcelWrapComponent, ParcelWrapItemDoAfterEvent>(OnWrapItemDoAfter);

        SubscribeLocalEvent<WrappedParcelComponent, UnwrapWrappedParcelDoAfterEvent>(OnUnwrapParcelDoAfter);
        SubscribeLocalEvent<WrappedParcelComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<WrappedParcelComponent, GotReclaimedEvent>(OnDestroyed);
    }

    private void OnWrapItemDoAfter(Entity<ParcelWrapComponent> _, ref ParcelWrapItemDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args is { Target: { } target, Used: { } used } &&
            TryComp<ParcelWrapComponent>(used, out var wrapper))
        {
            WrapInternal(args.User, (used, wrapper), target);
            args.Handled = true;
        }
    }

    private void OnUnwrapParcelDoAfter(Entity<WrappedParcelComponent> _, ref UnwrapWrappedParcelDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is { } target && TryComp<WrappedParcelComponent>(target, out var parcel))
        {
            UnwrapInternal((target, parcel));
            args.Handled = true;
        }
    }

    private void OnDestroyed<T>(Entity<WrappedParcelComponent> parcel, ref T args)
    {
        // Unwrap the package and if something was in it, show a popup describing "wow something came out!"
        if (UnwrapInternal(parcel) is { } contents)
        {
            _popup.PopupPredicted(Loc.GetString("parcel-wrap-popup-parcel-destroyed", ("contents", contents)),
                contents,
                null,
                PopupType.MediumCaution);
        }
    }
}
