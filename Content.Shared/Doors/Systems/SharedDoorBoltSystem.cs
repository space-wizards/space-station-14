using Content.Shared.Doors.Components;
using Content.Shared.Popups;

namespace Content.Shared.Doors.Systems;

public abstract class SharedDoorBoltSystem : EntitySystem
{

    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorPryEvent>(OnDoorPry);

    }

    private void OnDoorPry(EntityUid uid, DoorBoltComponent component, BeforeDoorPryEvent args)
    {
        if (component.BoltsDown)
        {
            Popup.PopupEntity(Loc.GetString("airlock-component-cannot-pry-is-bolted-message"), uid, args.User);
            args.Cancel();
        }
    }

    private void OnBeforeDoorOpened(EntityUid uid, DoorBoltComponent component, BeforeDoorOpenedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    private void OnBeforeDoorClosed(EntityUid uid, DoorBoltComponent component, BeforeDoorClosedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    private void OnBeforeDoorDenied(EntityUid uid, DoorBoltComponent component, BeforeDoorDeniedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    public void SetBoltWireCut(DoorBoltComponent component, bool value)
    {
        component.BoltWireCut = value;
    }
}
