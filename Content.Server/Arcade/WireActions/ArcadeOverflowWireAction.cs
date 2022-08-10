using Content.Server.Arcade.Components;
using Content.Server.Wires;
using Content.Shared.Arcade;
using Content.Shared.Wires;

namespace Content.Server.Arcade;

[DataDefinition]
public sealed class ArcadeOverflowWireAction : BaseToggleWireAction
{
    private Color _color = Color.Red;
    private string _text = "LMTR";

    public override object? StatusKey { get; } = SharedSpaceVillainArcadeComponent.Indicators.HealthLimiter;

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade))
        {
            arcade.OverflowFlag = !setting;
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent<SpaceVillainArcadeComponent>(owner, out var arcade)
            && !arcade.OverflowFlag;
    }

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;

        if (IsPowered(wire.Owner) && EntityManager.HasComponent<SpaceVillainArcadeComponent>(wire.Owner))
        {
            lightState = !GetValue(wire.Owner)
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }
}
