using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed class ActivateBombWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Lime;
    public override string Name { get; set; } = "wire-name-bomb-live";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.BombLive ? StatusLightState.Off : StatusLightState.BlinkingFast;
    }

    public override object StatusKey { get; } = DefusableWireStatus.LiveIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            EntityManager.System<DefusableSystem>().TryDefuseBomb(wire.Owner, comp);
        }
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        // bomb is defused why do you want it back on again
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            if (!comp.ActivatedWireCut)
            {
                Logger.Debug("Time delayed");
                EntityManager.System<DefusableSystem>().TryDelay(wire.Owner, 30f);
                comp.ActivatedWireCut = true;
            }
        }
        else
        {
            EntityManager.System<DefusableSystem>().TryStartCountdown(wire.Owner, comp);
        }
    }
}
