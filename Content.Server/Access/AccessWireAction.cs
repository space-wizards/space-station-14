using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Wires;
using Content.Server.Electrocution; // Starlight-edit
using Content.Shared.Electrocution; // Starlight-edit

namespace Content.Server.Access;

public sealed partial class AccessWireAction : ComponentWireAction<AccessReaderComponent>
{
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-access";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;
    
    [DataField("electrify")]
    private bool _electrify = false; // Starlight-edit
    
    private ElectrocutionSystem _electrocution = default!; // Starlight-edit
    
    public override StatusLightState? GetLightState(Wire wire, AccessReaderComponent comp)
    {
        return comp.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AccessWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        if (!TrySetElectrocution(user, wire)) // Starlight-edit
            return false;
        
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.System<AccessReaderSystem>().SetActive((wire.Owner, comp), false);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        if (!TrySetElectrocution(user, wire)) // Starlight-edit
            return false;
        
        EntityManager.System<AccessReaderSystem>().SetActive((wire.Owner, comp), true);

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        var electrified = TrySetElectrocution(user, wire, true); // Starlight-edit
        EntityManager.System<AccessReaderSystem>().SetActive((wire.Owner, comp), false);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        }
    }
    
    // Starlight-start: access wire electrify
    public override void Initialize()
    {
        base.Initialize();

        _electrocution = EntityManager.System<ElectrocutionSystem>();
    }
    
    /// <returns>false if failed, true otherwise, or if the entity cannot be electrified</returns>
    private bool TrySetElectrocution(EntityUid user, Wire wire, bool timed = false)
    {
        if (!EntityManager.TryGetComponent<ElectrifiedComponent>(wire.Owner, out var electrified) || !_electrify)
            return true;

        var electrifiedAttempt = _electrocution.TryDoElectrifiedAct(wire.Owner, user);

        // if we were electrified, then return false
        return !electrifiedAttempt;

    }
    // Starlight-end

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
            {
                EntityManager.System<AccessReaderSystem>().SetActive((wire.Owner, access), true);
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
