using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent, NetworkedComponent]
public sealed class BluespaceAnomalyComponent : Component
{
    public float MaxShuffleRadius = 10;

    public float MaxShuffleVariation = 4;

    public float MaxPortalRadius = 25;

    public float MinPortalRadius = 10;
}
