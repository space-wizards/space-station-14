using Content.Server.Arcade.Components;
using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed class ArcadePlayerInvincibleWireAction : ComponentWireAction<SharedSpaceVillainArcadeComponent>
{
    public override string Name { get; set; } = "MNGR";

    public override Color Color { get; set; } = Color.Purple;

    public override object? StatusKey { get; } = SharedSpaceVillainArcadeComponent.Indicators.HealthManager;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.PlayerInvincibilityFlag = !setting;
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.PlayerInvincibilityFlag;
    }

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(wire.Owner, out var arcade))
        {
            return arcade.PlayerInvincibilityFlag || arcade.EnemyInvincibilityFlag
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return StatusLightState.Off;
    }
}

public sealed class ArcadeEnemyInvincibleWireAction : ComponentWireAction<>
{
    // not displayed, just for admin logging
    public override string Name { get; set; } = nameof(ArcadeEnemyInvincibleWireAction); 

    public override Color Color { get; set; } =  default;
    public override bool RequirePower { get; set; } = false;

    public override object? StatusKey { get; } = null;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.PlayerInvincibilityFlag = !setting;
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.PlayerInvincibilityFlag;
    }

    public override StatusLightData? GetStatusLightData(Wire wire) => null;
}

public enum ArcadeInvincibilityWireActionKeys : short
{
    Player,
    Enemy
}
