using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Wires;

namespace Content.Server.Access;

public sealed class AccessWireAction : ComponentWireAction<AccessReaderComponent>
{
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-access";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightState? GetLightState(Wire wire, AccessReaderComponent comp)
    {
        return EntityManager.HasComponent<EmaggedComponent>(comp.Owner) ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey { get; } = AccessWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.RemoveComponent<EmaggedComponent>(comp.Owner);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        EntityManager.AddComponent<EmaggedComponent>(comp.Owner);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        EntityManager.RemoveComponent<EmaggedComponent>(comp.Owner);
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
            // check is still here incase you somehow TOCTOU it into unemagging something it shouldn't
            if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
            {
                EntityManager.RemoveComponent<EmaggedComponent>(wire.Owner);
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
