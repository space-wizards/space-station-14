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

public sealed class BoltireAction : ComponentWireAction<DefusableComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-bomb-bolt";

    public override StatusLightState? GetLightState(Wire wire, DefusableComponent comp)
        => comp.Bolted ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = DefusableWireStatus.BoomIndicator;

    public override bool Cut(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            comp.Bolted = false;
            EntityManager.System<AudioSystem>().PlayPvs(comp.BoltSound, wire.Owner);
        }
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        if (comp.BombLive)
        {
            EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);
        }
    }
}
