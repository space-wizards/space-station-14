using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed class ActivateBombWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Lime;
    public override string Name { get; set; } = "wire-name-bolt-light";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
        => comp.BombLive ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.BoltLightIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            EntityManager.System<DefusableSystem>().DefuseBomb(comp);
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
            comp.TimeUntilExplosion += comp.DelayTime;
            comp.ActivateWireUsed = true;
        }
        else
        {
            EntityManager.System<DefusableSystem>().StartCountdown(comp);
        }
    }
}
