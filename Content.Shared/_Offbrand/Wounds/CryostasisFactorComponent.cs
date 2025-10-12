namespace Content.Shared._Offbrand.Wounds;

// smalldoggers guessed what these numbers meant just from the graphs on discord :]
[RegisterComponent]
public sealed partial class CryostasisFactorComponent : Component
{
    /// <summary>
    /// The body's temperature will be multiplied by this value to determine its contribution to the stasis factor
    /// </summary>
    [DataField(required: true)]
    public float TemperatureCoefficient;

    /// <summary>
    /// This constant will be added to the stasis factor
    /// </summary>
    [DataField(required: true)]
    public float TemperatureConstant;
}
