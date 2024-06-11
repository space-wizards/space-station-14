using Content.Server.DeviceNetwork.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;
using Content.Shared.RadioJammer;
using Content.Shared.Radio.EntitySystems;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

            if (_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery))
            {
                if (!_battery.TryUseCharge(batteryUid.Value, GetCurrentWattage(jam) * frameTime, battery))
                {
                    ChangeLEDState(false, uid);
                    RemComp<ActiveRadioJammerComponent>(uid);
                    RemComp<DeviceNetworkJammerComponent>(uid);
                }
                else
                {
                    var percentCharged = battery.CurrentCharge / battery.MaxCharge;
                    if (percentCharged > .50)
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.High, uid);
                    }
                    else if (percentCharged < .15)
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.Low, uid);
                    }
                    else
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.Medium, uid);
                    }
                }

            }

        }
    }

    private void OnActivate(EntityUid uid, RadioJammerComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var activated = !HasComp<ActiveRadioJammerComponent>(uid) &&
            _powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
            battery.CurrentCharge > GetCurrentWattage(comp);
        if (activated)
        {
            ChangeLEDState(true, uid);
            EnsureComp<ActiveRadioJammerComponent>(uid);
            EnsureComp<DeviceNetworkJammerComponent>(uid, out var jammingComp);
            jammingComp.Range = GetCurrentRange(comp);
            jammingComp.JammableNetworks.Add(DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());
            Dirty(uid, jammingComp);
        }
        else
        {
            ChangeLEDState(false, uid);
            RemCompDeferred<ActiveRadioJammerComponent>(uid);
            RemCompDeferred<DeviceNetworkJammerComponent>(uid);
        }
        var state = Loc.GetString(activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        Popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(EntityUid uid, ActiveRadioJammerComponent comp, PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            ChangeLEDState(false, uid);
            RemCompDeferred<ActiveRadioJammerComponent>(uid);
        }
    }

    private void OnExamine(EntityUid uid, RadioJammerComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var powerIndicator = HasComp<ActiveRadioJammerComponent>(uid)
                ? Loc.GetString("radio-jammer-component-examine-on-state")
                : Loc.GetString("radio-jammer-component-examine-off-state");
            args.PushMarkup(powerIndicator);

            var powerLevel = Loc.GetString(comp.Settings[comp.SelectedPowerLevel].Name);
            var switchIndicator = Loc.GetString("radio-jammer-component-switch-setting", ("powerLevel", powerLevel));
            args.PushMarkup(switchIndicator);
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
            if (source.InRange(EntityManager, _transform, transform.Coordinates, GetCurrentRange(jam)))
            {
                return true;
            }
        }

        return false;
    }
}
