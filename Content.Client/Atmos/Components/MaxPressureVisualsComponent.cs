namespace Content.Client.Atmos.Components;

/// <summary>
/// This is used for...
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
