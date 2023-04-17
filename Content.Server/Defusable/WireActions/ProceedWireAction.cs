using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Defusable.WireActions;

public sealed class ProceedWireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Blue;
    public override string Name { get; set; } = "wire-name-bomb-proceed";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
    {
        return comp.BombLive ? StatusLightState.Off : StatusLightState.BlinkingFast;
    }

    public override object StatusKey { get; } = DefusableWireStatus.LiveIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive && !comp.ProceedWireCut)
        {
            EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner);
            comp.BombDisplayTime = false;
            comp.ProceedWireCut = true;
        }
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive && !comp.ProceedWireUsed)
        {
            Logger.Debug("Time proceeded");
            comp.ProceedWireUsed = true;
            EntityManager.System<DefusableSystem>().TryDelay(wire.Owner, -15f);
        }
        EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-proceed-pulse", ("name", wire.Owner)), wire.Owner);
    }
}
