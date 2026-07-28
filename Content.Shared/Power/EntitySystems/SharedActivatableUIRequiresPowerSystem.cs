using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.Power.EntitySystems;

public abstract partial class SharedActivatableUIRequiresPowerSystem : EntitySystem
{
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    protected void OnActivate(Entity<ActivatableUIRequiresPowerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || _powerReceiver.IsPowered(ent.Owner))
            return;

        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent.Owner)), args.User, args.User);

        args.Cancel();
    }
}
