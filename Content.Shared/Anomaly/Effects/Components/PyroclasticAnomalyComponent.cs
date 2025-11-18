namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class PyroclasticAnomalyComponent : Component
{
    /// <summary>
    /// The maximum distance from which you can be ignited by the anomaly.
    /// </summary>
    [DataField("maximumIgnitionRadius")]
    public float MaximumIgnitionRadius = 5f;
}
