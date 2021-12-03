using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Power;
using Content.Shared.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Power;

// Generic power wire action. Use on anything
// that requires power.
[DataDefinition]
public class PowerWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "POWR";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    private ElectrocutionSystem _electrocutionSystem = default!;

    public PowerWireAction()
    {
    }

    public override object Identifier { get; } = PowerWireActionKey.Key;

    private void UpdatePowerStatus(Wire wire)
    {
        if (EntityManager.TryGetComponent<ApcPowerReceiverComponent>(wire.Owner, out var power))
        {
            WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out var pulsed);
            StatusLightState lightState = StatusLightState.Off;

            if (pulsed is bool pulseCast)
            {
                power.PowerDisabled = wire.IsCut || pulseCast;
                if (pulseCast)
                {
                    lightState = StatusLightState.BlinkingSlow;
                }
                else
                {
                    lightState = (wire.IsCut == true)
                        ? StatusLightState.Off
                        : StatusLightState.On;
                }
            }
            else
            {
                power.PowerDisabled = wire.IsCut;

                lightState = (wire.IsCut == true)
                    ? StatusLightState.Off
                    : StatusLightState.On;

            }
            var status = new StatusLightData(
                _statusColor,
                lightState,
                _text);

            WiresSystem.SetStatus(wire.Owner, PowerWireActionKey.Status, status);
        }
    }

    private void TryElectrocution(EntityUid used, EntityUid user, bool toggle = false)
    {
        if (EntityManager.TryGetComponent<ElectrifiedComponent>(used, out var electrified))
        {
            electrified.Enabled = true;
            _electrocutionSystem.TryDoElectrifiedAct(used, user);
            electrified.Enabled = toggle;
        }
    }

    public override void Initialize(EntityUid uid, Wire wire)
    {
        base.Initialize(uid, wire);

        _electrocutionSystem = EntitySystem.Get<ElectrocutionSystem>();
        UpdatePowerStatus(wire);
    }

    public override bool Cut(EntityUid used, EntityUid user, Wire wire)
    {
        wire.IsCut = true;
        if (WiresSystem.TryGetData(used, PowerWireActionKey.PulseCancel, out var pulseObject))
        {
            if (pulseObject is CancellationTokenSource pulseToken)
            {
                pulseToken.Cancel();
            }
        }

        TryElectrocution(used, user);

        UpdatePowerStatus(wire);

        return true;
    }

    public override bool Mend(EntityUid used, EntityUid user, Wire wire)
    {
        wire.IsCut = false;
        TryElectrocution(used, user);

        UpdatePowerStatus(wire);

        return true;
    }

    public override bool Pulse(EntityUid used, EntityUid user, Wire wire)
    {
        WiresSystem.SetData(used, PowerWireActionKey.Pulsed, true);
        if (WiresSystem.TryGetData(used, PowerWireActionKey.PulseCancel, out var pulseObject))
        {
            if (pulseObject is CancellationTokenSource pulseToken)
            {
                pulseToken.Cancel();
            }
        }

        var newPulseToken = new CancellationTokenSource();
        var doAfter = new DoAfterEventArgs(
            used,
            _pulseTimeout,
            newPulseToken.Token)
        {
            UserCancelledEvent = new WireDoAfterEvent(AwaitPulseCancel, wire),
            UserFinishedEvent = new WireDoAfterEvent(AwaitPulseCancel, wire)
        };

        DoAfterSystem.DoAfter(doAfter);

        // AwaitPulseCancel(used, wire, _doAfterSystem.WaitDoAfter(doAfter));

        WiresSystem.SetData(used, PowerWireActionKey.PulseCancel, newPulseToken);
        UpdatePowerStatus(wire);

        return true;
    }

    private void AwaitPulseCancel(EntityUid used, Wire wire)
    {
        WiresSystem.SetData(used, PowerWireActionKey.Pulsed, false);
        UpdatePowerStatus(wire);
    }
}
