using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActiveRadioJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
    }

    // TODO: Very important: Make this charge rate based instead of updating every single tick
    // See PredictedBatteryComponent
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent>();

        while (query.MoveNext(out var uid, out var _, out var jam))
        {

            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                if (!_battery.TryUseCharge(battery.Value.AsNullable(), GetCurrentWattage((uid, jam)) * frameTime))
                {
                    ChangeLEDState(uid, false);
                    RemComp<ActiveRadioJammerComponent>(uid);
                    RemComp<DeviceNetworkJammerComponent>(uid);
                }
                else
                {
                    var chargeFraction = _battery.GetChargeLevel(battery.Value.AsNullable());
                    var chargeLevel = chargeFraction switch
                    {
                        > 0.50f => RadioJammerChargeLevel.High,
                        < 0.15f => RadioJammerChargeLevel.Low,
                        _ => RadioJammerChargeLevel.Medium,
                    };
                    ChangeChargeLevel(uid, chargeLevel);
                }

            }

        }
    }

    private void OnActivate(Entity<RadioJammerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var activated = !HasComp<ActiveRadioJammerComponent>(ent) &&
            _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) &&
            _battery.GetCharge(battery.Value.AsNullable()) > GetCurrentWattage(ent);
        if (activated)
        {
            ChangeLEDState(ent.Owner, true);
            EnsureComp<ActiveRadioJammerComponent>(ent);
            EnsureComp<DeviceNetworkJammerComponent>(ent, out var jammingComp);
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
            _jammer.AddJammableNetwork((ent, jammingComp), DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());

            // Add excluded frequencies using the system method
            if (ent.Comp.FrequenciesExcluded != null)
            {
                foreach (var freq in ent.Comp.FrequenciesExcluded)
                {
                    _jammer.AddExcludedFrequency((ent, jammingComp), (uint)freq);
                }
            }
        }
        else
        {
            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveRadioJammerComponent>(ent);
            RemCompDeferred<DeviceNetworkJammerComponent>(ent);
        }
        var state = Loc.GetString(activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        Popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(Entity<ActiveRadioJammerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveRadioJammerComponent>(ent);
        }
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancelSend(args.RadioSource, args.Channel.Frequency))
        {
            args.Cancelled = true;
        }
    }

    private bool ShouldCancelSend(EntityUid sourceUid, int frequency)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var jam, out var transform))
        {
            // Check if this jammer excludes the frequency
            if (jam.FrequenciesExcluded != null && jam.FrequenciesExcluded.Contains(frequency))
                continue;

            if (_transform.InRange(source, transform.Coordinates, GetCurrentRange((uid, jam))))
            {
                return true;
            }
        }

        return false;
    }
}
