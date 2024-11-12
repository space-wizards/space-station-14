using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component to handle non-breathing gas interactions.
/// Detects gasses around entities and applies effects. (this is currently for damage to borgs but ¯\_(ツ)_/¯)
/// </summary>
[RegisterComponent]
public sealed partial class EffectedByGasComponent : Component
{
    [ViewVariables]
    public GasMixture? GasMixture;


    /// amount of gas needed to trigger effect
    [DataField("gasThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float GasThreshold = 0.1f;

}
