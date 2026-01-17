using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Systems;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed partial class SpaceVillainInvincibleVillainToggleWireAction : ComponentWireAction<SpaceVillainArcadeComponent>
{
    public override string Name { get; set; } = "wire-name-space-villain-invincible-villain";
    public override Color Color { get; set; } = Color.PaleVioletRed;
    public override object StatusKey { get; } = SpaceVillainWireStatus.InvincibleVillain;

    public override StatusLightState? GetLightState(Wire wire, SpaceVillainArcadeComponent component)
    {
        return component.InvincibleVillain ? StatusLightState.BlinkingFast : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvincibleVillain((wire.Owner, component), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvincibleVillain((wire.Owner, component), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvincibleVillain((wire.Owner, component), !component.InvincibleVillain);
    }
}
