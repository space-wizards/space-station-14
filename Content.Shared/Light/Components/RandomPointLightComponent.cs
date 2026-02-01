using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Gives a point light random radius, energy and color when added to an entity with a <see cref="SharedPointLightComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomPointLightComponent : Component
{
    /// <summary>
    /// Maximum value for the random radius
    /// </summary>
    [DataField]
    public float MaxRadius = 6;

    /// <summary>
    /// Minimum value for the random radius
    /// </summary>
    [DataField]
    public float MinRadius = 3;

    /// <summary>
    /// Maximum value for the random energy
    /// </summary>
    [DataField]
    public float MaxEnergy = 5;

    /// <summary>
    /// Minimum value for the random energy
    /// </summary>
    [DataField]
    public float MinEnergy = 1;
}
