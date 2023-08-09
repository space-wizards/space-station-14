using Content.Server.Medical.Components;
using Content.Server.Wires;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Wires;

namespace Content.Server.Medical;

/// <summary>
/// Causes a failure in the cryo pod ejection system when cut. A crowbar will be needed to pry open the pod.
/// </summary>
[DataDefinition]
public sealed class CryoPodEjectLockWireAction: BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "LOCK";

    public override object? StatusKey { get; } = CryoPodWireActionKey.Key;
    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<CryoPodComponent>(wire.Owner, out var cryoPodComponent) && !cryoPodComponent.PermaLocked)
        {
            cryoPodComponent.Locked = true;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<CryoPodComponent>(wire.Owner, out var cryoPodComponent) && !cryoPodComponent.PermaLocked)
        {
            cryoPodComponent.Locked = false;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        return true;
    }

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (EntityManager.TryGetComponent<CryoPodComponent>(wire.Owner, out var cryoPodComponent) && cryoPodComponent.Locked)
        {
            lightState = StatusLightState.On; //TODO figure out why this doesn't get updated when the pod is emagged
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }
}
