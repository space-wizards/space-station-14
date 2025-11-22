using Content.Server.Wires;
using Content.Shared.MassDriver;
using Content.Shared.MassDriver.Components;
using Content.Shared.Wires;

namespace Content.Server.MassDriver.EntitySystems;

public sealed partial class MassDriverSecurityWireAction : ComponentWireAction<MassDriverComponent>
{
    public override Color Color { get; set; } = Color.DarkRed;
    public override string Name { get; set; } = "wire-name-security";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightState? GetLightState(Wire wire, MassDriverComponent component) => component.Hacked ? StatusLightState.Off : StatusLightState.On;
    public override object StatusKey => SecurityWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, MassDriverComponent component)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        component.Hacked = true;

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, MassDriverComponent component)
    {
        component.Hacked = false;

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, MassDriverComponent component)
    {
        component.Hacked = true;
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut
            && EntityManager.TryGetComponent<MassDriverComponent>(wire.Owner, out var driver))
            driver.Hacked = false;
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
