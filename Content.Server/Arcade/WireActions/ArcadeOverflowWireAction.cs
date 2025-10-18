using Content.Server.Arcade.SpaceVillain;
using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

public sealed partial class ArcadeOverflowWireAction : BaseToggleWireAction
{
    public override Color Color { get; set; } = Color.OrangeRed;
    public override string Name { get; set; } = "wire-name-arcade-overflow";

    public override object? StatusKey { get; } = SharedSpaceVillainArcadeComponent.Indicators.HealthLimiter;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.UncappedFlag = !setting;
            if (arcade.Game != null)
            {
                arcade.Game.PlayerChar.Uncapped = !setting;
                arcade.Game.VillainChar.Uncapped = !setting;
            }
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.UncappedFlag;
    }

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.HasComponent<SpaceVillainArcadeComponent>(wire.Owner))
        {
            return !GetValue(wire.Owner)
                ? StatusLightState.Off
                : StatusLightState.On;
        }

        return StatusLightState.Off;
    }
}
