using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Wires;

namespace Content.Server.Access;

public sealed partial class AccessWireAction : ComponentWireAction<AccessReaderComponent>
{
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-access";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightState? GetLightState(Wire wire, AccessReaderComponent comp)
    {
        return comp.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AccessWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        comp.Enabled = false;
        EntityManager.Dirty(comp);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        if (!EntityManager.HasComponent<EmaggedComponent>(wire.Owner))
        {
            comp.Enabled = true;
            EntityManager.Dirty(comp);
        }
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        comp.Enabled = false;
        EntityManager.Dirty(comp);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        }
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access) && !EntityManager.HasComponent<EmaggedComponent>(wire.Owner))
            {
                access.Enabled = true;
                EntityManager.Dirty(access);
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
