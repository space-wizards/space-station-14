using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Wires;
using Content.Shared.Defusable;
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

    public override bool Cut(EntityUid user, Wire wire, Entity<DefusableComponent> comp)
    {
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, Entity<DefusableComponent> comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, Entity<DefusableComponent> comp)
    {
        EntityManager.System<DefusableSystem>().DelayWirePulse(user, wire, comp);
    }
}
