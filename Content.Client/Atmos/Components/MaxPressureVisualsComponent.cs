using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Client.Atmos.Components;

/// <summary>
/// This listens to appearance changes from <see cref="GasMaxPressureSystem{T}"/>
/// and applies sprite changes to a gas holder currently experiencing <see cref="IGasMaxPressureHolder.Integrity"/> loss.
/// </summary>
[RegisterComponent]
public sealed partial class MaxPressureVisualsComponent : Component
{
    /// <summary>
    /// What RsiState we use for our integrity visuals.
    /// </summary>
    [DataField]
    public string? IntegrityState = "integrity";

    /// <summary>
    /// What RsiState we use for the mask that goes over integrity visuals.
    /// </summary>
    [DataField]
    public string? IntegrityMask = "mask";

    /// <summary>
    /// How many steps there are
    /// </summary>
    [DataField("steps")]
    public int IntegritySteps = 5;
}

public enum MaxPressureVisualLayers : byte
{
    Base,
    BaseUnshaded,
}
