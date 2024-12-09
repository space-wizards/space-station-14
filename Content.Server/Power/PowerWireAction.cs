using Content.Server.Electrocution;
using Content.Shared.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Power;
using Content.Shared.Wires;

namespace Content.Server.Power;

// Generic power wire action. Use on anything
// that requires power.
public sealed partial class PowerWireAction : BaseWireAction
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-power";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    private ElectrocutionSystem _electrocution = default!;

    public override object StatusKey { get; } = PowerWireActionKey.Status;

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (WiresSystem.TryGetData<int>(wire.Owner, PowerWireActionKey.MainWire, out var main)
            && main != wire.Id)
        {
            return null;
        }

        if (!AllWiresMended(wire.Owner)
                || WiresSystem.TryGetData<bool>(wire.Owner, PowerWireActionKey.Pulsed, out var pulsed)
                && pulsed)
        {
            return StatusLightState.BlinkingSlow;
        }

        return AllWiresCut(wire.Owner) ? StatusLightState.Off : StatusLightState.On;
    }

    private bool AllWiresCut(EntityUid owner)
    {
        return WiresSystem.TryGetData<int?>(owner, PowerWireActionKey.CutWires, out var cut)
            && WiresSystem.TryGetData<int?>(owner, PowerWireActionKey.WireCount, out var count)
            && count == cut;
    }

    private bool AllWiresMended(EntityUid owner)
    {
        return WiresSystem.TryGetData<int?>(owner, PowerWireActionKey.CutWires, out var cut)
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
            if (WiresSystem.TryGetData<bool>(owner, PowerWireActionKey.Pulsed, out var isPulsed)
                && isPulsed)
            {
                return;
            }

            power.PowerDisabled = false;
        }
    }

    private void SetWireCuts(EntityUid owner, bool isCut)
    {
        if (WiresSystem.TryGetData<int?>(owner, PowerWireActionKey.CutWires, out var cut)
            && WiresSystem.TryGetData<int?>(owner, PowerWireActionKey.WireCount, out var count))
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

        _electrocution.SetElectrifiedWireCut((used, electrified), setting);
        _electrocution.SetElectrified((used, electrified), setting);
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

        var electrifiedAttempt = _electrocution.TryDoElectrifiedAct(wire.Owner, user);

        // if we were electrified, then return false
        return !electrifiedAttempt;

    }

    private void UpdateElectrocution(Wire wire)
    {
        var allCut = AllWiresCut(wire.Owner);

        var activePulse = false;

        if (WiresSystem.TryGetData<bool>(wire.Owner, PowerWireActionKey.Pulsed, out var pulsed))
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

        _electrocution = EntityManager.System<ElectrocutionSystem>();
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
        base.Cut(user, wire);
        if (!TrySetElectrocution(user, wire))
            return false;

        SetWireCuts(wire.Owner, true);

        SetPower(wire.Owner, false);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        if (!TrySetElectrocution(user, wire))
            return false;

        // Mending any power wire restores shorts.
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.PulseCancel);
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        SetWireCuts(wire.Owner, false);

        SetPower(wire.Owner, false);

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        WiresSystem.TryCancelWireAction(wire.Owner, PowerWireActionKey.ElectrifiedCancel);

        var electrocuted = !TrySetElectrocution(user, wire, true);

        if (WiresSystem.TryGetData<bool>(wire.Owner, PowerWireActionKey.Pulsed, out var pulsedKey) && pulsedKey)
            return;

        WiresSystem.SetData(wire.Owner, PowerWireActionKey.Pulsed, true);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PowerWireActionKey.PulseCancel, new TimedWireEvent(AwaitPulseCancel, wire));

        if (electrocuted)
            return;

        SetPower(wire.Owner, true);
    }

    public override void Update(Wire wire)
    {
        UpdateElectrocution(wire);

        if (!IsPowered(wire.Owner))
        {
            if (!WiresSystem.TryGetData<bool>(wire.Owner, PowerWireActionKey.Pulsed, out var pulsed)
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
