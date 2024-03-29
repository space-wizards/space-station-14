using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract class SharedFirelockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<FirelockComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
    }

    private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, ref GetPryTimeModifierEvent args)
    {

        if (component.Fire)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-fire-message"),
                uid, args.User, PopupType.MediumCaution);
        }
        else if (component.Pressure)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-pressure-message"),
                uid, args.User, PopupType.MediumCaution);
        }

        if (component.Fire || component.Pressure)
            args.PryTimeModifier *= component.LockedPryTimeModifier;
    }

    private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            // Give the Door remote the ability to force a firelock open even if it is holding back dangerous gas
            var overrideAccess = (args.User != null) && _accessReaderSystem.IsAllowed(args.User.Value, uid);

            if (!component.Powered || (!overrideAccess && component.IsLocked))
                args.Cancel();
        }
}
