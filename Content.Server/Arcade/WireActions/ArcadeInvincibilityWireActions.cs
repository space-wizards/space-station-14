using Content.Server.Arcade.Components;
using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

[DataDefinition]
public sealed class ArcadePlayerInvincibleWireAction : BaseToggleWireAction
{
    private string _text = "MNGR";
    private Color _color = Color.Purple;

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

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;

        if (IsPowered(wire.Owner) && EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(wire.Owner, out var arcade))
        {
            lightState = arcade.PlayerInvincibilityFlag || arcade.EnemyInvincibilityFlag
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }
}

[DataDefinition]
public sealed class ArcadeEnemyInvincibleWireAction : BaseToggleWireAction
{
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
