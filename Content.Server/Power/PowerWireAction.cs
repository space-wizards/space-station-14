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
//
// note that a lot of this relies on a cheap trick;
// this should be refactored either before this is
// merged
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

    public override object Identifier { get; } = PowerWireActionKey.Key;

    public override object StatusKey { get; } = PowerWireActionKey.Status;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (EntityManager.TryGetComponent<ApcPowerReceiverComponent>(wire.Owner, out var power))
        {
            WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out var pulsed);

            if (pulsed is bool pulseCast)
            {
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
                lightState = (wire.IsCut == true)
                    ? StatusLightState.Off
                    : StatusLightState.On;
            }
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    private void DisablePower(EntityUid owner, bool setting, ApcPowerReceiverComponent? power = null)
    {
        if (power == null
            && !EntityManager.TryGetComponent(owner, out power))
        {
            return;
        }

        power.PowerDisabled = setting;
    }

    private void SetElectrified(EntityUid used, bool setting, ElectrifiedComponent? electrified = null)
    {
        if (electrified == null
            && !EntityManager.TryGetComponent(used, out electrified))
            return;

        electrified.Enabled = setting;
    }

    /// <returns>false if failed, true otherwise</returns>
    private bool TrySetElectrocution(EntityUid user, Wire wire, bool timed = false)
    {
        if (EntityManager.TryGetComponent<ApcPowerReceiverComponent>(wire.Owner, out var power)
            && EntityManager.TryGetComponent<ElectrifiedComponent>(wire.Owner, out var electrified))
        {
            // always set this to true
            SetElectrified(wire.Owner, true, electrified);

            // if we were electrified, then return false
            var electrifiedAttempt = _electrocutionSystem.TryDoElectrifiedAct(wire.Owner, user);

            // if this is timed, we set up a doAfter so that the
            // electrocution continues - unless cancelled
            //
            // if the power is disabled however, just don't bother
            if (timed && (!power.PowerDisabled || !power.Powered))
            {
                WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PowerWireActionKey.ElectrifiedCancel, new WireDoAfterEvent(AwaitElectrifiedCancel, wire));
            }
            else
            {
                SetElectrified(wire.Owner, false, electrified);
            }

            return !electrifiedAttempt;
        }

        return false;
    }

    public override void Initialize(Wire wire)
    {
        base.Initialize(wire);

        _electrocutionSystem = EntitySystem.Get<ElectrocutionSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (!TrySetElectrocution(user, wire))
            return false;

        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.PulseCancel);
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        DisablePower(wire.Owner, true);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (!TrySetElectrocution(user, wire))
            return false;

        DisablePower(wire.Owner, false);

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        if (!TrySetElectrocution(user, wire, true))
            return false;

        // disrupted power shouldn't re-disrupt
        if (WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out var pulsedKey)
            && (bool) pulsedKey)
        {
            return false;
        }

        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Pulsed, true);

        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PowerWireActionKey.PulseCancel, new WireDoAfterEvent(AwaitPulseCancel, wire));

        DisablePower(wire.Owner, true);

        // AwaitPulseCancel(wire.Owner, wire, _doAfterSystem.WaitDoAfter(doAfter));

        return true;
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            if (!WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out var pulsedObject)
                || pulsedObject is not bool pulsed
                || !pulsed)
            {
                WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);
                WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.PulseCancel);
            }
        }
    }

    private void AwaitElectrifiedCancel(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Electrified, false);
        SetElectrified(wire.Owner, false);
    }

    private void AwaitPulseCancel(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Pulsed, false);
        if (!wire.IsCut)
        {
            DisablePower(wire.Owner, false);
        }
    }
}
