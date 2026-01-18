using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Systems;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed partial class SpaceVillainOverflowToggleWireAction : ComponentWireAction<SpaceVillainArcadeComponent>
{
    public override string Name { get; set; } = "wire-name-space-villain-overflow";
    public override Color Color { get; set; } = Color.AliceBlue;
    public override object StatusKey { get; } = SpaceVillainArcadeWireStatus.Overflow;

    public override StatusLightState? GetLightState(Wire wire, SpaceVillainArcadeComponent component)
    {
        return component.Overflow ? StatusLightState.BlinkingFast : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetOverflow((wire.Owner, component), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetOverflow((wire.Owner, component), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetOverflow((wire.Owner, component), !component.Overflow);
    }
}
