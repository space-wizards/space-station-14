using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed class BoomWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-bomb-boom";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
        => StatusLightState.On;

    public override object StatusKey { get; } = DefusableWireStatus.BoomIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            EntityManager.System<DefusableSystem>().TryDetonateBomb(wire.Owner, comp);
        }
        else
        {
            comp.BombUsable = false;
        }
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (!comp.BombLive && !comp.BombUsable)
            comp.BombUsable = true;
        // you're already dead lol
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            EntityManager.System<DefusableSystem>().TryDetonateBomb(wire.Owner, comp);
        }
    }
}
