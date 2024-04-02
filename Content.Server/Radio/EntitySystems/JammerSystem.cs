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
using Content.Shared.Verbs;
using Content.Shared.RadioJammer;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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
        SubscribeLocalEvent<RadioJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
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
                    ChangeLEDState(false, uid, jam);
                    RemComp<ActiveRadioJammerComponent>(uid);
                    RemComp<DeviceNetworkJammerComponent>(uid);
                }
                else
                {
                    var percentCharged = battery.CurrentCharge / battery.MaxCharge;
                    if (percentCharged > .50)
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.High, uid, jam, null);
                    }
                    else if (percentCharged < .15)
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.Low, uid, jam, null);
                    }
                    else
                    {
                        ChangeChargeLevel(RadioJammerChargeLevel.Medium, uid, jam, null);
                    }
                }

            }

        }
    }

    private void OnActivate(EntityUid uid, RadioJammerComponent comp, ActivateInWorldEvent args)
    {
        var activated = !HasComp<ActiveRadioJammerComponent>(uid) &&
            _powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
            battery.CurrentCharge > GetCurrentWattage(comp);
        if (activated)
        {
            ChangeLEDState(true, uid, comp);
            EnsureComp<ActiveRadioJammerComponent>(uid);
            EnsureComp<DeviceNetworkJammerComponent>(uid, out var jammingComp);
            jammingComp.Range = GetCurrentRange(comp);
            jammingComp.JammableNetworks.Add(DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());
            Dirty(uid, jammingComp);
        }
        else
        {
            ChangeLEDState(false, uid, comp);
            RemCompDeferred<ActiveRadioJammerComponent>(uid);
            RemCompDeferred<DeviceNetworkJammerComponent>(uid);
        }
        var state = Loc.GetString(activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        _popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(EntityUid uid, ActiveRadioJammerComponent comp, PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            ChangeLEDState(false, uid, EntityManager.GetComponentOrNull<RadioJammerComponent>(uid));
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

    private void OnGetVerb(Entity<RadioJammerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        byte index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act wont work with index.
            // Needs it to be saved in the loop.
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.PowerLevel,
                Disabled = entity.Comp.SelectedPowerLevel == currIndex,
                Act = () =>
                {
                    entity.Comp.SelectedPowerLevel = currIndex;
                    if (TryComp<DeviceNetworkJammerComponent>(entity.Owner, out var jammerComp))
                    {
                        jammerComp.Range = GetCurrentRange(entity.Comp);
                        Dirty(entity.Owner, jammerComp);
                    }
                    _popup.PopupEntity(Loc.GetString(setting.Message), user, user);
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }
    private static float GetCurrentWattage(RadioJammerComponent jammer)
    {
        return jammer.Settings[jammer.SelectedPowerLevel].Wattage;
    }
    private static float GetCurrentRange(RadioJammerComponent jammer)
    {
        return jammer.Settings[jammer.SelectedPowerLevel].Range;
    }
    private void ChangeLEDState(bool isLEDOn, EntityUid uid, RadioJammerComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        _appearance.SetData(uid, RadioJammerVisuals.LEDOn, isLEDOn, appearance);
    }
    private void ChangeChargeLevel(RadioJammerChargeLevel chargeLevel, EntityUid uid, RadioJammerComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        _appearance.SetData(uid, RadioJammerVisuals.ChargeLevel, chargeLevel, appearance);
    }

}
