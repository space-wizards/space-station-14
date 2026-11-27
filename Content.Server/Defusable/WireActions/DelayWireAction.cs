using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Defusable.Components;
using Content.Shared.Defusable.Systems;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed partial class DelayWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Yellow;
    public override string Name { get; set; } = "wire-name-bomb-delay";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.DelayWireUsed ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey { get; } = DefusableWireStatus.DelayIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<DefusableSystem>().DelayWirePulse(user, (wire.Owner, comp));
    }
}
