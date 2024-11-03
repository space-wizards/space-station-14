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

        if (component.MaxStealthAngle == null)
            return true;

        if (!TryComp(target, out TransformComponent? targetTransform))
            return false;

        var targetLocalCardinal = targetTransform.LocalRotation.GetCardinalDir().ToAngle(); // players can only look in 4 directions so it seems weird if we don't force it to be cardinal
        var cardinalDifference = targetLocalCardinal - targetTransform.LocalRotation;

        var targetRotation = _transform.GetWorldRotation(target);
        var targetRotationCardinal = targetRotation + cardinalDifference;

        var userRelativeRotation = (_transform.GetWorldPosition(user) - _transform.GetWorldPosition(target)).Normalized().ToWorldAngle().FlipPositive();

        var targetRotationDegrees = targetRotationCardinal.Opposite().Reduced().FlipPositive().Degrees;
        var userRotationDegrees = userRelativeRotation.Reduced().FlipPositive().Degrees;
        var difference = 180 - Math.Abs(Math.Abs(targetRotationDegrees - userRotationDegrees) - 180);

        // Log.Debug($"{cardinalDifference.Degrees}");
        // Log.Debug($"{targetRotation.Degrees}");
        // Log.Debug($"{targetRotationCardinal.Degrees}");
        // Log.Debug($"{targetRotationCardinal.Opposite().Degrees}");
        // Log.Debug($"{userRelativeRotation.Degrees}");
        // Log.Debug($"{(userRelativeRotation - targetRotationCardinal.Opposite()).Degrees}");
        // Log.Debug($"{difference}");
        // Log.Debug($"{userRelativeRotation.EqualsApprox(targetRotationCardinal.Opposite(), 0.25)}");
        // This can remain if I ever need to start debugging this cursed function again
        // and to show the ways of testing the difference that did not work

        return difference <= component.MaxStealthAngle.Value.Degrees;
    }
}
