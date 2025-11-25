using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Gives a point light random radius, energy and color when added to an entity with a point light
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomPointLightComponent : Component
{
    /// <summary>
    /// Maximum value for the random radius
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRadius = 6;

    /// <summary>
    /// Minimum value for the random radius
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinRadius = 1;

    /// <summary>
    /// Maximum value for the random energy
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxEnergy = 5;

    /// <summary>
    /// Minimum value for the random energy
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinEnergy = 1;
}
