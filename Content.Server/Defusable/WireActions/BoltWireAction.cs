using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Defusable.Components;
using Content.Shared.Defusable.Systems;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed partial class BoltWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-bomb-bolt";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.Bolted ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey { get; } = DefusableWireStatus.BoltIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return EntityManager.System<DefusableSystem>().BoltWireCut(user, (wire.Owner, comp));
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return EntityManager.System<DefusableSystem>().BoltWireMend(user, (wire.Owner, comp));
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<DefusableSystem>().BoltWirePulse(user, (wire.Owner, comp));
    }
}
