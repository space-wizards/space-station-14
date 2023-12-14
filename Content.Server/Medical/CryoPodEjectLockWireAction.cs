using Content.Server.Medical.Components;
using Content.Server.Wires;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Wires;

namespace Content.Server.Medical;

/// <summary>
/// Causes a failure in the cryo pod ejection system when cut. A crowbar will be needed to pry open the pod.
/// </summary>
public sealed partial class CryoPodEjectLockWireAction: ComponentWireAction<CryoPodComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-lock";
    public override bool LightRequiresPower { get; set; } = false;

    public override object? StatusKey { get; } = CryoPodWireActionKey.Key;
    public override bool Cut(EntityUid user, Wire wire, CryoPodComponent cryoPodComponent)
    {
        if (!cryoPodComponent.PermaLocked)
            cryoPodComponent.Locked = true;

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, CryoPodComponent cryoPodComponent)
    {
        if (!cryoPodComponent.PermaLocked)
            cryoPodComponent.Locked = false;

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, CryoPodComponent cryoPodComponent) { }

    public override StatusLightState? GetLightState(Wire wire, CryoPodComponent comp)
        => comp.Locked ? StatusLightState.On : StatusLightState.Off;
}
