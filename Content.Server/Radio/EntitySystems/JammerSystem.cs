using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RadioJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<RadioJammerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioSendAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = AllEntityQuery<RadioJammerComponent>();
        while (query.MoveNext(out var uid, out var jam))
        {
            if (jam.Activated && _powerCell.TryGetBatteryFromSlot(uid, out var battery))
                jam.Activated = battery.TryUseCharge(jam.Wattage * frameTime);
        }
    }

    private void OnActivate(EntityUid uid, RadioJammerComponent comp, ActivateInWorldEvent args)
    {
        comp.Activated =
            !comp.Activated &&
            _powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
            battery.CurrentCharge > comp.Wattage;
        var state = Loc.GetString(comp.Activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        _popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(EntityUid uid, RadioJammerComponent comp, PowerCellChangedEvent args)
    {
        if (args.Ejected)
            comp.Activated = false;
    }

    private void OnExamine(EntityUid uid, RadioJammerComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var msg = comp.Activated
                ? Loc.GetString("radio-jammer-component-examine-on-state")
                : Loc.GetString("radio-jammer-component-examine-off-state");
            args.PushMarkup(msg);
            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
                args.PushMarkup(Loc.GetString("radio-jammer-component-charge",
                    ("charge", (int) ((battery.CurrentCharge / battery.MaxCharge) * 100))));
        }
    }

    private void OnRadioSendAttempt(ref RadioReceiveAttemptEvent args)
    {
        var source = Transform(args.RadioSource).Coordinates;
        var query = AllEntityQuery<RadioJammerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var jam, out var transform))
        {
            if (jam.Activated && source.InRange(EntityManager, _transform, transform.Coordinates, jam.Range))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
