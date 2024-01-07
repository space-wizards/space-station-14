using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class PriceGunSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PriceGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PriceGunComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnUtilityVerb(EntityUid uid, PriceGunComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        var price = _pricingSystem.GetPrice(args.Target);

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", Identity.Entity(args.Target, EntityManager)), ("price", $"{price:F2}")), args.User, args.User);
                _useDelay.TryResetDelay((uid, useDelay));
            },
            Text = Loc.GetString("price-gun-verb-text"),
            Message = Loc.GetString("price-gun-verb-message", ("object", Identity.Entity(args.Target, EntityManager)))
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(EntityUid uid, PriceGunComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        var price = _pricingSystem.GetPrice(args.Target.Value);

        _popupSystem.PopupEntity(Loc.GetString("price-gun-pricing-result", ("object", Identity.Entity(args.Target.Value, EntityManager)), ("price", $"{price:F2}")), args.User, args.User);
        _useDelay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }
}
