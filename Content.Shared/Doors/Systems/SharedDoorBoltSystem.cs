using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;

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
        SubscribeLocalEvent<DoorBoltComponent, BeforePryEvent>(OnDoorPry);

    }

    private void OnDoorPry(EntityUid uid, DoorBoltComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.BoltsDown || args.Force)
            return;

        args.Message = "airlock-component-cannot-pry-is-bolted-message";

        args.Cancelled = true;
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
