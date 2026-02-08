using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;

namespace Content.Client.Power.EntitySystems;

public sealed class PowerChargeSystem : SharedPowerChargeSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerChargeComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(EntityUid uid, PowerChargeComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (component.Intact || args.Cancelled)
            return;

        args.Cancel();
        if (!args.Silent)
            _popupSystem.PopupPredicted(Loc.GetString("power-charge-component-interact-broken"), uid, args.User);
    }
}
