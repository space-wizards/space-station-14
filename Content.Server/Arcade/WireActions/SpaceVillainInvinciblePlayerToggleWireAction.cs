using Content.Server.Wires;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Wires;

namespace Content.Server.Arcade.WireActions;

public sealed partial class SpaceVillainInvinciblePlayerToggleWireAction : BaseToggleWireAction
{
    public override string Name { get; set; } = "wire-name-space-villain-invincible-player";
    public override Color Color { get; set; } = Color.AliceBlue;
    public override object StatusKey { get; } = SpaceVillainArcadeWireStatus.InvinciblePlayer;

    public override StatusLightState? GetLightState(Wire wire)
    {
        return GetValue(wire.Owner) ? StatusLightState.BlinkingSlow : StatusLightState.Off;
    }

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (setting)
            EntityManager.EnsureComponent<SpaceVillainArcadeInvinciblePlayerComponent>(owner);
        else
            EntityManager.RemoveComponent<SpaceVillainArcadeInvinciblePlayerComponent>(owner);
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.HasComponent<SpaceVillainArcadeInvinciblePlayerComponent>(owner);
    }
}
