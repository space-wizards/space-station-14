using Content.Server.Defusable.Components;
using Content.Server.Defusable.Systems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Defusable;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

namespace Content.Server.Defusable.WireActions;

public sealed class BoltWireAction : ComponentWireAction<DefusableComponent>
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
        if (comp.Activated)
        {
            EntityManager.System<DefusableSystem>().SetBolt(comp, false);
            EntityManager.System<AudioSystem>().PlayPvs(comp.BoltSound, wire.Owner);
            EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);
        }
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DefusableComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DefusableComponent comp)
    {
        EntityManager.System<PopupSystem>().PopupEntity(Loc.GetString("defusable-popup-wire-bolt-pulse", ("name", wire.Owner)), wire.Owner);
    }
}
