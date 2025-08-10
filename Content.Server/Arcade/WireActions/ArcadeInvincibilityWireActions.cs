using Content.Server.Arcade.SpaceVillain;
using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed partial class ArcadePlayerInvincibleWireAction : BaseToggleWireAction
{
    public override string Name { get; set; } = "wire-name-arcade-player-invincible";

    public override Color Color { get; set; } = Color.Purple;

    public override object? StatusKey { get; } = SharedSpaceVillainArcadeComponent.Indicators.PlayerInvinc;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.InvincFlag = !setting;
            if (arcade.Game != null)
            {
                arcade.Game.PlayerChar.Invincible = arcade.InvincFlag;
            }
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.InvincFlag;
    }

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(wire.Owner, out var arcade))
        {
            return arcade.InvincFlag
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return StatusLightState.Off;
    }
}

public sealed partial class ArcadeEnemyInvincibleWireAction : BaseToggleWireAction
{
    public override string Name { get; set; } = "wire-name-arcade-villain-invincible";
    public override Color Color { get; set; } = Color.Purple;

    public override object? StatusKey { get; } = SharedSpaceVillainArcadeComponent.Indicators.VillainInvinc;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.VillainInvincFlag = !setting;
            if (arcade.Game != null)
            {
                arcade.Game.VillainChar.Invincible = arcade.VillainInvincFlag;
            }
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.VillainInvincFlag;
    }


    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(wire.Owner, out var arcade))
        {
            return arcade.VillainInvincFlag
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return StatusLightState.Off;
    }
}

public enum ArcadeInvincibilityWireActionKeys : short
{
    Player,
    Enemy
}
