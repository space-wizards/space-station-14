using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed partial class ProceedWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Blue;
    public override string Name { get; set; } = "wire-name-bomb-proceed";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.Activated ? StatusLightState.Off : StatusLightState.BlinkingFast;
    }

    public override object StatusKey { get; } = DefusableWireStatus.ProceedIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return EntityManager.System<DefusableSystem>().ProceedWireCut(user, wire, comp);
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<DefusableSystem>().ProceedWirePulse(user, wire, comp);
    }
}
