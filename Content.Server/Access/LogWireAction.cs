using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Wires;

namespace Content.Server.Access;

public sealed partial class LogWireAction : ComponentWireAction<AccessReaderComponent>
{
    public override Color Color { get; set; } = Color.Blue;
    public override string Name { get; set; } = "wire-name-log";

    [DataField]
    public int PulseTimeout = 30;

    [DataField]
    public LocId PulseLog = "log-wire-pulse-access-log";

    private AccessReaderSystem _access = default!;

    public override StatusLightState? GetLightState(Wire wire, AccessReaderComponent comp)
    {
        return comp.LoggingDisabled ? StatusLightState.Off : StatusLightState.On;
    }

    public override object StatusKey => LogWireActionKey.Status;

    public override void Initialize()
    {
        base.Initialize();

        _access = EntityManager.System<AccessReaderSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.System<AccessReaderSystem>().SetLoggingActive((wire.Owner, comp), false);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        EntityManager.System<AccessReaderSystem>().SetLoggingActive((wire.Owner, comp), true);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        _access.LogAccess((wire.Owner, comp), Loc.GetString(PulseLog));
        EntityManager.System<AccessReaderSystem>().SetLoggingActive((wire.Owner, comp), false);
        WiresSystem.StartWireAction(wire.Owner, PulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut && EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var comp))
            EntityManager.System<AccessReaderSystem>().SetLoggingActive((wire.Owner, comp), true);
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
