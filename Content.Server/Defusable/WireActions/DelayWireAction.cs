using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

namespace Content.Server.Defusable.WireActions;

public sealed class DelayWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-bomb-delay";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
        => comp.DelayWireUsed ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = DefusableWireStatus.BoomIndicator;

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
        if (comp.BombLive && !comp.DelayWireUsed)
        {
            EntityManager.System<DefusableSystem>().TryDelay(wire.Owner, 30f);
            comp.DelayWireUsed = true;
        }
        EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-delay-pulse", ("name", wire.Owner)), wire.Owner);
    }
}
