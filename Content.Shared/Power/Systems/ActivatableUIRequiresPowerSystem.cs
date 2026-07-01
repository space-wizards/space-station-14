using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.Events;
using Content.Shared.UserInterface;

namespace Content.Shared.Power.Systems;

public sealed partial class ActivatableUIRequiresPowerSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ActivatableUISystem _uiSystem = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, ActivatableUIOpenAttemptEvent>(OnActivate);
        SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnActivate(Entity<ActivatableUIRequiresPowerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || _power.IsPowered(ent.Owner))
            return;

        if (!args.Silent)
            _popup.PopupPredicted(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent.Owner)), args.User, args.User);

        args.Cancel();
    }

    private void OnPowerChanged(EntityUid uid, ActivatableUIRequiresPowerComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            _uiSystem.CloseAll(uid);
    }
}
