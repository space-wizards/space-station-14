using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Systems;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed partial class SpaceVillainInvinciblePlayerToggleWireAction : ComponentWireAction<SpaceVillainArcadeComponent>
{
    public override string Name { get; set; } = "wire-name-space-villain-invincible-player";
    public override Color Color { get; set; } = Color.Green;
    public override object StatusKey { get; } = SpaceVillainWireStatus.InvinciblePlayer;

    public override StatusLightState? GetLightState(Wire wire, SpaceVillainArcadeComponent component)
    {
        return component.InvinciblePlayer ? StatusLightState.BlinkingFast : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvinciblePlayer((wire.Owner, component), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvinciblePlayer((wire.Owner, component), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SpaceVillainArcadeComponent component)
    {
        EntityManager.System<SharedSpaceVillainArcadeSystem>().SetInvinciblePlayer((wire.Owner, component), !component.InvinciblePlayer);
    }
}
