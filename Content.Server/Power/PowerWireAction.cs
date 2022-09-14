using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Power;
using Content.Shared.Wires;

namespace Content.Server.Power;

// Generic power wire action. Use on anything
// that requires power.
[DataDefinition]
public sealed class PowerWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "POWR";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    private ElectrocutionSystem _electrocutionSystem = default!;

    public override object StatusKey { get; } = PowerWireActionKey.Status;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.MainWire, out int main)
            && main != wire.Id)
        {
            return null;
        }

        if (IsPowered(wire.Owner))
        {
            if (!AllWiresMended(wire.Owner)
                || WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out bool pulsed)
                && pulsed)
            {
                lightState = StatusLightState.BlinkingSlow;
            }
            else
            {
                lightState = (AllWiresCut(wire.Owner))
                    ? StatusLightState.Off
                    : StatusLightState.On;
            }
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    private bool AllWiresCut(EntityUid owner)
    {
        return WiresSystem.TryGetData(owner, PowerWireActionKey.CutWires, out int? cut)
            && WiresSystem.TryGetData(owner, PowerWireActionKey.WireCount, out int? count)
            && count == cut;
    }

    private bool AllWiresMended(EntityUid owner)
    {
        return WiresSystem.TryGetData(owner, PowerWireActionKey.CutWires, out int? cut)
               && cut == 0;
    }

    // I feel like these two should be within ApcPowerReceiverComponent at this point.
    // Getting it from a dictionary is significantly more expensive.
    private void SetPower(EntityUid owner, bool pulsed)
    {
        if (!EntityManager.TryGetComponent(owner, out ApcPowerReceiverComponent? power))
        {
            return;
        }

        if (pulsed)
        {
            power.PowerDisabled = true;
            return;
        }

        if (AllWiresCut(owner))
        {
            power.PowerDisabled = true;
        }
        else
        {
            if (WiresSystem.TryGetData(owner, PowerWireActionKey.Pulsed, out bool isPulsed)
                && isPulsed)
            {
                return;
            }

            power.PowerDisabled = false;
        }
    }

    private void SetWireCuts(EntityUid owner, bool isCut)
    {
        if (WiresSystem.TryGetData(owner, PowerWireActionKey.CutWires, out int? cut)
            && WiresSystem.TryGetData(owner, PowerWireActionKey.WireCount, out int? count))
        {
            if (cut == count && isCut
                || cut <= 0 && !isCut)
            {
                return;
            }

            cut = isCut ? cut + 1 : cut - 1;
            WiresSystem.SetData(owner, PowerWireActionKey.CutWires, cut);
        }
    }

    private void SetElectrified(EntityUid used, bool setting, ElectrifiedComponent? electrified = null)
    {
        if (electrified == null
            && !EntityManager.TryGetComponent(used, out electrified))
            return;

        electrified.Enabled = setting;
    }

    /// <returns>false if failed, true otherwise, or if the entity cannot be electrified</returns>
    private bool TrySetElectrocution(EntityUid user, Wire wire, bool timed = false)
    {
        if (!EntityManager.TryGetComponent<ElectrifiedComponent>(wire.Owner, out var electrified))
        {
            return true;
        }

        // always set this to true
        SetElectrified(wire.Owner, true, electrified);

        var electrifiedAttempt = _electrocutionSystem.TryDoElectrifiedAct(wire.Owner, user);

        // if we were electrified, then return false
        return !electrifiedAttempt;

    }

    private void UpdateElectrocution(Wire wire)
    {
        var allCut = AllWiresCut(wire.Owner);

        var activePulse = false;

        if (WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out bool pulsed))
        {
            activePulse = pulsed;
        }

        // if this is actively pulsed,
        // and there's not already an electrification cancel occurring,
        // we need to start that timer immediately
        if (!WiresSystem.HasData(wire.Owner, PowerWireActionKey.ElectrifiedCancel)
            && activePulse
            && IsPowered(wire.Owner)
            && !allCut)
        {
            WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PowerWireActionKey.ElectrifiedCancel, new TimedWireEvent(AwaitElectrifiedCancel, wire));
        }
        else
        {
            if (!activePulse && allCut || AllWiresMended(wire.Owner))
            {
                SetElectrified(wire.Owner, false);
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _electrocutionSystem = EntitySystem.Get<ElectrocutionSystem>();
    }

    // This should add a wire into the entity's state, whether it be
    // in WiresComponent or ApcPowerReceiverComponent.
    public override bool AddWire(Wire wire, int count)
    {
        if (!WiresSystem.HasData(wire.Owner, PowerWireActionKey.CutWires))
        {
            WiresSystem.SetData(wire.Owner, PowerWireActionKey.CutWires, 0);
        }

        if (count == 1)
        {
            WiresSystem.SetData(wire.Owner, PowerWireActionKey.MainWire, wire.Id);
        }

        WiresSystem.SetData(wire.Owner, PowerWireActionKey.WireCount, count);

        return true;
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (!TrySetElectrocution(user, wire))
            return false;

        SetWireCuts(wire.Owner, true);

        SetPower(wire.Owner, false);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (!TrySetElectrocution(user, wire))
            return false;

        // Mending any power wire restores shorts.
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.PulseCancel);
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        SetWireCuts(wire.Owner, false);

        SetPower(wire.Owner, false);

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        var electrocuted = !TrySetElectrocution(user, wire, true);

        if (WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out bool pulsedKey)
            && pulsedKey)
        {
            return false;
        }

        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Pulsed, true);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PowerWireActionKey.PulseCancel, new TimedWireEvent(AwaitPulseCancel, wire));

        if (electrocuted)
        {
            return false;
        }

        SetPower(wire.Owner, true);
        return true;
    }

    public override void Update(Wire wire)
    {
        UpdateElectrocution(wire);

        if (!IsPowered(wire.Owner))
        {
            if (!WiresSystem.TryGetData(wire.Owner, PowerWireActionKey.Pulsed, out bool pulsed)
                || !pulsed)
            {
                WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);
                WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.PulseCancel);
            }
        }
    }

    private void AwaitElectrifiedCancel(Wire wire)
    {
        if (AllWiresMended(wire.Owner))
        {
            SetElectrified(wire.Owner, false);
        }
    }

    private void AwaitPulseCancel(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Pulsed, false);
        SetPower(wire.Owner, false);
    }
}
