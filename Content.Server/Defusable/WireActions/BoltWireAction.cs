using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

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
        return EntityManager.System<DefusableSystem>().BoltWireCut(user, wire, comp);
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return EntityManager.System<DefusableSystem>().BoltWireMend(user, wire, comp);
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<DefusableSystem>().BoltWirePulse(user, wire, comp);
    }
}
