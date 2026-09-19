using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Defusable.Components;
using Content.Shared.Defusable.Systems;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed partial class ActivateWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Lime;
    public override string Name { get; set; } = "wire-name-bomb-live";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.Activated ? StatusLightState.BlinkingFast : StatusLightState.Off;
    }

    public override object StatusKey { get; } = DefusableWireStatus.LiveIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return EntityManager.System<DefusableSystem>().ActivateWireCut(user, (wire.Owner, comp));
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        // if its not disposable defusable system already handles* this
        // *probably
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<DefusableSystem>().ActivateWirePulse(user, (wire.Owner, comp));
    }
}
