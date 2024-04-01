using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Server.Station.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActiveRadioJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<RadioJammerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent>();
        while (query.MoveNext(out var uid, out var _, out var jam))
        {
            if (_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery) &&
                !_battery.TryUseCharge(batteryUid.Value, jam.Wattage * frameTime, battery))
            {
                RemComp<ActiveRadioJammerComponent>(uid);
                RemComp<DeviceNetworkJammerComponent>(uid);
            }
        }
    }

    private void OnActivate(EntityUid uid, RadioJammerComponent comp, ActivateInWorldEvent args)
    {
        var activated = !HasComp<ActiveRadioJammerComponent>(uid) &&
            _powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
            battery.CurrentCharge > comp.Wattage;
        if (activated)
        {
            EnsureComp<ActiveRadioJammerComponent>(uid);
            EnsureComp<DeviceNetworkJammerComponent>(uid, out var jammingComp);
            jammingComp.Range = comp.Range;
            jammingComp.JammableNetworks.Add(DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());
            Dirty(uid, jammingComp);
        }
        else
        {
            RemComp<ActiveRadioJammerComponent>(uid);
            RemComp<DeviceNetworkJammerComponent>(uid);
        }
        var state = Loc.GetString(activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        _popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(EntityUid uid, ActiveRadioJammerComponent comp, PowerCellChangedEvent args)
    {
        if (args.Ejected)
            RemComp<ActiveRadioJammerComponent>(uid);
    }

    private void OnExamine(EntityUid uid, RadioJammerComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var msg = HasComp<ActiveRadioJammerComponent>(uid)
                ? Loc.GetString("radio-jammer-component-examine-on-state")
                : Loc.GetString("radio-jammer-component-examine-off-state");
            args.PushMarkup(msg);
        }
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancelSend(args.RadioSource))
        {
            args.Cancelled = true;
        }
    }

    private bool ShouldCancelSend(EntityUid sourceUid)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out _, out _, out var jam, out var transform))
        {
            if (source.InRange(EntityManager, _transform, transform.Coordinates, jam.Range))
            {
                return true;
            }
        }

        return false;
    }
}
