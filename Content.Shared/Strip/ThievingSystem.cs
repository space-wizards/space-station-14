using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        if (component.Stealthy)
        {
            if (CanStealStealthily(uid, component, args.Target))
                args.Stealth = true;
            else
            {
                _popupSystem.PopupClient(
                    Loc.GetString("thieving-component-failed", ("owner", Identity.Name(args.Target, EntityManager, args.User))),
                    args.User,
                    args.User,
                    PopupType.Medium);
            }
        }

        args.Additive -= component.StripTimeReduction;
    }

    /// <summary>
    /// Checks if the <paramref name="user"/> is able to steal stealthily from the <paramref name="target"/>
    /// </summary>
    public bool CanStealStealthily(EntityUid user, ThievingComponent? component, EntityUid target)
    {
        if (!Resolve(user, ref component))
            return false;

        if (!component.Stealthy)
            return false;

        var targetBackRotation = _transform.GetWorldRotation(target) - Angle.FromDegrees(180);

        var userRelativeRotation =
            (_transform.GetWorldPosition(user) - _transform.GetWorldPosition(target)).ToWorldAngle();

        var isWithinStealthRange =
            userRelativeRotation.EqualsApprox(targetBackRotation, component.MaxStealthAngleTolerance);

        return isWithinStealthRange;
    }
}
